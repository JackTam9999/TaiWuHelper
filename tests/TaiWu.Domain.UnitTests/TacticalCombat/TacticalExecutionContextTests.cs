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
            TacticalContextFactState.Unsupported,
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
}
