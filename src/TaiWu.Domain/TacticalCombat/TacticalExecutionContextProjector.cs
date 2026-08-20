using System.Collections.Immutable;
using System.Text;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public static class TacticalExecutionContextProjector
{
    private const string SaveEvidence = "SAVE_SNAPSHOT";
    private const string ObservationEvidence = "CURRENT_SCREEN_OBSERVATION";
    private const string ProposalEvidence = "PROPOSED_PLAN";
    private const string RuntimeEvidence = "RUNTIME_NOT_CAPTURED";
    private const string ConfigurationEvidence = "INSTALLED_CONFIGURATION";

    public static TacticalExecutionContext Project(
        CombatSnapshot snapshot,
        TacticalCombatRuleResolution ruleResolution,
        TacticalExecutionProposal? proposal = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(ruleResolution);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateVersion(snapshot, ruleResolution);
        var observationFingerprint = ObservationFingerprint(
            snapshot.FieldSources,
            cancellationToken);
        var current = ProjectCurrent(snapshot, cancellationToken);
        var proposed = ProjectProposed(
            current,
            proposal,
            cancellationToken);
        var resolvedRules = ProjectRules(ruleResolution, cancellationToken);
        return new TacticalExecutionContext(
            snapshot.Metadata.SaveSha256,
            observationFingerprint,
            GameDataVersion(snapshot.Metadata.GameDataVersion),
            ruleResolution.RuleSetFingerprint,
            ruleResolution.Status,
            resolvedRules,
            current,
            proposed);
    }

    private static CurrentTacticalExecutionFacts ProjectCurrent(
        CombatSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var weaponTypes = CurrentWeaponTypes(
            snapshot.Player.Equipment,
            cancellationToken);
        var equippedSkills = Enum.GetValues<SkillCategory>()
            .SelectMany(category =>
                snapshot.Player.EquippedSkills.Get(category))
            .Order()
            .ToImmutableArray();
        cancellationToken.ThrowIfCancellationRequested();

        var innerPower = snapshot.Player.InnerPowerState.IsAvailable
            ? TacticalContextFact<TacticalInnerPowerContext>.Available(
                Strip(snapshot.Player.InnerPowerState.Value),
                TacticalContextOrigin.SaveSnapshot,
                TacticalContextAvailability.FixedForRequest,
                "INNER_POWER_MECHANICS_CAPTURED",
                SaveEvidence)
            : Unknown<TacticalInnerPowerContext>(
                "INNER_POWER_SOURCE_UNAVAILABLE",
                TacticalContextAvailability.FixedForRequest,
                SaveEvidence);

        return new CurrentTacticalExecutionFacts(
            weaponTypes,
            Unknown<ImmutableArray<int>>(
                "UNLOCKED_WEAPON_TYPES_NOT_CAPTURED",
                TacticalContextAvailability.PreCombatConfigurable,
                RuntimeEvidence),
            Unsupported<ImmutableArray<int>>(
                "USABLE_COMBAT_STYLES_NOT_SUPPORTED",
                RuntimeEvidence),
            RuntimeUnknown<int>("LIVE_DISTANCE_NOT_CAPTURED"),
            RuntimeUnknown<int>("LIVE_STANCE_NOT_CAPTURED"),
            RuntimeUnknown<int>("LIVE_BREATH_NOT_CAPTURED"),
            RuntimeUnknown<ImmutableArray<CombatResourceAmount>>(
                "LIVE_RESOURCES_NOT_CAPTURED"),
            RuntimeUnknown<int>("ACTIVE_DEFENSE_ROLE_NOT_CAPTURED"),
            RuntimeUnknown<int>("ACTIVE_AGILITY_ROLE_NOT_CAPTURED"),
            innerPower,
            AvailableFromSnapshot(
                snapshot,
                CombatSnapshotObservationMerger.PlayerSlotBudgetsField,
                snapshot.Player.SlotBudgets,
                "SLOT_BUDGETS_CAPTURED"),
            AvailableFromSnapshot(
                snapshot,
                CombatSnapshotObservationMerger
                    .PlayerGenericSlotAllocationField,
                snapshot.Player.GenericSlotAllocation,
                "UNIVERSAL_SLOT_ALLOCATION_CAPTURED"),
            AvailableFromSnapshot(
                snapshot,
                CombatSnapshotObservationMerger
                    .PlayerLegendaryBookCostSlotsField,
                snapshot.Player.LegendaryBookCostSlots,
                "LEGENDARY_COST_SLOTS_CAPTURED"),
            AvailableFromSnapshot(
                snapshot,
                CombatSnapshotObservationMerger
                    .PlayerLegendaryBookCostAssignmentsField,
                snapshot.Player.LegendaryBookCostAssignments,
                "LEGENDARY_COST_ASSIGNMENTS_CAPTURED"),
            AvailableFromSnapshot(
                snapshot,
                CombatSnapshotObservationMerger.PlayerEquippedSkillsField,
                equippedSkills,
                "EQUIPPED_SKILLS_CAPTURED"));
    }

    private static ProposedTacticalExecutionFacts ProjectProposed(
        CurrentTacticalExecutionFacts current,
        TacticalExecutionProposal? proposal,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (proposal is null)
        {
            return new ProposedTacticalExecutionFacts(
                ProposalUnknown<ImmutableArray<int>>(
                    "PROPOSED_WEAPON_TYPES_NOT_SUPPLIED"),
                ProposalUnknown<ImmutableArray<int>>(
                    "PROPOSED_UNLOCKED_WEAPON_TYPES_NOT_SUPPLIED"),
                Unsupported<ImmutableArray<int>>(
                    "USABLE_COMBAT_STYLES_NOT_SUPPORTED",
                    RuntimeEvidence),
                ProposalUnknown<int>("PROPOSED_DISTANCE_NOT_SUPPLIED"),
                RuntimeUnknown<int>("LIVE_STANCE_NOT_CAPTURED"),
                RuntimeUnknown<int>("LIVE_BREATH_NOT_CAPTURED"),
                ProposalUnknown<ImmutableArray<CombatResourceAmount>>(
                    "PROPOSED_RESOURCES_NOT_SUPPLIED"),
                ProposalUnknown<int>(
                    "PROPOSED_ACTIVE_DEFENSE_NOT_SUPPLIED"),
                ProposalUnknown<int>(
                    "PROPOSED_ACTIVE_AGILITY_NOT_SUPPLIED"),
                current.InnerPower,
                ProposalUnknown<SlotBudgetSet>(
                    "PROPOSED_SLOT_BUDGETS_NOT_SUPPLIED"),
                ProposalUnknown<GenericSlotAllocation>(
                    "PROPOSED_UNIVERSAL_SLOTS_NOT_SUPPLIED"),
                current.LegendaryCostSlots,
                ProposalUnknown<ImmutableArray<LegendaryBookCostAssignment>>(
                    "PROPOSED_LEGENDARY_ASSIGNMENTS_NOT_SUPPLIED"),
                ProposalUnknown<ImmutableArray<int>>(
                    "PROPOSED_EQUIPPED_SKILLS_NOT_SUPPLIED"));
        }

        var requirements = proposal.RequirementContext;
        var resourceValues = requirements.Resources
            .OrderBy(item => item.Key)
            .Select(item => new CombatResourceAmount(item.Key, item.Value))
            .ToImmutableArray();
        cancellationToken.ThrowIfCancellationRequested();

        var resources = resourceValues.IsEmpty
            || resourceValues.Any(item => !item.Amount.IsAvailable)
            ? ProposalUnknown<ImmutableArray<CombatResourceAmount>>(
                "PROPOSED_RESOURCES_INCOMPLETE")
            : Proposed(resourceValues, "PROPOSED_RESOURCES_SUPPLIED");

        return new ProposedTacticalExecutionFacts(
            Proposed(
                requirements.EquippedWeaponTypeIds.Order()
                    .ToImmutableArray(),
                "PROPOSED_WEAPON_TYPES_SUPPLIED"),
            Proposed(
                requirements.UnlockedWeaponTypeIds.Order()
                    .ToImmutableArray(),
                "PROPOSED_UNLOCKED_WEAPON_TYPES_SUPPLIED"),
            Unsupported<ImmutableArray<int>>(
                "USABLE_COMBAT_STYLES_NOT_SUPPORTED",
                RuntimeEvidence),
            requirements.Distance.IsAvailable
                ? Proposed(
                    requirements.Distance.Value,
                    "PROPOSED_DISTANCE_SUPPLIED")
                : ProposalUnknown<int>("PROPOSED_DISTANCE_NOT_SUPPLIED"),
            RuntimeUnknown<int>("LIVE_STANCE_NOT_CAPTURED"),
            RuntimeUnknown<int>("LIVE_BREATH_NOT_CAPTURED"),
            resources,
            requirements.ActiveDefenseSkillId.HasValue
                ? Proposed(
                    requirements.ActiveDefenseSkillId.Value,
                    "PROPOSED_ACTIVE_DEFENSE_SUPPLIED")
                : ProposalUnknown<int>(
                    "PROPOSED_ACTIVE_DEFENSE_NOT_SUPPLIED"),
            requirements.ActiveAgilitySkillId.HasValue
                ? Proposed(
                    requirements.ActiveAgilitySkillId.Value,
                    "PROPOSED_ACTIVE_AGILITY_SUPPLIED")
                : ProposalUnknown<int>(
                    "PROPOSED_ACTIVE_AGILITY_NOT_SUPPLIED"),
            current.InnerPower,
            proposal.SlotBudgets is null
                ? ProposalUnknown<SlotBudgetSet>(
                    "PROPOSED_SLOT_BUDGETS_NOT_SUPPLIED")
                : Proposed(
                    proposal.SlotBudgets,
                    "PROPOSED_SLOT_BUDGETS_SUPPLIED"),
            proposal.UniversalSlotAllocation is null
                ? ProposalUnknown<GenericSlotAllocation>(
                    "PROPOSED_UNIVERSAL_SLOTS_NOT_SUPPLIED")
                : Proposed(
                    proposal.UniversalSlotAllocation,
                    "PROPOSED_UNIVERSAL_SLOTS_SUPPLIED"),
            current.LegendaryCostSlots,
            proposal.HasLegendaryCostAssignments
                ? Proposed(
                    proposal.LegendaryCostAssignments,
                    "PROPOSED_LEGENDARY_ASSIGNMENTS_SUPPLIED")
                : ProposalUnknown<ImmutableArray<LegendaryBookCostAssignment>>(
                    "PROPOSED_LEGENDARY_ASSIGNMENTS_NOT_SUPPLIED"),
            Proposed(
                requirements.EquippedSkillIds.Order().ToImmutableArray(),
                "PROPOSED_EQUIPPED_SKILLS_SUPPLIED"));
    }

    private static ImmutableArray<TacticalResolvedRuleState> ProjectRules(
        TacticalCombatRuleResolution resolution,
        CancellationToken cancellationToken)
    {
        if (!resolution.IsResolved)
        {
            return [];
        }

        List<TacticalResolvedRuleState> values = [];
        foreach (var match in resolution.Transitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            values.Add(new TacticalResolvedRuleState(
                TacticalResolvedRuleKind.Transition,
                match.Rule.Identity.Code,
                match.Applicability,
                match.UnmetEvidence));
        }

        foreach (var match in resolution.Roles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            values.Add(new TacticalResolvedRuleState(
                TacticalResolvedRuleKind.SkillRole,
                match.Rule.Identity.Code,
                match.Applicability,
                match.UnmetEvidence));
        }

        return [.. values.OrderBy(item => item.StableKey, StringComparer.Ordinal)];
    }

    private static TacticalContextFact<ImmutableArray<int>>
        CurrentWeaponTypes(
            ImmutableArray<EquipmentSnapshot> equipment,
            CancellationToken cancellationToken)
    {
        List<int> values = [];
        foreach (var item in equipment)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!item.Kind.IsAvailable)
            {
                return Unknown<ImmutableArray<int>>(
                    "EQUIPMENT_KIND_INCOMPLETE",
                    TacticalContextAvailability.FixedForRequest,
                    SaveEvidence);
            }

            if (item.Kind.Value != EquipmentKind.Weapon)
            {
                continue;
            }

            if (!item.ItemSubtype.IsAvailable)
            {
                return Unknown<ImmutableArray<int>>(
                    "WEAPON_TYPE_INCOMPLETE",
                    TacticalContextAvailability.FixedForRequest,
                    SaveEvidence);
            }

            values.Add(item.ItemSubtype.Value);
        }

        return TacticalContextFact<ImmutableArray<int>>.Available(
            [.. values.Distinct().Order()],
            TacticalContextOrigin.SaveSnapshot,
            TacticalContextAvailability.FixedForRequest,
            "EQUIPPED_WEAPON_TYPES_CAPTURED",
            SaveEvidence);
    }

    private static TacticalInnerPowerContext Strip(
        InnerPowerStateSnapshot value) => new(
        value.StateId,
        value.MaxPowerChanges,
        value.RequirementChanges,
        value.BacklashOnUseElement);

    private static TacticalContextFact<string> GameDataVersion(
        SnapshotValue<string> version) => version.IsAvailable
        ? TacticalContextFact<string>.Available(
            version.Value,
            TacticalContextOrigin.InstalledConfiguration,
            TacticalContextAvailability.FixedForRequest,
            "GAMEDATA_VERSION_CAPTURED",
            ConfigurationEvidence)
        : TacticalContextFact<string>.Unavailable(
            TacticalContextFactState.Unknown,
            TacticalContextOrigin.InstalledConfiguration,
            TacticalContextAvailability.FixedForRequest,
            "GAMEDATA_VERSION_UNAVAILABLE",
            ConfigurationEvidence);

    private static TacticalContextFact<T> AvailableFromSnapshot<T>(
        CombatSnapshot snapshot,
        string fieldPath,
        T value,
        string reason)
    {
        var source = snapshot.FieldSources.SingleOrDefault(item =>
            string.Equals(item.FieldPath, fieldPath, StringComparison.Ordinal));
        var observed = source?.Source
            == SnapshotDataSource.CurrentScreenObservation;
        return TacticalContextFact<T>.Available(
            value,
            observed
                ? TacticalContextOrigin.CurrentScreenObservation
                : TacticalContextOrigin.SaveSnapshot,
            TacticalContextAvailability.FixedForRequest,
            reason,
            observed ? ObservationEvidence : SaveEvidence);
    }

    private static TacticalContextFact<T> Proposed<T>(
        T value,
        string reason) => TacticalContextFact<T>.Available(
        value,
        TacticalContextOrigin.ProposedPlan,
        TacticalContextAvailability.PreCombatConfigurable,
        reason,
        ProposalEvidence);

    private static TacticalContextFact<T> ProposalUnknown<T>(string reason) =>
        TacticalContextFact<T>.Unavailable(
            TacticalContextFactState.Unknown,
            TacticalContextOrigin.ProposedPlan,
            TacticalContextAvailability.PreCombatConfigurable,
            reason,
            ProposalEvidence);

    private static TacticalContextFact<T> RuntimeUnknown<T>(string reason) =>
        TacticalContextFact<T>.Unavailable(
            TacticalContextFactState.Unknown,
            TacticalContextOrigin.RuntimeUnavailable,
            TacticalContextAvailability.ManuallyObservable,
            reason,
            RuntimeEvidence);

    private static TacticalContextFact<T> Unknown<T>(
        string reason,
        TacticalContextAvailability availability,
        string evidence) => TacticalContextFact<T>.Unavailable(
        TacticalContextFactState.Unknown,
        evidence == RuntimeEvidence
            ? TacticalContextOrigin.ManualConfirmation
            : TacticalContextOrigin.SaveSnapshot,
        availability,
        reason,
        evidence);

    private static TacticalContextFact<T> Unsupported<T>(
        string reason,
        string evidence) => TacticalContextFact<T>.Unavailable(
        TacticalContextFactState.Unsupported,
        TacticalContextOrigin.RuntimeUnavailable,
        TacticalContextAvailability.RuntimeUnavailable,
        reason,
        evidence);

    private static string ObservationFingerprint(
        ImmutableArray<SnapshotFieldSource> sources,
        CancellationToken cancellationToken)
    {
        var canonical = new StringBuilder("TACTICAL_OBSERVATION_REVISION_V1\n");
        foreach (var source in sources.OrderBy(
            item => item.FieldPath,
            StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            canonical.Append(source.FieldPath).Append('|')
                .Append(source.Source).Append('|')
                .Append(source.EvidenceReference).Append('\n');
        }

        return TacticalCombatText.Fingerprint(canonical.ToString());
    }

    private static void ValidateVersion(
        CombatSnapshot snapshot,
        TacticalCombatRuleResolution resolution)
    {
        var expected = snapshot.Metadata.GameDataVersion.IsAvailable
            ? snapshot.Metadata.GameDataVersion.Value
            : TacticalContextGameDataVersions.Unavailable;
        if (!string.Equals(
            expected,
            resolution.GameDataVersion,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The tactical rule resolution must use the snapshot GameData version.",
                nameof(resolution));
        }
    }
}

public static class TacticalContextGameDataVersions
{
    public const string Unavailable = "UNAVAILABLE";
}
