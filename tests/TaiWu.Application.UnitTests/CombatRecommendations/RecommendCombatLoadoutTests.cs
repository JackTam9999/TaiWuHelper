using NSubstitute;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TargetArchetypes;
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
            observation,
            TaiwuLanguage.Chinese);
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
        Assert.Equal(4, result.ThreatAnalysis.Threats.Length);
        var targetPlaybook = Assert.IsType<TargetPlaybookPersonalization>(
            result.TargetPlaybook);
        Assert.Contains(
            targetPlaybook.Analysis.ArchetypeMatches.Matches,
            match => match.Definition.Identity.Code
                    == "MIND_RESONANCE_RESET_BASELINE"
                && match.State == TargetArchetypeMatchState.Matched);
        Assert.Equal(4, targetPlaybook.EligibleGoals.Length);
        Assert.Contains(
            targetPlaybook.Counters,
            counter => counter.Option.Effect.SkillId == 604
                && counter.State
                    == TargetPlaybookCounterAvailabilityState.Feasible);
        Assert.NotEmpty(result.Generation.Candidates);
        Assert.NotEmpty(result.Scoring.RankedCandidates);
        Assert.True(result.ManualPlan.HasPlan);
        Assert.NotNull(result.Explanation);
        Assert.Equal(3, result.Styles.Length);
        Assert.Equal(
            Enum.GetValues<RecommendationPolicy>(),
            result.Styles.Select(style => style.Policy));
        Assert.Equal(
            RecommendationPolicy.Safe,
            result.SelectedStyle.Policy);
        Assert.Equal(
            RecommendationPolicy.Safe,
            result.Scoring.Weights.Policy);
        Assert.Contains(
            result.ManualPlan.Plan!.LoadoutChanges,
            change => change.Kind == ManualLoadoutChangeKind.Add
                && change.SkillId == 604
                && change.RequiredDirection is null);
        await reader.Received(1).ReadAsync(
            Arg.Is<CombatSnapshotReadRequest>(value =>
                value != null
                && value.SaveFilePath == request.SaveFilePath
                && value.TargetCharacterId == request.TargetCharacterId
                && value.Language == TaiwuLanguage.Chinese
                && value.CurrentLoadoutObservation == observation),
            cancellationToken);
    }

    [Fact]
    public async Task Direction_change_is_not_assumed_without_evidence()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var snapshot = GoldenSnapshot(PracticeDirection.Neutral);
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var useCase = new RecommendCombatLoadout(reader);

        var result = await useCase.ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                snapshot.Metadata.SavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Safe),
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Generation.Candidates);
        Assert.Contains(
            result.Generation.Diagnostics,
            diagnostic => diagnostic.Code
                    == CombatLoadoutGenerationDiagnosticCode.OptionRejected
                && diagnostic.SkillId == 604);
        Assert.False(result.ManualPlan.HasPlan);
    }

    [Fact]
    public async Task Partial_archetype_does_not_supply_playbook_counters()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var counter = Skill(
            604,
            SkillCategory.Attack,
            PracticeDirection.Reverse,
            directEffectId: 338,
            reverseEffectId: 1064);
        var target = Skill(
            719,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 669,
            reverseEffectId: 1669);
        var snapshot = Snapshot(
            [counter],
            [target],
            new CombatLoadoutSnapshot(
                [],
                [counter.SkillId],
                [],
                [],
                []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot(
                    [],
                    [target.SkillId],
                    [],
                    [],
                    [])),
            warnings: []);
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);

        var result = await new RecommendCombatLoadout(reader).ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                snapshot.Metadata.SavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Balanced),
            TestContext.Current.CancellationToken);

        var targetPlaybook = Assert.IsType<TargetPlaybookPersonalization>(
            result.TargetPlaybook);
        Assert.Contains(
            targetPlaybook.Analysis.ArchetypeMatches.Matches,
            match => match.Definition.Identity.Code
                    == "MIND_RESONANCE_RESET_BASELINE"
                && match.State == TargetArchetypeMatchState.Partial);
        Assert.Empty(targetPlaybook.Composition.SourcePlaybooks);
        Assert.Empty(targetPlaybook.EligibleGoals);
        Assert.Empty(targetPlaybook.Counters);
        Assert.NotEmpty(result.Generation.Candidates);
        Assert.All(
            result.Generation.Candidates.SelectMany(
                candidate => candidate.SelectedOptions),
            option =>
            {
                Assert.Null(option.CounterStrength);
                Assert.Empty(option.ThreatCodes);
            });
        Assert.All(
            result.Scoring.RankedCandidates,
            candidate => Assert.Equal(
                100m,
                candidate.Get(
                    RecommendationScoreComponentKind.ThreatCoverage)
                    .Score));
    }

    [Fact]
    public async Task Matched_but_unowned_counters_remain_explicit_gaps()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var withPlayerCounter = GoldenSnapshot();
        var snapshot = Snapshot(
            playerSkills: [],
            withPlayerCounter.Target.LearnedSkills.ToArray(),
            new CombatLoadoutSnapshot([], [], [], [], []),
            withPlayerCounter.Target.EquippedSkills,
            warnings: []);
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);

        var result = await new RecommendCombatLoadout(reader).ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                snapshot.Metadata.SavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Safe),
            TestContext.Current.CancellationToken);

        var targetPlaybook = Assert.IsType<TargetPlaybookPersonalization>(
            result.TargetPlaybook);
        Assert.Equal(6, targetPlaybook.Counters.Length);
        Assert.All(
            targetPlaybook.Counters,
            counter =>
            {
                Assert.Equal(
                    TargetPlaybookCounterAvailabilityState.Inaccessible,
                    counter.State);
                Assert.NotNull(counter.Gap);
                Assert.False(counter.Access.IsAccessible);
            });
        Assert.Equal(7, targetPlaybook.Gaps.Length);
        Assert.Empty(result.Generation.Candidates);
        Assert.DoesNotContain(
            result.Generation.Candidates.SelectMany(
                candidate => candidate.SelectedOptions),
            option => option.CounterStrength.HasValue);
    }

    [Fact]
    public async Task Immediate_breakthrough_is_a_manual_recommendation_step()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var counter = UnbrokenSkill(
            686,
            SkillCategory.Assistance,
            [PracticeDirection.Reverse],
            directEffectId: 422,
            reverseEffectId: 1422);
        var targetSkill = Skill(
            719,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 669,
            reverseEffectId: 1669);
        var resetSkill = Skill(
            287,
            SkillCategory.Assistance,
            PracticeDirection.Reverse,
            directEffectId: 185,
            reverseEffectId: 911);
        var snapshot = Snapshot(
            [counter],
            [targetSkill, resetSkill],
            new CombatLoadoutSnapshot([], [], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot(
                    [],
                    [targetSkill.SkillId],
                    [],
                    [],
                    [resetSkill.SkillId])),
            warnings: []);
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var useCase = new RecommendCombatLoadout(reader);

        var result = await useCase.ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                snapshot.Metadata.SavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Safe),
            TestContext.Current.CancellationToken);

        Assert.NotEmpty(result.Generation.Candidates);
        var plan = Assert.IsType<ManualCombatPlan>(result.ManualPlan.Plan);
        var breakthrough = Assert.Single(
            plan.LoadoutChanges,
            change => change.Kind
                == ManualLoadoutChangeKind.CompleteBreakthrough);
        Assert.Equal(counter.SkillId, breakthrough.SkillId);
        Assert.Equal(
            PracticeDirection.Reverse,
            breakthrough.RequiredDirection);
        var explanation = Assert.Single(
            result.Explanation!.Skills,
            skill => skill.SkillId == counter.SkillId);
        Assert.True(explanation.Direction.RequiresBreakthrough);
        Assert.False(
            explanation.Direction.RequiresManualDirectionChange);
        var targetPlaybook = Assert.IsType<TargetPlaybookPersonalization>(
            result.TargetPlaybook);
        var availability = Assert.Single(
            targetPlaybook.Counters,
            value => value.Option.Effect.SkillId == counter.SkillId);
        Assert.Equal(
            TargetPlaybookCounterAvailabilityState.Feasible,
            availability.State);
        Assert.True(availability.Access.IsAccessible);
        Assert.Null(availability.Gap);
    }

    [Fact]
    public async Task Matched_baseline_recommends_reverse_qilun_for_reset()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var qilun = Skill(
            291,
            SkillCategory.Assistance,
            PracticeDirection.Reverse,
            directEffectId: 189,
            reverseEffectId: 915);
        var reset = Skill(
            287,
            SkillCategory.Assistance,
            PracticeDirection.Reverse,
            directEffectId: 185,
            reverseEffectId: 911);
        var magicSound = Skill(
            719,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 669,
            reverseEffectId: 1669);
        var snapshot = Snapshot(
            [qilun],
            [magicSound, reset],
            new CombatLoadoutSnapshot([], [], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot(
                    [],
                    [magicSound.SkillId],
                    [],
                    [],
                    [reset.SkillId])),
            warnings: []);
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var useCase = new RecommendCombatLoadout(reader);

        var result = await useCase.ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                snapshot.Metadata.SavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Safe),
            TestContext.Current.CancellationToken);

        var threat = Assert.Single(
            result.ThreatAnalysis.Threats,
            value => value.Threat.Code == "DEFEAT_MARK_RESET_LOOP");
        Assert.Equal("DEFEAT_MARK_RESET_LOOP", threat.Threat.Code);
        var plan = Assert.IsType<ManualCombatPlan>(result.ManualPlan.Plan);
        Assert.Contains(
            plan.LoadoutChanges,
            change => change.Kind == ManualLoadoutChangeKind.Add
                && change.SkillId == qilun.SkillId);
        Assert.Contains(
            plan.SelectedRecommendation.Candidate.SelectedOptions,
            option => option.Candidate.SkillId == qilun.SkillId
                && option.Candidate.RequiredDirection
                    == PracticeDirection.Reverse);
        Assert.Contains(
            plan.SelectedRecommendation.Candidate.ThreatCodes,
            code => code == "DEFEAT_MARK_RESET_LOOP");
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

    private static CombatSnapshot GoldenSnapshot(
        PracticeDirection direction = PracticeDirection.Reverse)
    {
        var playerSkill = Skill(
            604,
            SkillCategory.Attack,
            direction,
            directEffectId: 338,
            reverseEffectId: 1064);
        var targetSkill = Skill(
            719,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 669,
            reverseEffectId: 1669);
        var resetSkill = Skill(
            287,
            SkillCategory.Assistance,
            PracticeDirection.Reverse,
            directEffectId: 185,
            reverseEffectId: 911);
        return Snapshot(
            [playerSkill],
            [targetSkill, resetSkill],
            new CombatLoadoutSnapshot([], [], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot(
                    [],
                    [targetSkill.SkillId],
                    [],
                    [],
                    [resetSkill.SkillId])),
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

    private static CombatSkillSnapshot UnbrokenSkill(
        int skillId,
        SkillCategory category,
        PracticeDirection[] availableDirections,
        int directEffectId,
        int reverseEffectId)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            category,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Unavailable(
                "The skill has not completed breakthrough."),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(directEffectId),
            SnapshotValue<int>.Available(reverseEffectId),
            SnapshotValue<BreakthroughDirectionAvailability>.Available(
                new BreakthroughDirectionAvailability(
                    isBrokenOut: false,
                    canBreakthroughNow: true,
                    availableDirections)));
    }
}
