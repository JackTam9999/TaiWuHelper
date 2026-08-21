using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Domain.UnitTests.TacticalCombat;

public sealed class TacticalExecutionContextTests
{
    private static readonly TacticalCombatRuleSet Rules =
        VerifiedTacticalCombatRuleSets.HistoricalMagicSound;

    [Fact]
    public void Projection_distinguishes_fixed_observed_and_runtime_facts()
    {
        var snapshot = Snapshot(
            DateTimeOffset.Parse("2026-08-20T10:00:00Z"),
            DateTimeOffset.Parse("2026-08-20T09:30:00Z"));

        var result = Project(snapshot);

        Assert.Equal([42], result.Current.EquippedWeaponTypeIds.Value);
        Assert.Equal(
            TacticalContextFactState.Unknown,
            result.Current.UnlockedWeaponTypeIds.State);
        Assert.Equal(
            TacticalContextFactState.Unknown,
            result.Current.UsableCombatStyleIds.State);
        Assert.Equal(
            TacticalContextAvailability.ManuallyObservable,
            result.Current.Distance.Availability);
        Assert.Equal(
            TacticalContextOrigin.CurrentScreenObservation,
            result.Current.SlotBudgets.Origin);
        Assert.Equal(
            TacticalContextOrigin.SaveSnapshot,
            result.Current.LegendaryCostSlots.Origin);
        Assert.True(result.Current.InnerPower.IsAvailable);
        Assert.Equal(7, result.Current.InnerPower.Value.StateId);
    }

    [Fact]
    public void Manual_observation_and_proposal_remain_distinct_complete_facts()
    {
        var observation = new TacticalExecutionObservation(
            "E8-F04-MANUAL-CURRENT",
            confirmsNewerThanSave: true,
            equippedWeaponTypeIds: [9],
            unlockedWeaponTypeIds: [6, 9],
            trickCounts: [new CombatTrickCount(7, 3)],
            usableCombatStyleIds: [4],
            distance: 5,
            resources:
            [
                Amount(CombatResourceKind.Stance, 100),
                Amount(CombatResourceKind.Breath, 80),
                Amount(CombatResourceKind.DefenseTrueQi, 3)
            ],
            activeDefenseSkillId: 604);
        var requirements = new CombatRequirementContext(
            equippedWeaponTypeIds: [77],
            trickCounts: [new CombatTrickCount(8, 2)],
            SnapshotValue<int>.Available(6),
            resources:
            [
                Amount(CombatResourceKind.Stance, 12),
                Amount(CombatResourceKind.Breath, 11)
            ],
            unlockedWeaponTypeIds: [77, 88],
            equippedSkillIds: [604],
            activeDefenseSkillId: 604);
        var proposal = new TacticalExecutionProposal(
            requirements,
            usableCombatStyleIds: [5]);

        var result = TacticalExecutionContextProjector.ProjectObserved(
            Snapshot(),
            Resolution(),
            observation,
            proposal,
            TestContext.Current.CancellationToken);

        Assert.Equal([9], result.Current.EquippedWeaponTypeIds.Value);
        Assert.Equal([6, 9], result.Current.UnlockedWeaponTypeIds.Value);
        Assert.Equal(3, Assert.Single(result.Current.TrickCounts.Value).Count);
        Assert.Equal([4], result.Current.UsableCombatStyleIds.Value);
        Assert.Equal(5, result.Current.Distance.Value);
        Assert.Equal(100, result.Current.Stance.Value);
        Assert.Equal(80, result.Current.Breath.Value);
        Assert.Equal(604, result.Current.ActiveDefenseSkillId.Value);
        Assert.Equal(
            TacticalContextOrigin.ManualConfirmation,
            result.Current.Distance.Origin);

        Assert.Equal([77], result.Proposed.EquippedWeaponTypeIds.Value);
        Assert.Equal(2, Assert.Single(result.Proposed.TrickCounts.Value).Count);
        Assert.Equal([5], result.Proposed.UsableCombatStyleIds.Value);
        Assert.Equal(6, result.Proposed.Distance.Value);
        Assert.Equal(12, result.Proposed.Stance.Value);
        Assert.Equal(11, result.Proposed.Breath.Value);
        Assert.Equal(
            TacticalContextOrigin.ProposedPlan,
            result.Proposed.Distance.Origin);
    }

