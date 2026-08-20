using NSubstitute;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Application.UnitTests.TacticalCombat;

public sealed class DiscoverTacticalCandidatesTests
{
    [Fact]
    public async Task Execute_reuses_one_snapshot_for_context_and_discovery()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var snapshot = Snapshot();
        var snapshotRequest = new CombatSnapshotReadRequest("local.sav", 2);
        var request = new TacticalCandidateDiscoveryReadRequest(
            new TacticalExecutionContextReadRequest(
                snapshotRequest,
                VerifiedTacticalCombatRuleSets.HistoricalMagicSound
                    .SupportedTargetGoalCodes,
                evidence: [],
                Proposal(snapshot.Player.LearnedSkills.Select(
                    item => item.SkillId))));
        var cancellationToken = TestContext.Current.CancellationToken;
        reader.ReadAsync(snapshotRequest, cancellationToken).Returns(snapshot);

        var result = await new DiscoverTacticalCandidates(reader)
            .ExecuteAsync(request, cancellationToken);

        Assert.Equal(
            snapshot.Metadata.SaveSha256,
            result.Context.Context.SourceRevisionFingerprint);
        Assert.Equal(2, result.Discovery.Entries.Length);
        Assert.Equal(7, result.Discovery.SupportedRoleCount);
        Assert.Equal(1, result.Discovery.ConsideredVerifiedRoleCount);
        await reader.Received(1).ReadAsync(snapshotRequest, cancellationToken);
    }

    [Fact]
    public async Task Execute_does_not_read_when_pre_cancelled()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var request = new TacticalCandidateDiscoveryReadRequest(
            new TacticalExecutionContextReadRequest(
                new CombatSnapshotReadRequest("local.sav", 2),
                VerifiedTacticalCombatRuleSets.HistoricalMagicSound
                    .SupportedTargetGoalCodes,
                evidence: []));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new DiscoverTacticalCandidates(reader).ExecuteAsync(
                request,
                cancellation.Token));

        await reader.DidNotReceive().ReadAsync(
            Arg.Any<CombatSnapshotReadRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Request_preserves_explicit_discovery_limits()
    {
        var limits = new TacticalCandidateDiscoveryLimits(100, 3);
        var request = new TacticalCandidateDiscoveryReadRequest(
            new TacticalExecutionContextReadRequest(
                new CombatSnapshotReadRequest("local.sav", 2),
                VerifiedTacticalCombatRuleSets.HistoricalMagicSound
                    .SupportedTargetGoalCodes,
                evidence: []),
            limits);

        Assert.Same(limits, request.Limits);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TacticalCandidateDiscoveryLimits(0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TacticalCandidateDiscoveryLimits(100, 21));
    }

    private static TacticalExecutionProposal Proposal(
        IEnumerable<int> equippedSkillIds) => new(
        new CombatRequirementContext(
            equippedWeaponTypeIds: [],
            trickCounts: [],
            SnapshotValue<int>.Available(5),
            resources: [],
            unlockedWeaponTypeIds: [],
            equippedSkillIds),
        Budgets(),
        new GenericSlotAllocation(2, 1, 1, 0, 0),
        legendaryCostAssignments: []);

    private static CombatSnapshot Snapshot()
    {
        var skill = new CombatSkillSnapshot(
            604,
            SnapshotValue<string>.Available("display-only"),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(3),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Reverse),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(338),
            SnapshotValue<int>.Available(1064),
            element: SnapshotValue<CombatSkillElement>.Available(
                CombatSkillElement.Water));
        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                new string('D', 64),
                DateTimeOffset.Parse("2026-08-20T12:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-08-20T11:00:00Z")),
                SnapshotValue<string>.Available(
                    VerifiedTacticalCombatRuleSets
                        .HistoricalGameDataVersion)),
            new PlayerCombatSnapshot(
                1,
                SnapshotValue<string>.Unavailable("Not required."),
                [skill],
                new CombatLoadoutSnapshot([], [], [], [], []),
                equipment: [],
                Budgets(),
                new GenericSlotAllocation(2, 1, 1, 0, 0),
                legendaryBookCostSlots: [],
                legendaryBookCostAssignments: [],
                SnapshotValue<InnerPowerStateSnapshot>.Available(
                    new InnerPowerStateSnapshot(
                        1,
                        SnapshotValue<string>.Unavailable("Not required."),
                        SnapshotValue<string>.Unavailable("Not required."),
                        ElementAdjustmentSet.None,
                        ElementAdjustmentSet.None,
                        CombatSkillElement.Fire))),
            new TargetCombatSnapshot(
                2,
                SnapshotValue<string>.Unavailable("Not required."),
                SnapshotValue<int>.Unavailable("Not required."),
                features: [],
                learnedSkills: [],
                SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                    "Not required."),
                equipment: []),
            warnings: []);
    }

    private static SlotBudgetSet Budgets() => new(
    [
        new SlotBudget(SkillCategory.Neigong, 0, 6),
        new SlotBudget(SkillCategory.Attack, 0, 10),
        new SlotBudget(SkillCategory.Agility, 0, 8),
        new SlotBudget(SkillCategory.Defense, 0, 8),
        new SlotBudget(SkillCategory.Assistance, 0, 2)
    ]);
}
