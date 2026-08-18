using NSubstitute;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.LoadoutComparisons;
using TaiWu.Application.TargetObservations;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.LoadoutComparisons;
using Xunit;

namespace TaiWu.Application.UnitTests.CombatRecommendations;

public sealed class RecommendCombatLoadoutTargetObservationTests
{
    private const string TestSavePath = @"C:\Taiwu\local.sav";

    private static readonly DateTimeOffset SaveTime = DateTimeOffset.Parse(
        "2026-08-07T20:00:00Z");

    [Fact]
    public async Task Resolved_observation_returns_typed_immutable_merge()
    {
        var snapshot = Snapshot();
        var definition = Definition(719, "Target Art");
        var source = Source(definition);
        var repository = Repository(definition);
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var request = new RecommendCombatLoadoutRequest(
            TestSavePath,
            snapshot.Target.CharacterId,
            RecommendationPolicy.Balanced,
            language: TaiWu.Application.Localization.TaiwuLanguage.English,
            targetObservation: new TargetObservationRequest(
                TargetObservationContext.Sparring,
                SaveTime.AddMinutes(1),
                "E3-000-CAP-002",
                TargetLoadoutCoverageKind.PartialLoadout,
                [
                    new TargetObservedSkillRequest(
                        "Target Art",
                        SkillCategory.Attack,
                        confirmedSkillId: 719,
                        PracticeDirection.Reverse,
                        slotIndex: 0)
                ]));

        var saveOnly = await new RecommendCombatLoadout(reader).ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                TestSavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Balanced),
            TestContext.Current.CancellationToken);

        var result = await new TargetObservationRecommendationWorkflow(
                reader,
                new ResolveTargetSkillSelection(source, repository))
            .ExecuteAsync(request, TestContext.Current.CancellationToken);

        var processing = Assert.IsType<TargetObservationProcessingResult>(
            result.TargetObservation);
        Assert.Equal(
            TargetLoadoutMergeStatus.Applied,
            processing.Merge.Status);
        Assert.Equal(
            PracticeDirection.Reverse,
            processing.Merge.Snapshot.Target.LearnedSkills
                .Single(skill => skill.SkillId == 719)
                .Direction.Value);
        Assert.Equal(
            PracticeDirection.Direct,
            snapshot.Target.LearnedSkills.Single().Direction.Value);
        Assert.Same(processing.Merge.Snapshot, result.Snapshot);
        Assert.NotSame(snapshot, result.Snapshot);
        Assert.Single(processing.ResolvedSkills);
        Assert.Equal(
            TargetSkillSnapshotPresence.Present,
            processing.ResolvedSkills[0].SnapshotPresence);
        Assert.Empty(result.ThreatAnalysis.Threats);
        var unsupportedEffect = Assert.Single(
            result.ThreatAnalysis.Warnings,
            warning => warning.Code
                == TargetThreatAnalyzer.UnrecognizedEffectWarningCode);
        Assert.Equal(
            "E3-000-CAP-002",
            unsupportedEffect.Mechanic.EvidenceReference);
        Assert.Equal(
            DecisionFingerprint(saveOnly),
            DecisionFingerprint(result));
    }

    [Fact]
    public async Task Snapshot_absent_observed_skill_adds_verified_observed_threat_deterministically()
    {
        var qilun = PlayerSkill(
            291,
            SkillCategory.Assistance,
            PracticeDirection.Reverse,
            directEffectId: 189,
            reverseEffectId: 915);
        var snapshot = Snapshot(
            targetSkills:
            [
                TargetSkill(
                    719,
                    "Target Art",
                    directEffectId: 669,
                    reverseEffectId: 1669)
            ],
            playerSkills: [qilun]);
        var definition = Definition(
            287,
            "Nine-Colored Cicada Art",
            directEffectId: 900,
            reverseEffectId: 911);
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var catalogueSource = Source(definition);
        var repository = Repository(definition);
        var resolver = new ResolveTargetSkillSelection(
            catalogueSource,
            repository);
        var workflow = new TargetObservationRecommendationWorkflow(
            reader,
            resolver);
        var observedRequest = new RecommendCombatLoadoutRequest(
            TestSavePath,
            snapshot.Target.CharacterId,
            RecommendationPolicy.Balanced,
            targetObservation: new TargetObservationRequest(
                TargetObservationContext.Sparring,
                SaveTime.AddMinutes(1),
                "E3-000-CAP-002",
                TargetLoadoutCoverageKind.PartialLoadout,
                [
                    new TargetObservedSkillRequest(
                        "Nine-Colored Cicada Art",
                        SkillCategory.Attack,
                        confirmedSkillId: 287,
                        PracticeDirection.Reverse,
                        slotIndex: 0)
                ]));
        var saveOnly = await new RecommendCombatLoadout(reader).ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                TestSavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Balanced),
            TestContext.Current.CancellationToken);

        var first = await workflow.ExecuteAsync(
            observedRequest,
            TestContext.Current.CancellationToken);
        var second = await workflow.ExecuteAsync(
            observedRequest,
            TestContext.Current.CancellationToken);
        var cleared = await new RecommendCombatLoadout(reader).ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                TestSavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Balanced),
            TestContext.Current.CancellationToken);

        var finding = Assert.Single(
            first.ThreatAnalysis.Threats,
            value => value.Threat.Code == "DEFEAT_MARK_RESET_LOOP");
        Assert.Equal("DEFEAT_MARK_RESET_LOOP", finding.Threat.Code);
        var source = Assert.Single(finding.Sources);
        Assert.Equal(TargetThreatSourceKind.ObservedEquipped, source.Kind);
        Assert.Equal("E3-000-CAP-002", source.EvidenceReference);
        Assert.Equal(
            TargetThreatEvidenceConfidence.VerifiedRule,
            Assert.Single(finding.Threat.Evidence).Confidence);
        Assert.Equal(
            ThreatFingerprint(first),
            ThreatFingerprint(second));
        Assert.NotEqual(
            string.Join("|", DecisionFingerprint(saveOnly)),
            string.Join("|", DecisionFingerprint(first)));
        var savePlaybook = Assert.IsType<TargetPlaybookPersonalization>(
            saveOnly.TargetPlaybook);
        var observedPlaybook = Assert.IsType<TargetPlaybookPersonalization>(
            first.TargetPlaybook);
        var clearedPlaybook = Assert.IsType<TargetPlaybookPersonalization>(
            cleared.TargetPlaybook);
        Assert.NotEqual(
            savePlaybook.Analysis.Profile.Fingerprint,
            observedPlaybook.Analysis.Profile.Fingerprint);
        Assert.NotEqual(
            savePlaybook.Composition.StableKey,
            observedPlaybook.Composition.StableKey);
        Assert.NotEqual(
            savePlaybook.Adjustments.StableKey,
            observedPlaybook.Adjustments.StableKey);
        Assert.Equal(
            savePlaybook.Analysis.Profile.Fingerprint,
            clearedPlaybook.Analysis.Profile.Fingerprint);
        Assert.Equal(
            savePlaybook.Composition.StableKey,
            clearedPlaybook.Composition.StableKey);
        Assert.Equal(
            savePlaybook.Adjustments.StableKey,
            clearedPlaybook.Adjustments.StableKey);
        Assert.Equal(
            FullResultFingerprint(saveOnly),
            FullResultFingerprint(cleared));
        Assert.Equal(
            ComparisonFingerprint(saveOnly),
            ComparisonFingerprint(cleared));
        Assert.All(
            first.Styles,
            style => Assert.Equal(
                RecommendationPolicyWeights.For(style.Policy),
                style.Scoring.Weights));
        Assert.Contains(
            first.SelectedStyle.Scoring.RankedCandidates
                .SelectMany(value => value.Candidate.SelectedOptions),
            option => option.Candidate.SkillId == qilun.SkillId
                && option.ThreatCodes.Contains("DEFEAT_MARK_RESET_LOOP"));
        Assert.Equal(
            DecisionFingerprint(first),
            DecisionFingerprint(second));
        Assert.Equal(
            FullResultFingerprint(first),
            FullResultFingerprint(second));
        Assert.Equal(
            ComparisonFingerprint(first),
            ComparisonFingerprint(second));
        await catalogueSource.Received(2).ReadAsync(
            Arg.Any<CancellationToken>());
        await repository.Received(2).ReadStateAsync(
            Arg.Any<CancellationToken>());
        await repository.Received(2).QueryAsync(
            Arg.Any<CombatSkillCatalogueFilter>(),
            Arg.Any<CancellationToken>());
        Assert.DoesNotContain(
            repository.ReceivedCalls(),
            call => call.GetMethodInfo().Name
                == nameof(ICombatSkillCatalogueRepository.ReplaceAsync));
        var impact = Assert.IsType<TargetObservationRecommendationImpact>(
            first.TargetObservationImpact);
        var threatImpact = Assert.Single(
            impact.Threats,
            value => value.Kind == TargetThreatImpactKind.Added);
        Assert.Equal(TargetThreatImpactKind.Added, threatImpact.Kind);
        Assert.Equal("DEFEAT_MARK_RESET_LOOP", threatImpact.ThreatCode);
        Assert.All(
            impact.Threats.Where(value => value != threatImpact),
            value => Assert.Equal(
                TargetThreatImpactKind.Unchanged,
                value.Kind));
        Assert.True(impact.PartialCoverageLeavesUnknown);
        Assert.NotEmpty(impact.FeasibilityChanges);
        Assert.All(
            impact.RecommendationChanges,
            change =>
            {
                Assert.Equal(
                    TargetRecommendationImpactKind.Added,
                    change.Kind);
                Assert.Equal(
                    TargetRecommendationChangeCause.Feasibility,
                    change.Cause);
                Assert.Equal(qilun.SkillId, change.SkillId);
            });
    }

    [Fact]
    public async Task Observed_threat_cannot_make_unowned_counter_feasible()
    {
        var snapshot = Snapshot(
            targetSkills:
            [
                TargetSkill(
                    719,
                    "Target Art",
                    directEffectId: 669,
                    reverseEffectId: 1669)
            ]);
        var definition = Definition(
            287,
            "Nine-Colored Cicada Art",
            directEffectId: 900,
            reverseEffectId: 911);
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);

        var result = await new TargetObservationRecommendationWorkflow(
                reader,
                new ResolveTargetSkillSelection(
                    Source(definition),
                    Repository(definition)))
            .ExecuteAsync(
                ObservedRequest(
                    snapshot,
                    "Nine-Colored Cicada Art",
                    skillId: 287,
                    PracticeDirection.Reverse),
                TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                "DEFEAT_MARK_RESET_LOOP",
                "DISTRACTION_MARK_ACCUMULATION",
                "MIND_RESONANCE_CASCADE",
                "POSITIVE_MAGIC_SOUND_MIND_DAMAGE"
            ],
            result.ThreatAnalysis.Threats.Select(value => value.Threat.Code));
        Assert.Empty(result.Generation.Candidates);
        Assert.Contains(
            result.Generation.Diagnostics,
            diagnostic => diagnostic.Code
                    == CombatLoadoutGenerationDiagnosticCode.OptionRejected
                && diagnostic.SkillId == 291);
        Assert.All(
            result.Styles,
            style => Assert.Empty(style.Scoring.RankedCandidates));
        Assert.Empty(result.TargetObservationImpact!.RecommendationChanges);
    }

    [Theory]
    [InlineData(TargetObservationContext.Hostile)]
    [InlineData(TargetObservationContext.Story)]
    public async Task Battle_visible_effect_is_resolved_without_claiming_equipped_membership(
        TargetObservationContext context)
    {
        var snapshot = Snapshot(
            targetSkills: [],
            targetEquippedSkills:
                SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                    "The target loadout is not persisted."));
        var definition = Definition(
            287,
            "Nine-Colored Cicada Art",
            directEffectId: 900,
            reverseEffectId: 911);
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var request = new RecommendCombatLoadoutRequest(
            TestSavePath,
            snapshot.Target.CharacterId,
            RecommendationPolicy.Balanced,
            targetObservation: new TargetObservationRequest(
                context,
                SaveTime.AddMinutes(1),
                "E3-012-CAP-001",
                TargetLoadoutCoverageKind.PartialLoadout,
                [
                    new TargetObservedSkillRequest(
                        "Nine-Colored Cicada Art",
                        SkillCategory.Attack,
                        confirmedSkillId: 287,
                        PracticeDirection.Reverse,
                        visiblePowerPercent: 142)
                ]));

        var result = await new TargetObservationRecommendationWorkflow(
                reader,
                new ResolveTargetSkillSelection(
                    Source(definition),
                    Repository(definition)))
            .ExecuteAsync(request, TestContext.Current.CancellationToken);

        var processing = Assert.IsType<TargetObservationProcessingResult>(
            result.TargetObservation);
        Assert.False(processing.Merge.Snapshot.Target.EquippedSkills.IsAvailable);
        Assert.Equal(
            142,
            Assert.Single(processing.ResolvedSkills)
                .Observation.VisiblePowerPercent);
        var source = Assert.Single(
            Assert.Single(result.ThreatAnalysis.Threats).Sources);
        Assert.Equal(
            TargetThreatSourceKind.ObservedActiveEffect,
            source.Kind);
        Assert.Equal(
            TargetThreatSourceScope.BattleVisibleActiveEffect,
            source.Scope);
        var impact = Assert.IsType<TargetObservationRecommendationImpact>(
            result.TargetObservationImpact);
        var threatImpact = Assert.Single(impact.Threats);
        Assert.Equal(TargetThreatImpactKind.Added, threatImpact.Kind);
        Assert.Contains(
            TargetThreatSourceKind.ObservedActiveEffect,
            threatImpact.SourceKinds);
    }

    [Fact]
    public async Task Confirming_unchanged_verified_threat_preserves_all_policy_decisions()
    {
        var counter = PlayerSkill(
            604,
            SkillCategory.Attack,
            PracticeDirection.Reverse,
            directEffectId: 338,
            reverseEffectId: 1064);
        var target = TargetSkill(
            719,
            "Target Art",
            directEffectId: 669,
            reverseEffectId: 1669);
        var snapshot = Snapshot(
            targetSkills: [target],
            playerSkills: [counter]);
        var definition = Definition(
            target.SkillId,
            "Target Art",
            directEffectId: 669,
            reverseEffectId: 1669);
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var saveOnly = await new RecommendCombatLoadout(reader).ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                TestSavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Balanced),
            TestContext.Current.CancellationToken);

        var observed = await new TargetObservationRecommendationWorkflow(
                reader,
                new ResolveTargetSkillSelection(
                    Source(definition),
                    Repository(definition)))
            .ExecuteAsync(
                ObservedRequest(
                    snapshot,
                    "Target Art",
                    target.SkillId,
                    PracticeDirection.Direct),
                TestContext.Current.CancellationToken);

        Assert.Equal(
            saveOnly.ThreatAnalysis.Threats.Select(value => value.Threat.Code),
            observed.ThreatAnalysis.Threats.Select(value => value.Threat.Code));
        Assert.All(
            observed.ThreatAnalysis.Threats.SelectMany(value => value.Sources),
            source => Assert.Equal(
                TargetThreatSourceKind.ObservedEquipped,
                source.Kind));
        Assert.Equal(
            DecisionFingerprint(saveOnly),
            DecisionFingerprint(observed));
        Assert.All(
            observed.Styles,
            style => Assert.Equal(
                RecommendationPolicyWeights.For(style.Policy),
                style.Scoring.Weights));
        Assert.All(
            observed.TargetObservationImpact!.Threats,
            change => Assert.Equal(
                TargetThreatImpactKind.Confirmed,
                change.Kind));
        Assert.Empty(
            observed.TargetObservationImpact.RecommendationChanges);
    }

    [Fact]
    public async Task Observed_unrecognized_direction_removes_only_verified_counter_decisions()
    {
        var counter = PlayerSkill(
            604,
            SkillCategory.Attack,
            PracticeDirection.Reverse,
            directEffectId: 338,
            reverseEffectId: 1064);
        var target = TargetSkill(
            719,
            "Target Art",
            directEffectId: 669,
            reverseEffectId: 1669);
        var reset = TargetSkill(
            287,
            "Nine-Colored Cicada Art",
            directEffectId: 185,
            reverseEffectId: 911,
            direction: PracticeDirection.Reverse);
        var snapshot = Snapshot(
            targetSkills: [target, reset],
            playerSkills: [counter]);
        var definition = Definition(
            target.SkillId,
            "Target Art",
            directEffectId: 669,
            reverseEffectId: 1669);
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var saveOnly = await new RecommendCombatLoadout(reader).ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                TestSavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Balanced),
            TestContext.Current.CancellationToken);
        var workflow = new TargetObservationRecommendationWorkflow(
            reader,
            new ResolveTargetSkillSelection(
                Source(definition),
                Repository(definition)));
        var request = ObservedRequest(
            snapshot,
            "Target Art",
            target.SkillId,
            PracticeDirection.Reverse);

        var first = await workflow.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);
        var second = await workflow.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.NotEmpty(saveOnly.Generation.Candidates);
        Assert.Equal(
            ["DEFEAT_MARK_RESET_LOOP"],
            first.ThreatAnalysis.Threats.Select(value => value.Threat.Code));
        Assert.Empty(first.Generation.Candidates);
        Assert.All(
            first.Styles,
            style => Assert.Empty(style.Scoring.RankedCandidates));
        var unresolved = Assert.Single(
            first.ThreatAnalysis.Warnings,
            warning => warning.Code
                == TargetThreatAnalyzer.UnrecognizedEffectWarningCode);
        Assert.Equal(
            "E3-000-CAP-002",
            unresolved.Mechanic.EvidenceReference);
        Assert.NotEqual(
            string.Join("|", DecisionFingerprint(saveOnly)),
            string.Join("|", DecisionFingerprint(first)));
        Assert.Equal(
            DecisionFingerprint(first),
            DecisionFingerprint(second));
        Assert.Equal(
            ThreatFingerprint(first),
            ThreatFingerprint(second));
        var impact = first.TargetObservationImpact!;
        Assert.Equal(
            TargetThreatImpactKind.Unchanged,
            Assert.Single(
                impact.Threats,
                change => change.ThreatCode
                    == "DEFEAT_MARK_RESET_LOOP").Kind);
        Assert.All(
            impact.Threats.Where(change => change.ThreatCode
                != "DEFEAT_MARK_RESET_LOOP"),
            change => Assert.Equal(
                TargetThreatImpactKind.Removed,
                change.Kind));
        Assert.All(
            impact.RecommendationChanges,
            change =>
            {
                Assert.Equal(
                    TargetRecommendationImpactKind.Removed,
                    change.Kind);
                Assert.Equal(
                    TargetRecommendationChangeCause.Feasibility,
                    change.Cause);
                Assert.Equal(counter.SkillId, change.SkillId);
            });
        var unsupported = Assert.Single(impact.UnsupportedEvidence);
        Assert.False(unsupported.WasPresentBefore);
        Assert.Equal(
            TargetThreatAnalyzer.UnrecognizedEffectWarningCode,
            unsupported.Code);
    }

    [Fact]
    public async Task Complete_observation_demotes_threats_and_exposes_both_conflict_sources()
    {
        var counter = PlayerSkill(
            604,
            SkillCategory.Attack,
            PracticeDirection.Reverse,
            directEffectId: 338,
            reverseEffectId: 1064);
        var target = TargetSkill(
            719,
            "Target Art",
            directEffectId: 669,
            reverseEffectId: 1669);
        var snapshot = Snapshot(
            targetSkills: [target],
            playerSkills: [counter]);
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var request = new RecommendCombatLoadoutRequest(
            TestSavePath,
            snapshot.Target.CharacterId,
            RecommendationPolicy.Balanced,
            targetObservation: new TargetObservationRequest(
                TargetObservationContext.Sparring,
                SaveTime.AddMinutes(1),
                "E3-000-CAP-002",
                TargetLoadoutCoverageKind.CompleteCurrentLoadout,
                selectedSkills: []));

        var result = await new TargetObservationRecommendationWorkflow(
                reader,
                new ResolveTargetSkillSelection(
                    Source(),
                    Repository()))
            .ExecuteAsync(
                request,
                TestContext.Current.CancellationToken);

        var impact = result.TargetObservationImpact!;
        Assert.False(impact.PartialCoverageLeavesUnknown);
        Assert.All(
            impact.Threats,
            change => Assert.Equal(
                TargetThreatImpactKind.Demoted,
                change.Kind));
        var conflict = Assert.Single(impact.Conflicts);
        Assert.Equal(
            TargetLoadoutObservationMerger.TargetEquippedSkillsField,
            conflict.Field);
        Assert.Equal("SAVE_SCREEN_CONFLICT", conflict.ReasonCode);
        Assert.Equal(
            [SnapshotDataSource.Save, SnapshotDataSource.CurrentScreenObservation],
            conflict.Sources.Select(source => source.Source));
    }

    [Fact]
    public async Task Clearing_observation_reproduces_the_save_only_result()
    {
        var counter = PlayerSkill(
            604,
            SkillCategory.Attack,
            PracticeDirection.Reverse,
            directEffectId: 338,
            reverseEffectId: 1064);
        var target = TargetSkill(
            719,
            "Target Art",
            directEffectId: 669,
            reverseEffectId: 1669);
        var snapshot = Snapshot(
            targetSkills: [target],
            playerSkills: [counter]);
        var definition = Definition(
            target.SkillId,
            "Target Art",
            directEffectId: 669,
            reverseEffectId: 1669);
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var saveOnly = new RecommendCombatLoadout(reader);
        var saveOnlyRequest = new RecommendCombatLoadoutRequest(
            TestSavePath,
            snapshot.Target.CharacterId,
            RecommendationPolicy.Balanced);
        var initial = await saveOnly.ExecuteAsync(
            saveOnlyRequest,
            TestContext.Current.CancellationToken);
        var observed = await new TargetObservationRecommendationWorkflow(
                reader,
                new ResolveTargetSkillSelection(
                    Source(definition),
                    Repository(definition)))
            .ExecuteAsync(
                ObservedRequest(
                    snapshot,
                    "Target Art",
                    target.SkillId,
                    PracticeDirection.Reverse),
                TestContext.Current.CancellationToken);
        var cleared = await saveOnly.ExecuteAsync(
            saveOnlyRequest,
            TestContext.Current.CancellationToken);

        Assert.NotEqual(
            FullResultFingerprint(initial),
            FullResultFingerprint(observed));
        Assert.Null(cleared.TargetObservation);
        Assert.Null(cleared.TargetObservationImpact);
        Assert.Equal(
            FullResultFingerprint(initial),
            FullResultFingerprint(cleared));
        Assert.NotEqual(
            ComparisonFingerprint(initial),
            ComparisonFingerprint(observed));
        Assert.Equal(
            ComparisonFingerprint(initial),
            ComparisonFingerprint(cleared));
    }

    [Fact]
    public async Task Ambiguous_selection_fails_with_stable_candidates()
    {
        var first = Definition(719, "Target Art");
        var second = Definition(720, "Target Art Advanced");
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Snapshot());
        var request = Request(
            new TargetObservedSkillRequest(
                "Target Art",
                SkillCategory.Attack));

        var exception = await Assert.ThrowsAsync<
            TargetObservationResolutionException>(
            () => new TargetObservationRecommendationWorkflow(
                    reader,
                    new ResolveTargetSkillSelection(
                        Source(first, second),
                        Repository(first, second)))
                .ExecuteAsync(
                    request,
                    TestContext.Current.CancellationToken));

        Assert.Equal(TargetSkillSelectionStatus.Ambiguous, exception.Status);
        Assert.Equal(0, exception.SelectionIndex);
        Assert.Equal([719, 720], exception.Candidates.Select(x => x.SkillId));
        Assert.Equal(
            "The target observation could not be resolved.",
            exception.Message);
    }

    [Fact]
    public async Task Cancellation_propagates_during_catalogue_resolution()
    {
        var definition = Definition(719, "Target Art");
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Snapshot());
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        using var cancellation = new CancellationTokenSource();
        repository.ReadStateAsync(cancellation.Token).Returns(_ =>
        {
            cancellation.Cancel();
            return Task.FromCanceled<CombatSkillCatalogueRepositorySnapshot>(
                cancellation.Token);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new TargetObservationRecommendationWorkflow(
                    reader,
                    new ResolveTargetSkillSelection(
                        Source(definition),
                        repository))
                .ExecuteAsync(
                    Request(new TargetObservedSkillRequest(
                        "Target Art",
                        SkillCategory.Attack,
                        confirmedSkillId: 719)),
                    cancellation.Token));

        await repository.Received(1).ReadStateAsync(cancellation.Token);
    }

    [Fact]
    public void Observation_contract_accepts_partial_battle_context_and_rejects_invalid_evidence()
    {
        var story = new TargetObservationRequest(
            TargetObservationContext.Story,
            SaveTime,
            "E3-012-CAP-001",
            TargetLoadoutCoverageKind.PartialLoadout,
            [
                new TargetObservedSkillRequest(
                    "Target Art",
                    SkillCategory.Attack,
                    visiblePowerPercent: 146)
            ]);

        Assert.Equal(TargetObservationContext.Story, story.Context);
        Assert.Equal(
            146,
            Assert.Single(story.SelectedSkills).VisiblePowerPercent);
        Assert.Throws<ArgumentException>(() => new TargetObservationRequest(
            TargetObservationContext.Story,
            SaveTime,
            "E3-012-CAP-001",
            TargetLoadoutCoverageKind.CompleteCurrentLoadout,
            []));
        Assert.Throws<ArgumentException>(() => new TargetObservationRequest(
            TargetObservationContext.Sparring,
            SaveTime,
            @"C:\captures\target.png",
            TargetLoadoutCoverageKind.PartialLoadout,
            []));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TargetObservedSkillRequest(
                "Target Art",
                SkillCategory.Attack,
                visiblePowerPercent: -1));
    }

    private static RecommendCombatLoadoutRequest Request(
        TargetObservedSkillRequest selection)
    {
        var snapshot = Snapshot();
        return new RecommendCombatLoadoutRequest(
            TestSavePath,
            snapshot.Target.CharacterId,
            RecommendationPolicy.Balanced,
            targetObservation: new TargetObservationRequest(
                TargetObservationContext.Sparring,
                SaveTime.AddMinutes(1),
                "E3-000-CAP-002",
                TargetLoadoutCoverageKind.PartialLoadout,
                [selection]));
    }

    private static RecommendCombatLoadoutRequest ObservedRequest(
        CombatSnapshot snapshot,
        string visibleName,
        int skillId,
        PracticeDirection direction) => new(
            TestSavePath,
            snapshot.Target.CharacterId,
            RecommendationPolicy.Balanced,
            targetObservation: new TargetObservationRequest(
                TargetObservationContext.Sparring,
                SaveTime.AddMinutes(1),
                "E3-000-CAP-002",
                TargetLoadoutCoverageKind.PartialLoadout,
                [
                    new TargetObservedSkillRequest(
                        visibleName,
                        SkillCategory.Attack,
                        skillId,
                        direction,
                        slotIndex: 0)
                ]));

    private static CombatSnapshot Snapshot(
        CombatSkillSnapshot[]? targetSkills = null,
        SnapshotValue<CombatLoadoutSnapshot>? targetEquippedSkills = null,
        CombatSkillSnapshot[]? playerSkills = null)
    {
        var skills = targetSkills ?? [TargetSkill(719, "Target Art")];
        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                new string('A', 64),
                SaveTime,
                SnapshotValue<DateTimeOffset>.Available(SaveTime),
                SnapshotValue<string>.Available(
                    TargetLoadoutCompletenessEvidence
                        .E3000GameDataVersion)),
            new PlayerCombatSnapshot(
                1,
                SnapshotValue<string>.Available("Taiwu"),
                learnedSkills: playerSkills ?? [],
                new CombatLoadoutSnapshot([], [], [], [], []),
                equipment: [],
                new SlotBudgetSet(Enum.GetValues<SkillCategory>().Select(
                    category => new SlotBudget(category, 0, 10))),
                new GenericSlotAllocation(0, 0, 0, 0, 0),
                legendaryBookCostSlots: [],
                legendaryBookCostAssignments: []),
            new TargetCombatSnapshot(
                16317,
                SnapshotValue<string>.Available("Target"),
                SnapshotValue<int>.Available(52),
                features: [],
                skills,
                targetEquippedSkills
                    ?? SnapshotValue<CombatLoadoutSnapshot>.Available(
                    new CombatLoadoutSnapshot(
                        [],
                        skills.Select(skill => skill.SkillId),
                        [],
                        [],
                        [])),
                equipment: []),
            warnings: []);
    }

    private static CombatSkillSnapshot PlayerSkill(
        int skillId,
        SkillCategory category,
        PracticeDirection direction,
        int directEffectId,
        int reverseEffectId) => new(
            skillId,
            SnapshotValue<string>.Available($"Player Skill {skillId}"),
            category,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(direction),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(directEffectId),
            SnapshotValue<int>.Available(reverseEffectId));

    private static CombatSkillSnapshot TargetSkill(
        int skillId,
        string name,
        int? directEffectId = null,
        int? reverseEffectId = null,
        PracticeDirection direction = PracticeDirection.Direct) => new(
            skillId,
            SnapshotValue<string>.Available(name),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(2),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(
                direction),
            new SkillSlotContribution(2, 0, 0, 0, 1),
            SnapshotValue<int>.Available(directEffectId ?? 1000 + skillId),
            SnapshotValue<int>.Available(reverseEffectId ?? 2000 + skillId));

    private static ICombatSkillDefinitionSource Source(
        params CombatSkillDefinition[] definitions)
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            CombatSkillDefinitionSourceResult.Available(
                Identity,
                definitions));
        return source;
    }

    private static ICombatSkillCatalogueRepository Repository(
        params CombatSkillDefinition[] definitions)
    {
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>()).Returns(
            new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Ready,
                Identity,
                definitions.Length,
                SaveTime));
        repository.QueryAsync(
                Arg.Any<CombatSkillCatalogueFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(definitions);
        return repository;
    }

    private static CombatSkillDefinition Definition(
        int skillId,
        string englishName,
        int? directEffectId = null,
        int? reverseEffectId = null)
    {
        var source = new CatalogueSourceReference(
            CatalogueSourceKind.GameData,
            "gamedata:test",
            $"combat-skill:{skillId}");
        return new CombatSkillDefinition(
            skillId,
            new CombatSkillLocalizedNames(
            [
                new LocalizedCombatSkillName(
                    CatalogueLanguage.English,
                    englishName,
                    new CatalogueSourceReference(
                        CatalogueSourceKind.EnglishLanguageResource,
                        "language-en:test",
                        $"combat-skill-name:{skillId}"))
            ]),
            CatalogueField<CombatSkillDiscipline>.Available(
                CombatSkillDiscipline.Finger,
                source),
            CatalogueField<CombatSkillGrade>.Available(
                new CombatSkillGrade(5),
                source),
            CatalogueField<CombatSkillFactionId>.Available(
                new CombatSkillFactionId(1),
                source),
            CatalogueField<CombatSkillElement>.Available(
                CombatSkillElement.Wood,
                source),
            CatalogueField<CombatSkillEquipmentType>.Available(
                CombatSkillEquipmentType.Attack,
                source),
            CatalogueField<CombatSkillGridCost>.Available(
                new CombatSkillGridCost(2),
                source),
            CatalogueField<SkillSlotContribution>.Available(
                new SkillSlotContribution(2, 0, 0, 0, 1),
                source),
            requirements: null,
            new CombatSkillTimingDefinition(
                CatalogueField<int>.Available(100, source),
                CatalogueField<int>.Available(100, source),
                CatalogueField<int>.Available(100, source)),
            new CombatSkillEffectReferences(
                CatalogueField<CombatSkillEffectId>.Available(
                    new CombatSkillEffectId(
                        directEffectId ?? 1000 + skillId),
                    source),
                CatalogueField<CombatSkillEffectId>.Available(
                    new CombatSkillEffectId(
                        reverseEffectId ?? 2000 + skillId),
                    source),
                CatalogueField<CombatSkillEffectId>.Unavailable("unused")),
            rawDescriptions: [],
            source);
    }

    private static string[] ThreatFingerprint(
        CombatLoadoutRecommendation recommendation) =>
    [
        .. recommendation.ThreatAnalysis.Threats.Select(finding =>
            $"{finding.Threat.Code}:"
            + string.Join(",", finding.Sources.Select(source =>
                $"{source.SkillId}/{source.Kind}/{source.Direction}/"
                + $"{source.RawEffectId}/{source.EvidenceReference}")))
    ];

    private static string[] DecisionFingerprint(
        CombatLoadoutRecommendation recommendation) =>
    [
        .. recommendation.Styles.Select(style =>
            $"{style.Policy}:"
            + string.Join(",", style.Scoring.RankedCandidates.Select(value =>
                $"{value.Candidate.StableKey}/{value.TotalScore}")))
    ];

    private static string FullResultFingerprint(
        CombatLoadoutRecommendation recommendation)
    {
        var snapshot = recommendation.Snapshot;
        var targetLoadout = snapshot.Target.EquippedSkills.IsAvailable
            ? string.Join(
                ";",
                Enum.GetValues<SkillCategory>().Select(category =>
                    $"{category}="
                    + string.Join(",", snapshot.Target.EquippedSkills
                        .Value.Get(category))))
            : $"unavailable:{snapshot.Target.EquippedSkills.UnavailableReason}";
        var targetSkills = string.Join(
            ";",
            snapshot.Target.LearnedSkills.Select(skill =>
                $"{skill.SkillId}/{skill.Category}/"
                + (skill.Direction.IsAvailable
                    ? skill.Direction.Value
                    : $"unavailable:{skill.Direction.UnavailableReason}")));
        var candidates = string.Join(
            ";",
            recommendation.Generation.Candidates.Select(candidate =>
                $"{candidate.StableKey}:["
                + string.Join(",", candidate.SelectedOptions.Select(option =>
                    $"{option.Candidate.SkillId}/"
                    + $"{option.Candidate.RequiredDirection}"))
                + "]"));
        var threats = string.Join(";", ThreatFingerprint(recommendation));
        var decisions = string.Join(";", DecisionFingerprint(recommendation));
        var impact = recommendation.TargetObservationImpact is null
            ? "none"
            : string.Join(
                ";",
                recommendation.TargetObservationImpact.Threats.Select(value =>
                    $"threat:{value.ThreatCode}/{value.Kind}/"
                    + string.Join(",", value.SourceKinds))
                .Concat(recommendation.TargetObservationImpact
                    .RecommendationChanges.Select(value =>
                        $"recommendation:{value.Policy}/{value.Kind}/"
                        + $"{value.Cause}/{value.SkillId}/"
                        + $"{value.RequiredDirection}"))
                .Concat(recommendation.TargetObservationImpact
                    .UnsupportedEvidence.Select(value =>
                        $"unsupported:{value.Code}/{value.SkillId}/"
                        + $"{value.RawEffectId}/{value.WasPresentBefore}"))
                .Concat(recommendation.TargetObservationImpact.Conflicts.Select(
                    value => $"conflict:{value.Field}/{value.PrecedenceRule}/"
                        + string.Join(",", value.Sources.Select(source =>
                            $"{source.Source}/{source.CapturedAtUtc:O}")))));
        return string.Join(
            "\n",
            $"snapshot:{snapshot.Metadata.SaveSha256}/{targetLoadout}",
            $"target-skills:{targetSkills}",
            $"threats:{threats}",
            $"candidates:{candidates}",
            $"decisions:{decisions}",
            $"impact:{impact}");
    }

    private static string ComparisonFingerprint(
        CombatLoadoutRecommendation recommendation)
    {
        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);
        List<string> facts =
        [
            comparison.ComparisonReference.Value,
            comparison.SnapshotReference.Value
        ];
        foreach (var column in comparison.Columns)
        {
            facts.Add(
                $"column:{column.Kind}/{column.Status}/"
                + column.Diagnostic?.Code.Value);
            if (column.Loadout is not null)
            {
                facts.Add(
                    $"allocation:{column.Kind}/"
                    + ComparisonValue(column.Loadout.GenericSlotAllocation));
                foreach (var category in column.Loadout.Categories)
                {
                    facts.Add(
                        $"capacity:{column.Kind}/{category.Category}/"
                        + $"{ComparisonValue(category.Capacity.Used)}/"
                        + $"{ComparisonValue(category.Capacity.Capacity)}/"
                        + $"{ComparisonValue(category.Capacity.Remaining)}/"
                        + $"{ComparisonValue(category.Capacity.CategoryContribution)}/"
                        + ComparisonValue(
                            category.Capacity.GenericContribution));
                    facts.AddRange(category.Skills.Select(skill =>
                        $"skill:{column.Kind}/{skill.Identity.Category}/"
                        + $"{skill.Identity.SkillId}/"
                        + $"{ComparisonValue(skill.Membership)}/"
                        + $"{ComparisonValue(skill.EffectiveCost)}/"
                        + string.Join(
                            ",",
                            skill.Actions.Select(action =>
                                $"{action.Kind}/{action.RequiredDirection}"))));
                }
            }

            var tactical = column.TacticalSummary;
            if (tactical is null)
            {
                continue;
            }

            facts.Add(
                $"tactical:{column.Kind}/"
                + $"{ComparisonValue(tactical.ManualActionCount)}/"
                + string.Join(",", tactical.CoveredThreats.Select(
                    value => value.Value))
                + "/"
                + string.Join(",", tactical.UnresolvedThreats.Select(
                    value => value.Value)));
            facts.AddRange(tactical.ScoreComponents.Select(score =>
                $"score:{column.Kind}/{score.Kind}/{score.Weight}/"
                + ComparisonValue(score.Score)));
        }

        return string.Join("\n", facts);
    }

    private static string ComparisonValue<T>(
        LoadoutComparisonValue<T> value) where T : notnull =>
        value.IsAvailable
            ? $"available:{value.Value}"
            : $"unavailable:{value.UnavailableReason}";

    private static CombatSkillCatalogueSourceIdentity Identity { get; } = new(
        "1.0.0-current",
        importerVersion: 1,
        new string('A', 64),
        new string('B', 64),
        new string('C', 64));
}
