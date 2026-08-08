using NSubstitute;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Application.UnitTests.CombatSkills;

public sealed class ResolveTargetSkillSelectionTests
{
    private static readonly CancellationToken CancellationToken =
        TestContext.Current.CancellationToken;

    [Fact]
    public async Task English_query_resolves_Traditional_Chinese_display_name()
    {
        var definitions = new[]
        {
            Definition(
                1,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.TraditionalChinese, "黑血蠱降"),
                (CatalogueLanguage.English, "Corruptive Gu Infection"))
        };
        var useCase = UseCase(definitions);

        var pending = await useCase.ExecuteAsync(
            Request(
                "Corruptive Gu Infection",
                SkillCategory.Attack,
                CatalogueLanguage.TraditionalChinese),
            CancellationToken);
        var candidate = Assert.Single(pending.Candidates);

        Assert.Equal(
            TargetSkillSelectionStatus.ConfirmationRequired,
            pending.Status);
        Assert.Equal(TargetSkillMatchKind.Exact, candidate.MatchKind);
        Assert.Equal("黑血蠱降", candidate.DisplayName.Value.Value.Text);
        Assert.False(candidate.DisplayName.UsedFallback);

        var resolved = await useCase.ExecuteAsync(
            Request(
                "Corruptive Gu Infection",
                SkillCategory.Attack,
                CatalogueLanguage.TraditionalChinese,
                confirmedSkillId: candidate.SkillId,
                observationContext: TargetObservationContext.Story,
                visiblePowerPercent: 142),
            CancellationToken);

