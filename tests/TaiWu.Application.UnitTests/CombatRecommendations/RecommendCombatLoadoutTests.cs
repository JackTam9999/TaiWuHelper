using NSubstitute;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Application.UnitTests.CombatRecommendations;

public sealed class RecommendCombatLoadoutTests
{
    [Fact]
    public async Task Execute_orchestrates_read_analysis_generation_and_plan()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var snapshot = GoldenSnapshot();
        var observation = new PlayerLoadoutObservation(
            DateTimeOffset.Parse("2026-07-30T12:30:00Z"),
            "observation:current-screen",
            snapshot.Player.EquippedSkills,
            snapshot.Player.GenericSlotAllocation);
        var request = new RecommendCombatLoadoutRequest(
            snapshot.Metadata.SavePath,
            snapshot.Target.CharacterId,
            RecommendationPolicy.Safe,
            observation);
        var cancellationToken = TestContext.Current.CancellationToken;
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                cancellationToken)
            .Returns(snapshot);
        var useCase = new RecommendCombatLoadout(reader);

        var result = await useCase.ExecuteAsync(
            request,
            cancellationToken);

        Assert.Same(snapshot, result.Snapshot);
        Assert.Equal(snapshot.Warnings, result.SnapshotWarnings);
        Assert.Equal(3, result.ThreatAnalysis.Threats.Length);
        Assert.NotEmpty(result.Generation.Candidates);
        Assert.NotEmpty(result.Scoring.RankedCandidates);
        Assert.True(result.ManualPlan.HasPlan);
        Assert.NotNull(result.Explanation);
        Assert.Equal(
            RecommendationPolicy.Safe,
            result.Scoring.Weights.Policy);
        Assert.Contains(
            result.ManualPlan.Plan!.LoadoutChanges,
            change => change.Kind
                    == ManualLoadoutChangeKind.ChangeDirection
                && change.SkillId == 604
                && change.RequiredDirection
                    == PracticeDirection.Reverse);
        await reader.Received(1).ReadAsync(
            Arg.Is<CombatSnapshotReadRequest>(value =>
                value != null
                && value.SaveFilePath == request.SaveFilePath
                && value.TargetCharacterId == request.TargetCharacterId
                && value.CurrentLoadoutObservation == observation),
            cancellationToken);
    }

    [Fact]
    public async Task Execute_propagates_cancellation_to_snapshot_reader()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        using var cancellation = new CancellationTokenSource();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                cancellation.Token)
            .Returns(_ =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<CombatSnapshot>(
                    cancellation.Token);
            });
        var useCase = new RecommendCombatLoadout(reader);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => useCase.ExecuteAsync(
                new RecommendCombatLoadoutRequest(
                    "local.sav",
                    16317,
                    RecommendationPolicy.Balanced),
                cancellation.Token));

        await reader.Received(1).ReadAsync(
            Arg.Any<CombatSnapshotReadRequest>(),
            cancellation.Token);
    }

    [Fact]
    public async Task Execute_propagates_reader_failure()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var failure = new InvalidDataException("Unreadable save.");
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CombatSnapshot>(failure));
        var useCase = new RecommendCombatLoadout(reader);

        var actual = await Assert.ThrowsAsync<InvalidDataException>(
            () => useCase.ExecuteAsync(
                new RecommendCombatLoadoutRequest(
                    "local.sav",
                    16317,
                    RecommendationPolicy.Balanced),
                TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task No_analyzed_threat_or_option_returns_diagnostic_plan()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var snapshot = EmptySnapshot();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var useCase = new RecommendCombatLoadout(reader);

        var result = await useCase.ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                snapshot.Metadata.SavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Balanced),
            TestContext.Current.CancellationToken);

        Assert.Empty(result.ThreatAnalysis.Threats);
        Assert.Empty(result.Generation.Candidates);
        Assert.Empty(result.Scoring.RankedCandidates);
        Assert.False(result.ManualPlan.HasPlan);
        Assert.False(
            string.IsNullOrWhiteSpace(result.ManualPlan.Diagnostic));
        Assert.Null(result.Explanation);
    }

    [Fact]
    public void Request_validates_required_values_and_policy()
    {
        Assert.Throws<ArgumentException>(
            () => new RecommendCombatLoadoutRequest(
                " ",
                16317,
                RecommendationPolicy.Safe));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RecommendCombatLoadoutRequest(
                "local.sav",
                0,
                RecommendationPolicy.Safe));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RecommendCombatLoadoutRequest(
                "local.sav",
                16317,
                (RecommendationPolicy)999));
    }

    private static CombatSnapshot GoldenSnapshot()
    {
        var playerSkill = Skill(
            604,
            SkillCategory.Attack,
            PracticeDirection.Neutral,
            directEffectId: 338,
            reverseEffectId: 1064);
        var targetSkill = Skill(
            719,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 669,
            reverseEffectId: 1669);
        return Snapshot(
            [playerSkill],
            [targetSkill],
            new CombatLoadoutSnapshot([], [], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot(
                    [],
                    [targetSkill.SkillId],
                    [],
                    [],
                    [])),
            [
                new SnapshotWarning(
                    "SOURCE_WARNING",
                    "Preserve this source warning.")
            ]);
    }

    private static CombatSnapshot EmptySnapshot()
    {
        return Snapshot(
            playerSkills: [],
            targetSkills: [],
            new CombatLoadoutSnapshot([], [], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot([], [], [], [], [])),
            warnings: []);
    }

    private static CombatSnapshot Snapshot(
        CombatSkillSnapshot[] playerSkills,
        CombatSkillSnapshot[] targetSkills,
        CombatLoadoutSnapshot playerLoadout,
        SnapshotValue<CombatLoadoutSnapshot> targetLoadout,
        SnapshotWarning[] warnings)
    {
        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                @"C:\Taiwu\local.sav",
                new string('A', 64),
                DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-07-30T11:00:00Z")),
                SnapshotValue<string>.Available(
                    VerifiedCombatEffectCatalogs.GoldenGameDataVersion)),
            new PlayerCombatSnapshot(
                characterId: 1,
                SnapshotValue<string>.Available("Taiwu"),
                playerSkills,
                playerLoadout,
                equipment: [],
                new SlotBudgetSet(
                [
                    new SlotBudget(SkillCategory.Neigong, 0, 6),
                    new SlotBudget(SkillCategory.Attack, 0, 2),
                    new SlotBudget(SkillCategory.Agility, 0, 2),
                    new SlotBudget(SkillCategory.Defense, 0, 2),
                    new SlotBudget(SkillCategory.Assistance, 0, 2)
                ]),
                new GenericSlotAllocation(0, 0, 0, 0, 0),
                legendaryBookCostSlots: [],
                legendaryBookCostAssignments: []),
            new TargetCombatSnapshot(
                characterId: 16317,
                SnapshotValue<string>.Available("Target"),
                SnapshotValue<int>.Available(52),
                features: [],
                targetSkills,
                targetLoadout,
                equipment: []),
            warnings);
    }

    private static CombatSkillSnapshot Skill(
        int skillId,
        SkillCategory category,
        PracticeDirection direction,
        int directEffectId,
        int reverseEffectId)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            category,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(direction),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(directEffectId),
            SnapshotValue<int>.Available(reverseEffectId));
    }
}
