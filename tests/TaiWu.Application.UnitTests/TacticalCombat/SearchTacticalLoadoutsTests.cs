using NSubstitute;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Application.UnitTests.TacticalCombat;

public sealed class SearchTacticalLoadoutsTests
{
    private static readonly TacticalCombatRuleSet Rules =
        VerifiedTacticalCombatRuleSets.HistoricalMagicSound;

    [Fact]
    public async Task Execute_reuses_one_snapshot_through_projection_discovery_and_search()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var snapshot = Snapshot();
        var snapshotRequest = new CombatSnapshotReadRequest("local.sav", 2);
        var request = Request(snapshotRequest, snapshot);
        var cancellationToken = TestContext.Current.CancellationToken;
        reader.ReadAsync(snapshotRequest, cancellationToken).Returns(snapshot);

        var result = await new SearchTacticalLoadouts(
                reader,
                new ZeroElapsedTimeProvider())
            .ExecuteAsync(request, cancellationToken);

        Assert.Equal(
            snapshot.Metadata.SaveSha256,
            result.Context.Context.SourceRevisionFingerprint);
        Assert.Equal(2, result.Discovery.Entries.Length);
        Assert.Equal(1, result.Search.Coverage.AdmittedCount);
        Assert.Equal(2, result.Search.Coverage.ExploredCombinationCount);
        Assert.True(result.Search.IsComplete);
        Assert.Contains(
            result.Search.Coverage.Caches,
            cache => cache.CacheIdentity == "CANDIDATE_PROJECTION_CACHE"
                && cache.MissCount == 1);
        await reader.Received(1).ReadAsync(snapshotRequest, cancellationToken);
    }

    [Fact]
    public async Task Execute_does_not_read_when_pre_cancelled()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var snapshot = Snapshot();
        var request = Request(
            new CombatSnapshotReadRequest("local.sav", 2),
            snapshot);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new SearchTacticalLoadouts(reader, new ZeroElapsedTimeProvider())
                .ExecuteAsync(request, cancellation.Token));

        await reader.DidNotReceive().ReadAsync(
            Arg.Any<CombatSnapshotReadRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_reports_injected_elapsed_limit_honestly()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var snapshot = Snapshot();
        var snapshotRequest = new CombatSnapshotReadRequest("local.sav", 2);
        var request = Request(
            snapshotRequest,
            snapshot,
            new TacticalSearchBounds(
                8,
                100,
                TimeSpan.FromSeconds(1),
                100));
        var cancellationToken = TestContext.Current.CancellationToken;
        reader.ReadAsync(snapshotRequest, cancellationToken).Returns(snapshot);

        var result = await new SearchTacticalLoadouts(
                reader,
                new AdvancingTimeProvider(TimeSpan.FromSeconds(1)))
            .ExecuteAsync(request, cancellationToken);

        Assert.Equal(
            TacticalSearchTerminator.TimeLimit,
            result.Search.Coverage.FirstTerminator);
        Assert.Equal(0, result.Search.Coverage.ExploredCombinationCount);
        Assert.False(result.Search.IsComplete);
    }

    private static TacticalLoadoutSearchReadRequest Request(
        CombatSnapshotReadRequest snapshotRequest,
        CombatSnapshot snapshot,
        TacticalSearchBounds? bounds = null) => new(
        new TacticalExecutionContextReadRequest(
            snapshotRequest,
            Rules.SupportedTargetGoalCodes,
            ConfirmedEvidence(),
            Proposal(snapshot.Player.LearnedSkills.Select(
                item => item.SkillId))),
        bounds ?? new TacticalSearchBounds(
            8,
            100,
            TimeSpan.FromSeconds(30),
            100));

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

    private static TacticalRuleEvidenceObservation[] ConfirmedEvidence() =>
        Rules.Transitions
            .SelectMany(item => item.EvidenceRequirements)
            .Concat(Rules.Roles.SelectMany(item => item.EvidenceRequirements))
            .DistinctBy(item => (item.Identity.Code, item.Scope, item.Source))
            .Select((item, index) => new TacticalRuleEvidenceObservation(
                item.Identity,
                item.Scope,
                item.Source,
                TacticalRuleEvidenceDisposition.Confirmed,
                new TacticalEvidenceReference(
                    item.Source,
                    $"CONFIRMED_{index:000}",
                    VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
                    VerifiedTacticalCombatRuleSets.RuleVersion,
                    item.Scope == TacticalRuleEvidenceScope.ExactTarget
                        ? "EXACT_TARGET"
                        : "BROAD_RULE")))
            .ToArray();

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
                    VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion)),
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

    private sealed class ZeroElapsedTimeProvider : TimeProvider
    {
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => 0;
    }

    private sealed class AdvancingTimeProvider(TimeSpan step) : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            var current = _timestamp;
            _timestamp += step.Ticks;
            return current;
        }
    }
}