        Assert.Equal(TargetSkillSelectionStatus.Resolved, resolved.Status);
        Assert.Equal(
            candidate.SkillId,
            resolved.ResolvedSelection!.Observation.SkillId);
        Assert.Equal(
            142,
            resolved.ResolvedSelection.Observation.VisiblePowerPercent);
    }

    [Fact]
    public async Task Traditional_Chinese_query_can_use_English_fallback()
    {
        var definitions = new[]
        {
            Definition(
                2,
                CombatSkillEquipmentType.Defense,
                (CatalogueLanguage.TraditionalChinese, "鐵鼎金身功"))
        };

        var result = await UseCase(definitions).ExecuteAsync(
            Request(
                "鐵鼎金身功",
                SkillCategory.Defense,
                CatalogueLanguage.English),
            CancellationToken);

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal("鐵鼎金身功", candidate.DisplayName.Value.Value.Text);
        Assert.True(candidate.DisplayName.UsedFallback);
    }

    [Fact]
    public async Task Exact_matches_rank_before_partial_matches_deterministically()
    {
        var definitions = new[]
        {
            Definition(
                3,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Alpha Blade")),
            Definition(
                2,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Alpha")),
            Definition(
                1,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Alpha"))
        };

        var result = await UseCase(definitions).ExecuteAsync(
            Request("  Ａｌｐｈａ  ", SkillCategory.Attack),
            CancellationToken);

        Assert.Equal(TargetSkillSelectionStatus.Ambiguous, result.Status);
        Assert.Equal([1, 2, 3], result.Candidates.Select(value => value.SkillId));
        Assert.Equal(
            [TargetSkillMatchKind.Exact, TargetSkillMatchKind.Exact,
                TargetSkillMatchKind.Partial],
            result.Candidates.Select(value => value.MatchKind));
        Assert.Null(result.ResolvedSelection);
    }

    [Fact]
    public async Task Ambiguous_match_requires_explicit_stable_id_confirmation()
    {
        var definitions = new[]
        {
            Definition(
                1,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Alpha")),
            Definition(
                2,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Alpha Blade"))
        };
        var useCase = UseCase(definitions);

        var result = await useCase.ExecuteAsync(
            Request(
                "Alpha",
                SkillCategory.Attack,
                confirmedSkillId: 2,
                direction: PracticeDirection.Reverse,
                slotIndex: 3),
            CancellationToken);

        Assert.Equal(TargetSkillSelectionStatus.Resolved, result.Status);
        Assert.Equal(2, result.ResolvedSelection!.Observation.SkillId);
        Assert.Equal(
            PracticeDirection.Reverse,
            result.ResolvedSelection.Observation.Direction);
        Assert.Equal(3, result.ResolvedSelection.Observation.SlotIndex);
    }

    [Fact]
    public async Task Confirmed_category_must_match_verified_static_definition()
    {
        var definitions = new[]
        {
            Definition(
                10,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Strike"))
        };

        var result = await UseCase(definitions).ExecuteAsync(
            Request(
                "Strike",
                SkillCategory.Defense,
                confirmedSkillId: 10),
            CancellationToken);

        Assert.Equal(TargetSkillSelectionStatus.CategoryMismatch, result.Status);
        Assert.Null(result.ResolvedSelection);
        Assert.Equal(
            SkillCategory.Attack,
            Assert.Single(result.Candidates).StaticFacts!.Category);
    }

    [Fact]
    public async Task Missing_or_unconfirmed_definition_is_explicit()
    {
        var definitions = new[]
        {
            Definition(
                10,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Strike"))
        };
        var useCase = UseCase(definitions);

        var missing = await useCase.ExecuteAsync(
            Request("Missing", SkillCategory.Attack),
            CancellationToken);
        var invalid = await useCase.ExecuteAsync(
            Request(
                "Strike",
                SkillCategory.Attack,
                confirmedSkillId: 999),
            CancellationToken);

        Assert.Equal(
            TargetSkillSelectionStatus.DefinitionMissing,
            missing.Status);
        Assert.Equal(
            TargetSkillSelectionStatus.ConfirmationInvalid,
            invalid.Status);
        Assert.Null(missing.ResolvedSelection);
        Assert.Null(invalid.ResolvedSelection);
    }

    [Fact]
    public async Task Unsupported_static_category_cannot_be_confirmed()
    {
        var definition = Definition(
            10,
            equipmentType: null,
            (CatalogueLanguage.English, "Unknown Category"));

        var result = await UseCase([definition]).ExecuteAsync(
            Request(
                "Unknown Category",
                SkillCategory.Attack,
                confirmedSkillId: 10),
            CancellationToken);

        Assert.Equal(
            TargetSkillSelectionStatus.DefinitionUnsupported,
            result.Status);
        Assert.Null(Assert.Single(result.Candidates).StaticFacts);
    }

    [Fact]
    public async Task Skill_absent_from_target_snapshot_remains_resolvable()
    {
        var definition = Definition(
            10,
            CombatSkillEquipmentType.Attack,
            (CatalogueLanguage.English, "Observed Strike"));

        var result = await UseCase([definition]).ExecuteAsync(
            Request(
                "Observed Strike",
                SkillCategory.Attack,
                confirmedSkillId: 10,
                targetSnapshotSkillIds: []),
            CancellationToken);

        var resolved = Assert.IsType<ResolvedTargetSkillSelection>(
            result.ResolvedSelection);
        Assert.Equal(TargetSkillSnapshotPresence.Absent, resolved.SnapshotPresence);
        Assert.Equal(1010, resolved.StaticFacts.DirectEffect.Value.Value);
        Assert.Equal(2010, resolved.StaticFacts.ReverseEffect.Value.Value);
        var projected = resolved.StaticFacts.CreateSnapshot(
            resolved.Observation);
        Assert.Equal(10, projected.SkillId);
        Assert.Equal(SkillCategory.Attack, projected.Category);
        Assert.Equal(1010, projected.DirectEffectId.Value);
        Assert.Equal(2010, projected.ReverseEffectId.Value);
        Assert.False(projected.Mastered.IsAvailable);
        Assert.DoesNotContain(
            typeof(TargetSkillStaticFacts).GetProperties(),
            property => property.Name.Contains(
                "Description",
                StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, TargetSkillSnapshotPresence.Unknown)]
    [InlineData(10, TargetSkillSnapshotPresence.Present)]
    [InlineData(20, TargetSkillSnapshotPresence.Absent)]
    public async Task Target_snapshot_presence_is_explicit(
        int? snapshotSkillId,
        TargetSkillSnapshotPresence expected)
    {
        var definition = Definition(
            10,
            CombatSkillEquipmentType.Attack,
            (CatalogueLanguage.English, "Strike"));
        IEnumerable<int>? snapshotIds = snapshotSkillId is null
            ? null
            : [snapshotSkillId.Value];

        var result = await UseCase([definition]).ExecuteAsync(
            Request(
                "Strike",
                SkillCategory.Attack,
                confirmedSkillId: 10,
                targetSnapshotSkillIds: snapshotIds),
            CancellationToken);

        Assert.Equal(expected, result.ResolvedSelection!.SnapshotPresence);
    }

    [Theory]
    [InlineData(
        CombatSkillCatalogueStatus.Missing,
        TargetSkillSelectionStatus.CatalogueMissing)]
    [InlineData(
        CombatSkillCatalogueStatus.MissingSources,
        TargetSkillSelectionStatus.CatalogueMissing)]
    [InlineData(
        CombatSkillCatalogueStatus.Stale,
        TargetSkillSelectionStatus.CatalogueStale)]
    [InlineData(
        CombatSkillCatalogueStatus.Rebuilding,
        TargetSkillSelectionStatus.CatalogueRebuilding)]
    [InlineData(
        CombatSkillCatalogueStatus.UnsupportedVersion,
        TargetSkillSelectionStatus.CatalogueUnsupportedVersion)]
    [InlineData(
        CombatSkillCatalogueStatus.RepositoryFailed,
        TargetSkillSelectionStatus.CatalogueUnavailable)]
    public void Lifecycle_status_mapping_never_guesses_a_skill(
        CombatSkillCatalogueStatus catalogueStatus,
        TargetSkillSelectionStatus expected)
    {
        Assert.Equal(
            expected,
            TargetSkillSelectionResult.MapCatalogueStatus(catalogueStatus));
    }

    [Fact]
    public async Task Missing_stale_and_unsupported_catalogues_return_no_candidates()
    {
        var definition = Definition(
            10,
            CombatSkillEquipmentType.Attack,
            (CatalogueLanguage.English, "Strike"));
        var missing = await UseCase(
                [definition],
                new CombatSkillCatalogueRepositorySnapshot(
                    CatalogueRepositoryState.Missing,
                    sourceIdentity: null,
                    definitionCount: 0,
                    builtAtUtc: null))
            .ExecuteAsync(
                Request("Strike", SkillCategory.Attack),
                CancellationToken);
        var stale = await UseCase(
                [definition],
                Ready(OtherIdentity, 1))
            .ExecuteAsync(
                Request("Strike", SkillCategory.Attack),
                CancellationToken);
        var unsupported = await new ResolveTargetSkillSelection(
                Source(CombatSkillDefinitionSourceResult.UnsupportedVersion(
                    "unsupported")),
                Repository(Ready(CurrentIdentity, 1), [definition]))
            .ExecuteAsync(
                Request("Strike", SkillCategory.Attack),
                CancellationToken);

        Assert.Equal(TargetSkillSelectionStatus.CatalogueMissing, missing.Status);
        Assert.Equal(TargetSkillSelectionStatus.CatalogueStale, stale.Status);
        Assert.Equal(
            TargetSkillSelectionStatus.CatalogueUnsupportedVersion,
            unsupported.Status);
        Assert.Empty(missing.Candidates);
        Assert.Empty(stale.Candidates);
        Assert.Empty(unsupported.Candidates);
    }

    [Fact]
    public void Request_accepts_battle_context_and_rejects_invalid_values()
    {
        var hostile = Request(
            "Strike",
            SkillCategory.Attack,
            observationContext: TargetObservationContext.Hostile,
            visiblePowerPercent: 146);

        Assert.Equal(TargetObservationContext.Hostile,
            hostile.ObservationContext);
        Assert.Equal(146, hostile.VisiblePowerPercent);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Request(
                "Strike",
                SkillCategory.Attack,
                observationContext: (TargetObservationContext)99));
        Assert.Throws<ArgumentException>(
            () => Request(" ", SkillCategory.Attack));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Request("Strike", (SkillCategory)99));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Request(
                "Strike",
                SkillCategory.Attack,
                confirmedSkillId: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Request(
                "Strike",
                SkillCategory.Attack,
                direction: PracticeDirection.Neutral));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Request(
                "Strike",
                SkillCategory.Attack,
                slotIndex: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Request(
                "Strike",
                SkillCategory.Attack,
                visiblePowerPercent: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Request(
                "Strike",
                SkillCategory.Attack,
                targetSnapshotSkillIds: [-1]));
        Assert.Throws<ArgumentException>(
            () => Request(
                "Strike",
                SkillCategory.Attack,
                targetSnapshotSkillIds: [1, 1]));
    }

    private static TargetSkillSelectionRequest Request(
        string query,
        SkillCategory category,
        CatalogueLanguage language = CatalogueLanguage.English,
        int? confirmedSkillId = null,
        PracticeDirection? direction = null,
        int? slotIndex = null,
        IEnumerable<int>? targetSnapshotSkillIds = null,
        TargetObservationContext observationContext =
            TargetObservationContext.Sparring,
        int? visiblePowerPercent = null) => new(
                observationContext,
                language,
                query,
                category,
                confirmedSkillId,
                direction,
                slotIndex,
                targetSnapshotSkillIds,
                visiblePowerPercent);

    private static ResolveTargetSkillSelection UseCase(
        IReadOnlyList<CombatSkillDefinition> definitions,
        CombatSkillCatalogueRepositorySnapshot? snapshot = null) => new(
            Source(CombatSkillDefinitionSourceResult.Available(
                CurrentIdentity,
                definitions)),
            Repository(
                snapshot ?? Ready(CurrentIdentity, definitions.Count),
                definitions));

    private static ICombatSkillDefinitionSource Source(
        CombatSkillDefinitionSourceResult result)
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>()).Returns(result);
        return source;
    }

    private static ICombatSkillCatalogueRepository Repository(
        CombatSkillCatalogueRepositorySnapshot snapshot,
        IReadOnlyList<CombatSkillDefinition> definitions)
    {
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        repository.QueryAsync(
                Arg.Any<CombatSkillCatalogueFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(definitions);
        return repository;
    }

    private static CombatSkillDefinition Definition(
        int skillId,
        CombatSkillEquipmentType? equipmentType,
        params (CatalogueLanguage Language, string Text)[] names)
    {
        var source = new CatalogueSourceReference(
            CatalogueSourceKind.GameData,
            "gamedata:test",
            $"combat-skill:{skillId}");
        CatalogueField<CombatSkillEquipmentType> category =
            equipmentType is null
                ? CatalogueField<CombatSkillEquipmentType>.Unavailable(
                    "not mapped")
                : CatalogueField<CombatSkillEquipmentType>.Available(
                    equipmentType.Value,
                    source);
        return new CombatSkillDefinition(
            skillId,
            new CombatSkillLocalizedNames(names.Select(name =>
                new LocalizedCombatSkillName(
                    name.Language,
                    name.Text,
                    new CatalogueSourceReference(
                        name.Language == CatalogueLanguage.English
                            ? CatalogueSourceKind.EnglishLanguageResource
                            : CatalogueSourceKind
                                .TraditionalChineseLanguageResource,
                        name.Language == CatalogueLanguage.English
                            ? "language-en:test"
                            : "language-cnh:test",
                        $"combat-skill-name:{skillId}")))),
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
            category,
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
                    new CombatSkillEffectId(1000 + skillId),
                    source),
                CatalogueField<CombatSkillEffectId>.Available(
                    new CombatSkillEffectId(2000 + skillId),
                    source),
                CatalogueField<CombatSkillEffectId>.Unavailable("not needed")),
            rawDescriptions:
            [
                new RawCombatSkillDescription(
                    RawCombatSkillDescriptionKind.Effect,
                    CatalogueLanguage.English,
                    "Unverified free-form mechanic text.",
                    source)
            ],
            source);
    }

    private static CombatSkillCatalogueRepositorySnapshot Ready(
        CombatSkillCatalogueSourceIdentity identity,
        int count) => new(
            CatalogueRepositoryState.Ready,
            identity,
            count,
            DateTimeOffset.Parse("2026-08-07T20:00:00Z"));

    private static CombatSkillCatalogueSourceIdentity CurrentIdentity { get; } =
        Identity("1.0.0-current", 'A');

    private static CombatSkillCatalogueSourceIdentity OtherIdentity { get; } =
        Identity("1.0.0-other", 'B');

    private static CombatSkillCatalogueSourceIdentity Identity(
        string version,
        char fingerprint) => new(
            version,
            importerVersion: 1,
            new string(fingerprint, 64),
            new string(fingerprint, 64),
            new string(fingerprint, 64));
}