    [Fact]
    public void Partial_observation_preserves_save_fallback_and_unknown_live_facts()
    {
        var result = TacticalExecutionContextProjector.ProjectCurrentLoadout(
            Snapshot(),
            Resolution(),
            new TacticalExecutionObservation(
                "E8-F04-DISTANCE-ONLY",
                confirmsNewerThanSave: true,
                distance: 5),
            TestContext.Current.CancellationToken);

        Assert.Equal([42], result.Current.EquippedWeaponTypeIds.Value);
        Assert.Equal(5, result.Current.Distance.Value);
        Assert.False(result.Current.UnlockedWeaponTypeIds.IsAvailable);
        Assert.False(result.Current.TrickCounts.IsAvailable);
        Assert.False(result.Current.Stance.IsAvailable);
        Assert.Same(result.Current.Distance, result.Proposed.Distance);
    }

    [Fact]
    public void Observed_active_skill_conflicts_with_the_same_revision_loadout()
    {
        var result = TacticalExecutionContextProjector.ProjectCurrentLoadout(
            Snapshot(),
            Resolution(),
            new TacticalExecutionObservation(
                "E8-F04-CONFLICTING-ACTIVE",
                confirmsNewerThanSave: true,
                activeAgilitySkillId: 134),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TacticalContextFactState.Conflicting,
            result.Current.ActiveAgilitySkillId.State);
        Assert.Equal(
            "ACTIVE_AGILITY_NOT_IN_CURRENT_LOADOUT",
            result.Current.ActiveAgilitySkillId.ReasonIdentity);
    }

    [Fact]
    public void Unconfirmed_manual_observation_cannot_override_the_save()
    {
        var observation = new TacticalExecutionObservation(
            "E8-F04-STALE",
            confirmsNewerThanSave: false,
            distance: 5);

        var exception = Assert.Throws<ArgumentException>(() =>
            TacticalExecutionContextProjector.ProjectCurrentLoadout(
                Snapshot(),
                Resolution(),
                observation,
                TestContext.Current.CancellationToken));

        Assert.Contains("newer-than-save confirmation", exception.Message);
    }

    [Fact]
    public void Proposal_is_explicit_and_does_not_mutate_current_facts()
    {
        var requirements = new CombatRequirementContext(
            equippedWeaponTypeIds: [77],
            trickCounts: [],
            SnapshotValue<int>.Available(6),
            resources:
            [
                new CombatResourceAmount(
                    CombatResourceKind.Stance,
                    SnapshotValue<int>.Available(12))
            ],
            unlockedWeaponTypeIds: [77, 88],
            equippedSkillIds: [604],
            activeDefenseSkillId: 604);
        var proposal = new TacticalExecutionProposal(requirements);

        var result = Project(Snapshot(), proposal);

        Assert.Equal([42], result.Current.EquippedWeaponTypeIds.Value);
        Assert.Equal([77], result.Proposed.EquippedWeaponTypeIds.Value);
        Assert.Equal(
            TacticalContextOrigin.ProposedPlan,
            result.Proposed.EquippedWeaponTypeIds.Origin);
        Assert.Equal(6, result.Proposed.Distance.Value);
        Assert.Equal(604, result.Proposed.ActiveDefenseSkillId.Value);
        Assert.Equal(
            TacticalContextFactState.Unknown,
            result.Proposed.ActiveAgilitySkillId.State);
        Assert.Equal(
            TacticalContextFactState.Unknown,
            result.Proposed.SlotBudgets.State);
    }

    [Fact]
    public void Missing_proposal_values_never_become_empty_or_zero_facts()
    {
        var result = Project(Snapshot());

        Assert.False(result.Proposed.EquippedWeaponTypeIds.IsAvailable);
        Assert.False(result.Proposed.Resources.IsAvailable);
        Assert.False(result.Proposed.ActiveDefenseSkillId.IsAvailable);
        Assert.False(result.Proposed.SlotBudgets.IsAvailable);
        Assert.Throws<InvalidOperationException>(
            () => result.Proposed.Distance.Value);
    }

