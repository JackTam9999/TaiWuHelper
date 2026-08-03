using GameData.Domains.CombatSkill;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Infrastructure.Catalogue;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed record RawCharacterCombatSkillProgress(
    int SkillId,
    bool Learned,
    int? Proficiency,
    int ReadingState,
    int ActivationState,
    bool MeetsBreakthroughReadingRequirement,
    bool Simplified,
    bool Equipped,
    bool DirectBreakthroughCompleted = false,
    bool ReverseBreakthroughCompleted = false);

internal static class CombatSkillProgressMapping
{
    internal const int CacheMappingVersion = 2;

    internal static CharacterCombatSkillProgress Map(
        int characterId,
        SaveSnapshotIdentity snapshot,
        RawCharacterCombatSkillProgress raw,
        string gameDataVersion,
        CombatSkillStudyDetailLabelSet labels,
        ICollection<CharacterCombatSkillProgressWarning> warnings)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(raw);
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDataVersion);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(warnings);

        var saveIdentity = $"save:{snapshot.Sha256}";
        var studyDetails = CombatSkillStudyDetailDecoder.Decode(
            gameDataVersion,
            saveIdentity,
            raw.SkillId,
            raw.ReadingState,
            raw.ActivationState,
            labels,
            warnings);
        var learned = SkillProgressField<bool>.Available(
            raw.Learned,
            SaveSource(saveIdentity, raw.SkillId, "learned-membership"));
        var proficiency = MapProficiency(
            saveIdentity,
            raw,
            warnings);
        var breakthrough = MapBreakthrough(
            saveIdentity,
            raw,
            studyDetails,
            warnings);
        var direction = MapSnapshotValue(
            CombatSnapshotMapping.MapActivePracticeDirection(
                raw.ActivationState,
                raw.SkillId),
            SaveSource(saveIdentity, raw.SkillId, "active-direction"),
            warnings,
            warningCode: null);
        var activated = MapActivation(
            saveIdentity,
            raw,
            studyDetails,
            warnings);

        return new CharacterCombatSkillProgress(
            characterId,
            snapshot,
            raw.SkillId,
            learned,
            proficiency,
            studyDetails.Details,
            breakthrough,
            direction,
            SkillProgressField<bool>.Unavailable(
                "The save-derived attainment mastery rule is not verified "
                + "for this version.",
                VerifiedSource(raw.SkillId, "attainment-mastered")),
            SkillProgressField<bool>.Available(
                raw.Simplified,
                SaveSource(saveIdentity, raw.SkillId, "simplified")),
            activated,
            SkillProgressField<bool>.Available(
                raw.Equipped,
                SaveSource(saveIdentity, raw.SkillId, "equipped")));
    }

    private static CombatSkillProficiencyProgress MapProficiency(
        string saveIdentity,
        RawCharacterCombatSkillProgress raw,
        ICollection<CharacterCombatSkillProgressWarning> warnings)
    {
        SkillProgressField<int> current;
        if (raw.Proficiency is null)
        {
            current = SkillProgressField<int>.Unavailable(
                "The save contains no proficiency key for this skill.",
                SaveSource(saveIdentity, raw.SkillId, "proficiency"));
        }
        else if (raw.Proficiency is < 0
                 or > CombatSkillProficiencyProgress.MaximumSupportedValue)
        {
            var reason = $"Skill {raw.SkillId} has an out-of-range persisted "
                + $"proficiency value {raw.Proficiency}.";
            warnings.Add(new CharacterCombatSkillProgressWarning(
                "PROFICIENCY_OUT_OF_RANGE",
                reason));
            current = SkillProgressField<int>.Unavailable(
                reason,
                SaveSource(saveIdentity, raw.SkillId, "proficiency"));
        }
        else
        {
            current = SkillProgressField<int>.Available(
                raw.Proficiency.Value,
                SaveSource(saveIdentity, raw.SkillId, "proficiency"));
        }

        return new CombatSkillProficiencyProgress(
            current,
            SkillProgressField<int>.Available(
                CombatSkillProficiencyProgress.MaximumSupportedValue,
                VerifiedSource(raw.SkillId, "maximum-proficiency")),
            SkillProgressField<decimal>.Unavailable(
                "The conversion from persisted proficiency to the displayed "
                + "percentage is not verified.",
                VerifiedSource(raw.SkillId, "proficiency-percentage")));
    }

    private static SkillProgressField<bool> MapActivation(
        string saveIdentity,
        RawCharacterCombatSkillProgress raw,
        CombatSkillStudyDetailDecodeResult studyDetails,
        ICollection<CharacterCombatSkillProgressWarning> warnings)
    {
        var source = SaveSource(
            saveIdentity,
            raw.SkillId,
            "activation-state");
        if (studyDetails.IsVersionSupported
            && studyDetails.IsActivationStateSupported)
        {
            return SkillProgressField<bool>.Available(
                studyDetails.Details.Any(detail => detail.IsActive.Value),
                source);
        }

        var reason = studyDetails.UnavailableReason
            ?? $"Skill {raw.SkillId} has an unsupported activation state.";
        warnings.Add(new CharacterCombatSkillProgressWarning(
            "ACTIVATION_STATE_UNSUPPORTED",
            reason));
        return SkillProgressField<bool>.Unavailable(reason, source);
    }

    private static SkillProgressField<BreakthroughDirectionAvailability>
        MapBreakthrough(
            string saveIdentity,
            RawCharacterCombatSkillProgress raw,
            CombatSkillStudyDetailDecodeResult studyDetails,
            ICollection<CharacterCombatSkillProgressWarning> warnings)
    {
        var source = SaveSource(
            saveIdentity,
            raw.SkillId,
            "breakthrough");
        if (!studyDetails.IsVersionSupported
            || !studyDetails.IsReadingStateSupported
            || !studyDetails.IsActivationStateSupported)
        {
            return BreakthroughUnavailable(
                raw.SkillId,
                studyDetails.UnavailableReason,
                source,
                warnings);
        }

        if (CombatSkillStateHelper.IsBrokenOut((ushort)raw.ActivationState))
        {
            return SkillProgressField<BreakthroughDirectionAvailability>
                .Available(
                    new BreakthroughDirectionAvailability(
                        isBrokenOut: true,
                        canBreakthroughNow: false,
                        availableDirections: [],
                        completedDirections: CompletedDirections(raw)),
                    source);
        }

        if (!raw.MeetsBreakthroughReadingRequirement)
        {
            return SkillProgressField<BreakthroughDirectionAvailability>
                .Available(
                    new BreakthroughDirectionAvailability(
                        isBrokenOut: false,
                        canBreakthroughNow: false,
                        availableDirections: [],
                        completedDirections: CompletedDirections(raw)),
                    source);
        }

        var normalReadDetails = studyDetails.Details.Where(detail =>
            detail.Group is Domain.CombatSkills.CombatSkillStudyDetailGroup.Direct
                or Domain.CombatSkills.CombatSkillStudyDetailGroup.Reverse
            && detail.ReadState.Value == CombatSkillStudyState.Read);
        var directCount = normalReadDetails.Count(detail =>
            detail.Group
            == Domain.CombatSkills.CombatSkillStudyDetailGroup.Direct);
        var reverseCount = normalReadDetails.Count(detail =>
            detail.Group
            == Domain.CombatSkills.CombatSkillStudyDetailGroup.Reverse);
        if (directCount + reverseCount < 5)
        {
            return BreakthroughUnavailable(
                raw.SkillId,
                $"Skill {raw.SkillId} was reported as satisfying the "
                + "reading prerequisite, but its decoded details do not "
                + "contain the required five normal pages.",
                source,
                warnings);
        }

        List<PracticeDirection> directions = [];
        if (directCount >= 3)
        {
            directions.Add(PracticeDirection.Direct);
        }

        if (reverseCount >= 3)
        {
            directions.Add(PracticeDirection.Reverse);
        }

        return directions.Count == 0
            ? BreakthroughUnavailable(
                raw.SkillId,
                $"Skill {raw.SkillId} can break through, but its decoded "
                + "details do not produce a Direct or Reverse result.",
                source,
                warnings)
            : SkillProgressField<BreakthroughDirectionAvailability>.Available(
                new BreakthroughDirectionAvailability(
                    isBrokenOut: false,
                    canBreakthroughNow: true,
                    directions,
                    CompletedDirections(raw)),
                source);
    }

    private static IReadOnlyList<PracticeDirection> CompletedDirections(
        RawCharacterCombatSkillProgress raw)
    {
        List<PracticeDirection> directions = [];
        var currentDirection = raw.ActivationState is >= 0 and <= ushort.MaxValue
                               && CombatSkillStateHelper.IsBrokenOut(
                                   (ushort)raw.ActivationState)
            ? CombatSkillStateHelper.GetCombatSkillDirection(
                (ushort)raw.ActivationState)
            : -1;
        if (raw.DirectBreakthroughCompleted || currentDirection == 0)
        {
            directions.Add(PracticeDirection.Direct);
        }

        if (raw.ReverseBreakthroughCompleted || currentDirection == 1)
        {
            directions.Add(PracticeDirection.Reverse);
        }

        return directions;
    }

    private static SkillProgressField<BreakthroughDirectionAvailability>
        BreakthroughUnavailable(
            int skillId,
            string? reason,
            SkillProgressSource source,
            ICollection<CharacterCombatSkillProgressWarning> warnings)
    {
        var actualReason = reason
            ?? $"Skill {skillId} has unavailable decoded study details.";
        warnings.Add(new CharacterCombatSkillProgressWarning(
            "BREAKTHROUGH_STATE_UNAVAILABLE",
            actualReason));
        return SkillProgressField<BreakthroughDirectionAvailability>
            .Unavailable(actualReason, source);
    }

    private static SkillProgressField<T> MapSnapshotValue<T>(
        SnapshotValue<T> value,
        SkillProgressSource source,
        ICollection<CharacterCombatSkillProgressWarning> warnings,
        string? warningCode)
    {
        if (value.IsAvailable)
        {
            return SkillProgressField<T>.Available(value.Value, source);
        }

        var reason = value.UnavailableReason
            ?? "The save value could not be mapped.";
        if (warningCode is not null)
        {
            warnings.Add(new CharacterCombatSkillProgressWarning(
                warningCode,
                reason));
        }
        return SkillProgressField<T>.Unavailable(reason, source);
    }

    private static SkillProgressSource SaveSource(
        string saveIdentity,
        int skillId,
        string field) => new(
            SkillProgressSourceKind.SaveSnapshot,
            saveIdentity,
            $"combat-skill:{skillId}:{field}");

    private static SkillProgressSource VerifiedSource(
        int skillId,
        string field) => new(
            SkillProgressSourceKind.VerifiedRule,
            "verified-rule:e2-002",
            $"combat-skill:{skillId}:{field}");
}
