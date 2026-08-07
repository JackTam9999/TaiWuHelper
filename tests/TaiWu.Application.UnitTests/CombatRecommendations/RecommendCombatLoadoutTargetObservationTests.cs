using NSubstitute;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.TargetObservations;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using Xunit;

namespace TaiWu.Application.UnitTests.CombatRecommendations;

public sealed class RecommendCombatLoadoutTargetObservationTests
{
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
            snapshot.Metadata.SavePath,
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
    }

    [Fact]
    public async Task Snapshot_absent_observed_skill_adds_verified_observed_threat_deterministically()
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
        var resolver = new ResolveTargetSkillSelection(
            Source(definition),
            Repository(definition));
        var workflow = new TargetObservationRecommendationWorkflow(
            reader,
            resolver);
        var observedRequest = new RecommendCombatLoadoutRequest(
            snapshot.Metadata.SavePath,
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
                snapshot.Metadata.SavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Balanced),
            TestContext.Current.CancellationToken);

        var first = await workflow.ExecuteAsync(
            observedRequest,
            TestContext.Current.CancellationToken);
        var second = await workflow.ExecuteAsync(
            observedRequest,
            TestContext.Current.CancellationToken);

        var finding = Assert.Single(first.ThreatAnalysis.Threats);
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
        Assert.Equal(
            DecisionFingerprint(saveOnly),
            DecisionFingerprint(first));
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
    public void Observation_contract_rejects_hidden_context_and_local_path()
    {
        Assert.Throws<ArgumentException>(() => new TargetObservationRequest(
            TargetObservationContext.Story,
            SaveTime,
            "E3-000-CAP-001",
            TargetLoadoutCoverageKind.PartialLoadout,
            []));
        Assert.Throws<ArgumentException>(() => new TargetObservationRequest(
            TargetObservationContext.Sparring,
            SaveTime,
            @"C:\captures\target.png",
            TargetLoadoutCoverageKind.PartialLoadout,
            []));
    }

    private static RecommendCombatLoadoutRequest Request(
        TargetObservedSkillRequest selection)
    {
        var snapshot = Snapshot();
        return new RecommendCombatLoadoutRequest(
            snapshot.Metadata.SavePath,
            snapshot.Target.CharacterId,
            RecommendationPolicy.Balanced,
            targetObservation: new TargetObservationRequest(
                TargetObservationContext.Sparring,
                SaveTime.AddMinutes(1),
                "E3-000-CAP-002",
                TargetLoadoutCoverageKind.PartialLoadout,
                [selection]));
    }

    private static CombatSnapshot Snapshot(
        CombatSkillSnapshot[]? targetSkills = null,
        SnapshotValue<CombatLoadoutSnapshot>? targetEquippedSkills = null)
    {
        var skills = targetSkills ?? [TargetSkill(719, "Target Art")];
        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                @"C:\Taiwu\local.sav",
                new string('A', 64),
                SaveTime,
                SnapshotValue<DateTimeOffset>.Available(SaveTime),
                SnapshotValue<string>.Available(
                    TargetLoadoutCompletenessEvidence
                        .E3000GameDataVersion)),
            new PlayerCombatSnapshot(
                1,
                SnapshotValue<string>.Available("Taiwu"),
                learnedSkills: [],
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

    private static CombatSkillSnapshot TargetSkill(
        int skillId,
        string name,
        int? directEffectId = null,
        int? reverseEffectId = null) => new(
            skillId,
            SnapshotValue<string>.Available(name),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(2),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct),
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

    private static CombatSkillCatalogueSourceIdentity Identity { get; } = new(
        "1.0.0-current",
        importerVersion: 1,
        new string('A', 64),
        new string('B', 64),
        new string('C', 64));
}
