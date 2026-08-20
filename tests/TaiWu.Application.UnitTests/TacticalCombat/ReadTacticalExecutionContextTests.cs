using NSubstitute;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Application.UnitTests.TacticalCombat;

public sealed class ReadTacticalExecutionContextTests
{
    [Fact]
    public async Task Execute_reads_one_snapshot_and_projects_one_revision()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var snapshot = Snapshot();
        var snapshotRequest = new CombatSnapshotReadRequest(
            "local.sav",
            snapshot.Target.CharacterId);
        var request = new TacticalExecutionContextReadRequest(
            snapshotRequest,
            VerifiedTacticalCombatRuleSets.HistoricalMagicSound
                .SupportedTargetGoalCodes,
            evidence: []);
        var cancellationToken = TestContext.Current.CancellationToken;
        reader.ReadAsync(snapshotRequest, cancellationToken).Returns(snapshot);

        var result = await new ReadTacticalExecutionContext(reader)
            .ExecuteAsync(request, cancellationToken);

        Assert.Equal(
            snapshot.Metadata.SaveSha256,
            result.Context.SourceRevisionFingerprint);
        Assert.Equal(snapshot.Metadata.CapturedAtUtc, result.CapturedAtUtc);
        Assert.True(result.Context.HasCompatibleRules);
        Assert.NotEmpty(result.Context.ResolvedRules);
        await reader.Received(1).ReadAsync(snapshotRequest, cancellationToken);
        await reader.DidNotReceive().ReadAsync(
            Arg.Is<CombatSnapshotReadRequest>(item => item != snapshotRequest),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_returns_typed_unsupported_context_for_current_version()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var snapshot = Snapshot("1.0.0+current");
        var snapshotRequest = new CombatSnapshotReadRequest("local.sav", 2);
        var request = new TacticalExecutionContextReadRequest(
            snapshotRequest,
            VerifiedTacticalCombatRuleSets.HistoricalMagicSound
                .SupportedTargetGoalCodes,
            evidence: []);
        var cancellationToken = TestContext.Current.CancellationToken;
        reader.ReadAsync(snapshotRequest, cancellationToken).Returns(snapshot);

        var result = await new ReadTacticalExecutionContext(reader)
            .ExecuteAsync(request, cancellationToken);

        Assert.False(result.Context.HasCompatibleRules);
        Assert.Empty(result.Context.ResolvedRules);
        await reader.Received(1).ReadAsync(snapshotRequest, cancellationToken);
    }

    [Fact]
    public async Task Execute_does_not_read_when_pre_cancelled()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var request = new TacticalExecutionContextReadRequest(
            new CombatSnapshotReadRequest("local.sav", 2),
            VerifiedTacticalCombatRuleSets.HistoricalMagicSound
                .SupportedTargetGoalCodes,
            evidence: []);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new ReadTacticalExecutionContext(reader).ExecuteAsync(
                request,
                cancellation.Token));

        await reader.DidNotReceive().ReadAsync(
            Arg.Any<CombatSnapshotReadRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Request_copies_and_canonicalizes_goal_codes()
    {
        var goals = VerifiedTacticalCombatRuleSets.HistoricalMagicSound
            .SupportedTargetGoalCodes.Reverse().ToList();
        var request = new TacticalExecutionContextReadRequest(
            new CombatSnapshotReadRequest("local.sav", 2),
            goals,
            evidence: []);
        goals.Clear();

        Assert.NotEmpty(request.TargetGoalCodes);
        Assert.Equal(
            request.TargetGoalCodes.Order(StringComparer.Ordinal),
            request.TargetGoalCodes);
        Assert.Throws<ArgumentException>(() =>
            new TacticalExecutionContextReadRequest(
                new CombatSnapshotReadRequest("local.sav", 2),
                targetGoalCodes: [],
                evidence: []));
    }

    private static CombatSnapshot Snapshot(string? gameDataVersion = null) =>
        new(
            new CombatSnapshotMetadata(
                new string('B', 64),
                DateTimeOffset.Parse("2026-08-20T10:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-08-20T09:00:00Z")),
                SnapshotValue<string>.Available(
                    gameDataVersion
                    ?? VerifiedTacticalCombatRuleSets
                        .HistoricalGameDataVersion)),
            new PlayerCombatSnapshot(
                1,
                SnapshotValue<string>.Unavailable("Not required."),
                learnedSkills: [],
                new CombatLoadoutSnapshot([], [], [], [], []),
                equipment: [],
                Budgets(),
                new GenericSlotAllocation(0, 0, 0, 0, 0),
                legendaryBookCostSlots: [],
                legendaryBookCostAssignments: []),
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

    private static SlotBudgetSet Budgets() => new(
    [
        new SlotBudget(SkillCategory.Neigong, 0, 6),
        new SlotBudget(SkillCategory.Attack, 0, 10),
        new SlotBudget(SkillCategory.Agility, 0, 8),
        new SlotBudget(SkillCategory.Defense, 0, 8),
        new SlotBudget(SkillCategory.Assistance, 0, 2)
    ]);
}