    [Fact]
    public void Explicit_alternative_budget_and_universal_allocation_are_preserved()
    {
        var budgets = new SlotBudgetSet(
        [
            new SlotBudget(SkillCategory.Neigong, 0, 6),
            new SlotBudget(SkillCategory.Attack, 0, 11),
            new SlotBudget(SkillCategory.Agility, 0, 7),
            new SlotBudget(SkillCategory.Defense, 0, 8),
            new SlotBudget(SkillCategory.Assistance, 0, 5)
        ]);
        var allocation = new GenericSlotAllocation(8, 2, 2, 2, 2);
        var proposal = new TacticalExecutionProposal(
            new CombatRequirementContext(
                equippedWeaponTypeIds: [],
                trickCounts: [],
                SnapshotValue<int>.Unavailable("Opening range undecided."),
                resources: [],
                unlockedWeaponTypeIds: [],
                equippedSkillIds: []),
            budgets,
            allocation,
            legendaryCostAssignments: []);

        var result = Project(Snapshot(), proposal);

        Assert.Same(budgets, result.Proposed.SlotBudgets.Value);
        Assert.Same(allocation, result.Proposed.UniversalSlotAllocation.Value);
        Assert.Equal(
            TacticalContextOrigin.ProposedPlan,
            result.Proposed.SlotBudgets.Origin);
    }

    [Fact]
    public void Current_loadout_baseline_copies_only_captured_facts()
    {
        var snapshot = Snapshot();
        var result = TacticalExecutionContextProjector.ProjectCurrentLoadout(
            snapshot,
            Resolution(),
            TestContext.Current.CancellationToken);

        Assert.Equal([42], result.Proposed.EquippedWeaponTypeIds.Value);
        Assert.Equal([604], result.Proposed.EquippedSkillIds.Value);
        Assert.Same(
            result.Current.SlotBudgets,
            result.Proposed.SlotBudgets);
        Assert.Same(
            result.Current.UniversalSlotAllocation,
            result.Proposed.UniversalSlotAllocation);
        Assert.Same(
            result.Current.LegendaryCostAssignments,
            result.Proposed.LegendaryCostAssignments);
        Assert.False(result.Proposed.UnlockedWeaponTypeIds.IsAvailable);
        Assert.False(result.Proposed.Resources.IsAvailable);
        Assert.False(result.Proposed.ActiveDefenseSkillId.IsAvailable);
        Assert.Equal(
            TacticalContextOrigin.SaveSnapshot,
            result.Proposed.EquippedWeaponTypeIds.Origin);
    }

    [Fact]
    public void Capture_times_do_not_change_semantic_or_observation_revision()
    {
        var first = Project(Snapshot(
            DateTimeOffset.Parse("2026-08-20T10:00:00Z"),
            DateTimeOffset.Parse("2026-08-20T09:30:00Z")));
        var second = Project(Snapshot(
            DateTimeOffset.Parse("2026-08-20T11:00:00Z"),
            DateTimeOffset.Parse("2026-08-20T10:30:00Z")));

        Assert.Equal(
            first.ObservationRevisionFingerprint,
            second.ObservationRevisionFingerprint);
        Assert.Equal(first.SemanticFingerprint, second.SemanticFingerprint);
    }

    [Fact]
    public void Proposed_semantic_change_changes_fingerprint()
    {
        var first = Project(Snapshot(), ProposalAtDistance(4));
        var second = Project(Snapshot(), ProposalAtDistance(5));

        Assert.NotEqual(first.SemanticFingerprint, second.SemanticFingerprint);
    }

    [Fact]
    public void Unsupported_GameData_version_exposes_no_stale_rules()
    {
        var snapshot = Snapshot(gameDataVersion: "1.0.0+current");
        var resolution = Rules.Resolve(
            "1.0.0+current",
            Rules.SupportedTargetGoalCodes,
            []);

        var result = TacticalExecutionContextProjector.Project(
            snapshot,
            resolution,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.HasCompatibleRules);
        Assert.Empty(result.ResolvedRules);
        Assert.Equal("1.0.0+current", result.GameDataVersion.Value);
    }

