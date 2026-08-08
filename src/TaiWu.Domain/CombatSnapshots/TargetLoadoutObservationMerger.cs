using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public static class TargetLoadoutObservationMerger
{
    public const string TargetEquippedSkillsField =
        "target.equippedSkills";

    public const string TargetLoadoutObservationField =
        "target.loadoutObservation";

    public const string TargetVisibleActiveEffectsField =
        "target.visibleActiveEffects";

    public static TargetLoadoutObservationMergeResult Merge(
        CombatSnapshot snapshot,
        TargetLoadoutObservation observation,
        IEnumerable<CombatSkillSnapshot>? resolvedObservedSkills = null,
        bool confirmPrecedenceWhenSaveTimeUnavailable = false)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(observation);
        if (snapshot.Target.CharacterId != observation.TargetCharacterId)
        {
            throw new ArgumentException(
                "The target observation does not identify the snapshot target.",
                nameof(observation));
        }

        var observedLoadout = CreateObservedLoadout(observation.ObservedSkills);
        if (!SupportsObservationVersion(snapshot))
        {
            return Unapplied(
                snapshot,
                observation,
                observedLoadout,
                TargetLoadoutMergeStatus.UnsupportedVersion,
                CombatSnapshotWarningCodes
                    .TargetObservationUnsupportedVersion,
                "The target observation was not applied because the "
                + "GameData version does not match E3-000 evidence.",
                "UNSUPPORTED_GAMEDATA_VERSION");
        }

        if (snapshot.Metadata.SaveLastWriteTimeUtc.IsAvailable
            && observation.ObservedAtUtc
                <= snapshot.Metadata.SaveLastWriteTimeUtc.Value)
        {
            return Stale(snapshot, observation, observedLoadout);
        }

        if (!snapshot.Metadata.SaveLastWriteTimeUtc.IsAvailable
            && !confirmPrecedenceWhenSaveTimeUnavailable)
        {
            return Unapplied(
                snapshot,
                observation,
                observedLoadout,
                TargetLoadoutMergeStatus.PrecedenceConfirmationRequired,
                CombatSnapshotWarningCodes
                    .TargetObservationSaveTimeConfirmationRequired,
                "The target observation requires explicit source-precedence "
                + "confirmation because the save timestamp is unavailable.",
                "SAVE_TIMESTAMP_CONFIRMATION_REQUIRED");
        }

        var resolved = CopyResolvedSkills(resolvedObservedSkills);
        var learnedSkills = MergeLearnedSkills(
            snapshot.Target.LearnedSkills,
            observation.ObservedSkills,
            resolved);
        var equippedSkills = MergeEquippedSkills(
            snapshot.Target.EquippedSkills,
            observedLoadout,
            observation);
        var target = new TargetCombatSnapshot(
            snapshot.Target.CharacterId,
            snapshot.Target.DisplayName,
            snapshot.Target.Age,
            snapshot.Target.Features,
            learnedSkills,
            equippedSkills,
            snapshot.Target.Equipment,
            observation);

        var warningValues = snapshot.Warnings.ToList();
        if (observation.Coverage.Kind
            == TargetLoadoutCoverageKind.CompleteCurrentLoadout)
        {
            warningValues.RemoveAll(warning => warning.Code
                == CombatSnapshotWarningCodes.TargetLoadoutNotPersisted);
        }
        else
        {
            AddWarning(
                warningValues,
                CombatSnapshotWarningCodes.TargetObservationPartial,
                observation.ObservationContext
                    == TargetObservationContext.Sparring
                    ? "The target observation is partial; omitted skills "
                        + "remain unknown."
                    : "The target's full loadout is unavailable; only the "
                        + "reported battle-visible active effects are known, "
                        + "and omitted skills remain unknown.");
        }

        if (!snapshot.Metadata.SaveLastWriteTimeUtc.IsAvailable)
        {
            AddWarning(
                warningValues,
                CombatSnapshotWarningCodes
                    .TargetObservationSaveTimeUnavailable,
                "The target observation was applied after explicit source "
                + "precedence confirmation because the save timestamp is "
                + "unavailable.");
        }

        var loadoutConflict =
            observation.Coverage.Kind
                == TargetLoadoutCoverageKind.CompleteCurrentLoadout
            && snapshot.Target.EquippedSkills.IsAvailable
            && !LoadoutsEqual(
                snapshot.Target.EquippedSkills.Value,
                observedLoadout);
        var directionEvidence = CreateDirectionEvidence(
            snapshot,
            observation);
        var directionConflict = directionEvidence.Any(value =>
            value.Evidence.Status == SnapshotEvidenceStatus.Conflicting);
        if (loadoutConflict || directionConflict)
        {
            AddWarning(
                warningValues,
                CombatSnapshotWarningCodes.TargetObservationSaveConflict,
                "The target observation conflicts with saved loadout or "
                + "direction evidence; both sources are retained.");
        }

        var newSources = CreateAppliedFieldSources(snapshot, observation);
        var mergedSnapshot = new CombatSnapshot(
            snapshot.Metadata,
            snapshot.Player,
            target,
            warningValues,
            newSources);
        var loadoutEvidence = CreateAppliedLoadoutEvidence(
            snapshot,
            observation,
            observedLoadout,
            loadoutConflict);
        return new TargetLoadoutObservationMergeResult(
            TargetLoadoutMergeStatus.Applied,
            mergedSnapshot,
            observation,
            loadoutEvidence,
            directionEvidence);
    }

    private static bool SupportsObservationVersion(CombatSnapshot snapshot) =>
        snapshot.Metadata.GameDataVersion.IsAvailable
        && string.Equals(
            snapshot.Metadata.GameDataVersion.Value,
            TargetLoadoutCompletenessEvidence.E3000GameDataVersion,
            StringComparison.Ordinal);

    private static ImmutableDictionary<int, CombatSkillSnapshot>
        CopyResolvedSkills(
            IEnumerable<CombatSkillSnapshot>? resolvedObservedSkills)
    {
        var values = (resolvedObservedSkills ?? []).ToImmutableArray();
        if (values.Any(value => value is null))
        {
            throw new ArgumentException(
                "Resolved observed skills cannot contain null entries.",
                nameof(resolvedObservedSkills));
        }

        if (values.GroupBy(value => value.SkillId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Resolved observed skills cannot duplicate a skill ID.",
                nameof(resolvedObservedSkills));
        }

        return values.ToImmutableDictionary(value => value.SkillId);
    }

    private static ImmutableArray<CombatSkillSnapshot> MergeLearnedSkills(
        IEnumerable<CombatSkillSnapshot> savedSkills,
        IEnumerable<ObservedTargetCombatSkill> observedSkills,
        IReadOnlyDictionary<int, CombatSkillSnapshot> resolvedSkills)
    {
        var byId = savedSkills.ToDictionary(skill => skill.SkillId);
        foreach (var observed in observedSkills)
        {
            if (byId.TryGetValue(observed.SkillId, out var saved))
            {
                if (saved.Category != observed.Category)
                {
                    throw new ArgumentException(
                        $"Observed target skill {observed.SkillId} belongs to "
                        + $"{saved.Category}, not {observed.Category}.",
                        nameof(observedSkills));
                }

                if (observed.Direction is not null)
                {
                    byId[observed.SkillId] = CopyWithDirection(
                        saved,
                        SnapshotValue<PracticeDirection>.Available(
                            observed.Direction.Value));
                }

                continue;
            }

            if (!resolvedSkills.TryGetValue(observed.SkillId, out var resolved))
            {
                throw new ArgumentException(
                    $"Observed target skill {observed.SkillId} is absent from "
                    + "the target snapshot and has no resolved static facts.",
                    nameof(resolvedSkills));
            }

            if (resolved.Category != observed.Category)
            {
                throw new ArgumentException(
                    $"Resolved target skill {observed.SkillId} belongs to "
                    + $"{resolved.Category}, not {observed.Category}.",
                    nameof(resolvedSkills));
            }

            var direction = observed.Direction is null
                ? SnapshotValue<PracticeDirection>.Unavailable(
                    "Practice direction was not observed.")
                : SnapshotValue<PracticeDirection>.Available(
                    observed.Direction.Value);
            byId.Add(
                observed.SkillId,
                CopyWithDirection(resolved, direction));
        }

        var observedIds = observedSkills.Select(value => value.SkillId).ToHashSet();
        if (resolvedSkills.Keys.Any(skillId => !observedIds.Contains(skillId)))
        {
            throw new ArgumentException(
                "Resolved static facts include a skill that was not observed.",
                nameof(resolvedSkills));
        }

        return [.. byId.Values.OrderBy(skill => skill.SkillId)];
    }

    private static CombatSkillSnapshot CopyWithDirection(
        CombatSkillSnapshot skill,
        SnapshotValue<PracticeDirection> direction) => new(
            skill.SkillId,
            skill.DisplayName,
            skill.Category,
            skill.GridCost,
            skill.Mastered,
            direction,
            skill.SlotContribution,
            skill.DirectEffectId,
            skill.ReverseEffectId,
            skill.BreakthroughDirections,
            skill.Element);

    private static SnapshotValue<CombatLoadoutSnapshot> MergeEquippedSkills(
        SnapshotValue<CombatLoadoutSnapshot> saved,
        CombatLoadoutSnapshot observed,
        TargetLoadoutObservation observation)
    {
        if (observation.ObservationContext
            != TargetObservationContext.Sparring)
        {
            return saved;
        }

        if (observation.Coverage.Kind
            == TargetLoadoutCoverageKind.CompleteCurrentLoadout)
        {
            return SnapshotValue<CombatLoadoutSnapshot>.Available(observed);
        }

        if (!saved.IsAvailable)
        {
            return saved;
        }

        return SnapshotValue<CombatLoadoutSnapshot>.Available(
            Union(saved.Value, observed));
    }

    private static CombatLoadoutSnapshot Union(
        CombatLoadoutSnapshot saved,
        CombatLoadoutSnapshot observed) => new(
            Combine(saved.NeigongSkillIds, observed.NeigongSkillIds),
            Combine(saved.AttackSkillIds, observed.AttackSkillIds),
            Combine(saved.AgilitySkillIds, observed.AgilitySkillIds),
            Combine(saved.DefenseSkillIds, observed.DefenseSkillIds),
            Combine(saved.AssistanceSkillIds, observed.AssistanceSkillIds));

    private static IEnumerable<int> Combine(
        IEnumerable<int> saved,
        IEnumerable<int> observed) => saved
        .Concat(observed)
        .Distinct()
        .OrderBy(skillId => skillId);

    private static CombatLoadoutSnapshot CreateObservedLoadout(
        IEnumerable<ObservedTargetCombatSkill> skills) => new(
            OrderedIds(skills, SkillCategory.Neigong),
            OrderedIds(skills, SkillCategory.Attack),
            OrderedIds(skills, SkillCategory.Agility),
            OrderedIds(skills, SkillCategory.Defense),
            OrderedIds(skills, SkillCategory.Assistance));

    private static IEnumerable<int> OrderedIds(
        IEnumerable<ObservedTargetCombatSkill> skills,
        SkillCategory category) => skills
        .Where(skill => skill.Category == category)
        .OrderBy(skill => skill.SlotIndex ?? int.MaxValue)
        .ThenBy(skill => skill.SkillId)
        .Select(skill => skill.SkillId);

    private static IEnumerable<SnapshotFieldSource> CreateAppliedFieldSources(
        CombatSnapshot snapshot,
        TargetLoadoutObservation observation)
    {
        HashSet<string> replacedPaths =
        [
            TargetLoadoutObservationField,
            .. observation.ObservedSkills
                .Where(skill => skill.Direction is not null)
                .Select(skill => DirectionField(skill.SkillId))
        ];
        if (observation.Coverage.Kind
            == TargetLoadoutCoverageKind.CompleteCurrentLoadout)
        {
            replacedPaths.Add(TargetEquippedSkillsField);
        }
        else if (observation.ObservationContext
            != TargetObservationContext.Sparring)
        {
            replacedPaths.Add(TargetVisibleActiveEffectsField);
        }

        var retained = snapshot.FieldSources
            .Where(source => !replacedPaths.Contains(source.FieldPath));
        var observedPaths = replacedPaths.Order(StringComparer.Ordinal).Select(
            path => ScreenSource(path, observation));
        return retained.Concat(observedPaths);
    }

    private static SnapshotEvidenceField<CombatLoadoutSnapshot>
        CreateAppliedLoadoutEvidence(
            CombatSnapshot snapshot,
            TargetLoadoutObservation observation,
            CombatLoadoutSnapshot observedLoadout,
        bool conflict)
    {
        var observedField = observation.ObservationContext
            == TargetObservationContext.Sparring
            ? TargetEquippedSkillsField
            : TargetVisibleActiveEffectsField;
        var screen = new SnapshotFieldObservation<CombatLoadoutSnapshot>(
            observedLoadout,
            ScreenSource(observedField, observation));
        if (!conflict)
        {
            return SnapshotEvidenceField<CombatLoadoutSnapshot>.Available(
                observedLoadout,
                screen.Source);
        }

        var save = new SnapshotFieldObservation<CombatLoadoutSnapshot>(
            snapshot.Target.EquippedSkills.Value,
            SaveSource(snapshot, TargetEquippedSkillsField));
        return SnapshotEvidenceField<CombatLoadoutSnapshot>.Conflicting(
            "SAVE_SCREEN_CONFLICT",
            [save, screen]);
    }

    private static ImmutableArray<TargetSkillDirectionEvidence>
        CreateDirectionEvidence(
            CombatSnapshot snapshot,
            TargetLoadoutObservation observation)
    {
        var savedById = snapshot.Target.LearnedSkills.ToDictionary(
            skill => skill.SkillId);
        List<TargetSkillDirectionEvidence> values = [];
        foreach (var observed in observation.ObservedSkills
                     .Where(skill => skill.Direction is not null)
                     .OrderBy(skill => skill.SkillId))
        {
            var field = DirectionField(observed.SkillId);
            var screenSource = ScreenSource(field, observation);
            SnapshotEvidenceField<PracticeDirection> evidence;
            if (savedById.TryGetValue(observed.SkillId, out var saved)
                && saved.Direction.IsAvailable
                && saved.Direction.Value != observed.Direction!.Value)
            {
                evidence = SnapshotEvidenceField<PracticeDirection>.Conflicting(
                    "SAVE_SCREEN_CONFLICT",
                    [
                        new SnapshotFieldObservation<PracticeDirection>(
                            saved.Direction.Value,
                            SaveSource(snapshot, field)),
                        new SnapshotFieldObservation<PracticeDirection>(
                            observed.Direction.Value,
                            screenSource)
                    ]);
            }
            else
            {
                evidence = SnapshotEvidenceField<PracticeDirection>.Available(
                    observed.Direction!.Value,
                    screenSource);
            }

            values.Add(new TargetSkillDirectionEvidence(
                observed.SkillId,
                evidence));
        }

        return [.. values];
    }

    private static TargetLoadoutObservationMergeResult Stale(
        CombatSnapshot snapshot,
        TargetLoadoutObservation observation,
        CombatLoadoutSnapshot observedLoadout)
    {
        var warning = new SnapshotWarning(
            CombatSnapshotWarningCodes.TargetObservationNotNewer,
            "The target observation was not applied because it is not newer "
            + "than the disk save.");
        var copied = CopyWithWarning(snapshot, warning);
        var observedField = observation.ObservationContext
            == TargetObservationContext.Sparring
            ? TargetEquippedSkillsField
            : TargetVisibleActiveEffectsField;
        List<SnapshotFieldObservation<CombatLoadoutSnapshot>> observations =
        [
            new(
                observedLoadout,
                ScreenSource(observedField, observation))
        ];
        if (snapshot.Target.EquippedSkills.IsAvailable)
        {
            observations.Add(new SnapshotFieldObservation<CombatLoadoutSnapshot>(
                snapshot.Target.EquippedSkills.Value,
                SaveSource(snapshot, TargetEquippedSkillsField)));
        }

        return new TargetLoadoutObservationMergeResult(
            TargetLoadoutMergeStatus.Stale,
            copied,
            observation,
            SnapshotEvidenceField<CombatLoadoutSnapshot>.Stale(
                "OBSERVATION_NOT_NEWER_THAN_SAVE",
                observations));
    }

    private static TargetLoadoutObservationMergeResult Unapplied(
        CombatSnapshot snapshot,
        TargetLoadoutObservation observation,
        CombatLoadoutSnapshot observedLoadout,
        TargetLoadoutMergeStatus status,
        string warningCode,
        string warningMessage,
        string reasonCode)
    {
        var copied = CopyWithWarning(
            snapshot,
            new SnapshotWarning(warningCode, warningMessage));
        return new TargetLoadoutObservationMergeResult(
            status,
            copied,
            observation,
            SnapshotEvidenceField<CombatLoadoutSnapshot>.Unavailable(
                reasonCode));
    }

    private static CombatSnapshot CopyWithWarning(
        CombatSnapshot snapshot,
        SnapshotWarning warning)
    {
        var warnings = snapshot.Warnings.Any(value => value.Code == warning.Code)
            ? snapshot.Warnings
            : snapshot.Warnings.Add(warning);
        return new CombatSnapshot(
            snapshot.Metadata,
            snapshot.Player,
            snapshot.Target,
            warnings,
            snapshot.FieldSources);
    }

    private static void AddWarning(
        List<SnapshotWarning> warnings,
        string code,
        string message)
    {
        if (warnings.All(warning => warning.Code != code))
        {
            warnings.Add(new SnapshotWarning(code, message));
        }
    }

    private static SnapshotFieldSource ScreenSource(
        string fieldPath,
        TargetLoadoutObservation observation) => new(
            fieldPath,
            SnapshotDataSource.CurrentScreenObservation,
            observation.ObservedAtUtc,
            observation.EvidenceReference);

    private static SnapshotFieldSource SaveSource(
        CombatSnapshot snapshot,
        string fieldPath) => new(
            fieldPath,
            SnapshotDataSource.Save,
            snapshot.Metadata.SaveLastWriteTimeUtc.IsAvailable
                ? snapshot.Metadata.SaveLastWriteTimeUtc.Value
                : snapshot.Metadata.CapturedAtUtc,
            $"save:{snapshot.Metadata.SaveSha256}");

    private static string DirectionField(int skillId) =>
        $"target.skills.{skillId}.direction";

    private static bool LoadoutsEqual(
        CombatLoadoutSnapshot left,
        CombatLoadoutSnapshot right) => Enum
        .GetValues<SkillCategory>()
        .All(category => left.Get(category).SequenceEqual(right.Get(category)));
}