    [Fact]
    public void Projection_rejects_a_rule_resolution_for_another_snapshot()
    {
        var resolution = Rules.Resolve(
            "1.0.0+other",
            Rules.SupportedTargetGoalCodes,
            []);

        Assert.Throws<ArgumentException>(
            () => TacticalExecutionContextProjector.Project(
                Snapshot(),
                resolution,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Projection_observes_pre_cancelled_token()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => TacticalExecutionContextProjector.Project(
                Snapshot(),
                Resolution(),
                cancellationToken: cancellation.Token));
    }

    [Fact]
    public void Conflicting_fact_requires_multiple_evidence_identities()
    {
        Assert.Throws<ArgumentException>(() =>
            TacticalContextFact<int>.Unavailable(
                TacticalContextFactState.Conflicting,
                TacticalContextOrigin.CurrentScreenObservation,
                TacticalContextAvailability.ManuallyObservable,
                "CONFLICTING_DISTANCE",
                "OBSERVATION_A"));

        var conflict = TacticalContextFact<int>.Unavailable(
            TacticalContextFactState.Conflicting,
            TacticalContextOrigin.CurrentScreenObservation,
            TacticalContextAvailability.ManuallyObservable,
            "CONFLICTING_DISTANCE",
            "OBSERVATION_A",
            "OBSERVATION_B");

        Assert.Equal(TacticalContextFactState.Conflicting, conflict.State);
        Assert.False(conflict.IsAvailable);
    }

    private static TacticalExecutionContext Project(
        CombatSnapshot snapshot,
        TacticalExecutionProposal? proposal = null) =>
        TacticalExecutionContextProjector.Project(
            snapshot,
            Resolution(),
            proposal,
            TestContext.Current.CancellationToken);

    private static TacticalCombatRuleResolution Resolution() => Rules.Resolve(
        VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
        Rules.SupportedTargetGoalCodes,
        []);

    private static TacticalExecutionProposal ProposalAtDistance(int distance) =>
        new(new CombatRequirementContext(
            equippedWeaponTypeIds: [],
            trickCounts: [],
            SnapshotValue<int>.Available(distance),
            resources: [],
            unlockedWeaponTypeIds: [],
            equippedSkillIds: []));

    private static CombatSnapshot Snapshot(
        DateTimeOffset? capturedAt = null,
        DateTimeOffset? observedAt = null,
        string? gameDataVersion = null)
    {
        var capture = capturedAt
            ?? DateTimeOffset.Parse("2026-08-20T10:00:00Z");
        var observation = observedAt
            ?? DateTimeOffset.Parse("2026-08-20T09:30:00Z");
        var skill = new CombatSkillSnapshot(
            604,
            SnapshotValue<string>.Available("display-only skill"),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(3),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Reverse),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(338),
            SnapshotValue<int>.Available(1064));
        var budgets = Budgets();
        var player = new PlayerCombatSnapshot(
            1,
            SnapshotValue<string>.Available("display-only player"),
            [skill],
            new CombatLoadoutSnapshot([], [604], [], [], []),
            [
                new EquipmentSnapshot(
                    0,
                    SnapshotValue<long>.Available(9001),
                    SnapshotValue<int>.Available(100),
                    SnapshotValue<string>.Available("display-only weapon"),
                    SnapshotValue<EquipmentKind>.Available(
                        EquipmentKind.Weapon),
                    SnapshotValue<int>.Available(42))
            ],
            budgets,
            new GenericSlotAllocation(2, 1, 1, 0, 0),
            legendaryBookCostSlots: [],
            legendaryBookCostAssignments: [],
            SnapshotValue<InnerPowerStateSnapshot>.Available(
                new InnerPowerStateSnapshot(
                    7,
                    SnapshotValue<string>.Available("display-only inner"),
                    SnapshotValue<string>.Available("proprietary description"),
                    new ElementAdjustmentSet(1, 2, 3, 4, 5),
                    ElementAdjustmentSet.None,
                    CombatSkillElement.Fire)));
        var target = new TargetCombatSnapshot(
            2,
            SnapshotValue<string>.Available("display-only target"),
            SnapshotValue<int>.Available(52),
            features: [],
            learnedSkills: [],
            SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                "Target loadout is not required."),
            equipment: []);

        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                new string('A', 64),
                capture,
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-08-20T09:00:00Z")),
                SnapshotValue<string>.Available(
                    gameDataVersion
                    ?? VerifiedTacticalCombatRuleSets
                        .HistoricalGameDataVersion)),
            player,
            target,
            warnings: [],
            [
                new SnapshotFieldSource(
                    CombatSnapshotObservationMerger.PlayerSlotBudgetsField,
                    SnapshotDataSource.CurrentScreenObservation,
                    observation,
                    "sha256:observation"),
                new SnapshotFieldSource(
                    CombatSnapshotObservationMerger
                        .PlayerGenericSlotAllocationField,
                    SnapshotDataSource.Save,
                    capture,
                    "save:generic-slots")
            ]);
    }

    private static SlotBudgetSet Budgets() => new(
    [
        new SlotBudget(SkillCategory.Neigong, 0, 6),
        new SlotBudget(SkillCategory.Attack, 3, 10),
        new SlotBudget(SkillCategory.Agility, 0, 8),
        new SlotBudget(SkillCategory.Defense, 0, 8),
        new SlotBudget(SkillCategory.Assistance, 0, 2)
    ]);

    private static CombatResourceAmount Amount(
        CombatResourceKind kind,
        int value) => new(kind, SnapshotValue<int>.Available(value));
}
