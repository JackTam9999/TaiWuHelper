using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NSubstitute;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWuAPI.Contracts.CombatSkills;
using TaiWuAPI.Controllers;
using Xunit;

namespace TaiWu.API.UnitTests.Controllers;

public sealed class CombatSkillsControllerTests
{
    [Fact]
    public async Task Search_maps_typed_filters_sort_paging_and_safe_fields()
    {
        var definition = Definition(456, "Black Blood Gu");
        var (source, repository) = Current([definition]);
        var controller = new CombatSkillsController(
            source,
            repository,
            Substitute.For<ICharacterCombatSkillProgressReader>(),
            Substitute.For<ICharacterCombatSkillProgressCacheMaintenance>());

        var action = await controller.Search(
            query: "blood",
            language: CatalogueLanguage.English,
            sort: CombatSkillSearchSort.SkillId,
            category: CombatSkillDiscipline.Finger,
            grade: 5,
            faction: 1,
            element: CombatSkillElement.Wood,
            equipmentType: CombatSkillEquipmentType.Attack,
            offset: 0,
            limit: 25,
            CancellationToken);

        var response = Response<CombatSkillSearchResponse>(action);
        var item = Assert.Single(response.Items);
        Assert.Equal("combat-skill:456", item.Reference);
        Assert.Equal(456, item.Definition.SkillId);
        Assert.Equal(
            "Black Blood Gu",
            Assert.IsType<LocalizedCombatSkillNameResponse>(
                item.DisplayName.Value.Value).Text);
        Assert.Equal(CombatSkillCatalogueStatus.Current, response.Catalogue.Status);
        await repository.Received(1).QueryAsync(
            Arg.Is<CombatSkillCatalogueFilter>(filter =>
                filter != null
                && filter.Category == CombatSkillDiscipline.Finger
                && filter.Grade == new CombatSkillGrade(5)
                && filter.Faction == new CombatSkillFactionId(1)
                && filter.Element == CombatSkillElement.Wood
                && filter.EquipmentType == CombatSkillEquipmentType.Attack),
            CancellationToken);
        Assert.DoesNotContain("Value cannot be read", Serialize(response));
    }

    [Fact]
    public async Task Search_validation_is_stable_and_does_not_read_sources()
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        var controller = new CombatSkillsController(
            source,
            repository,
            Substitute.For<ICharacterCombatSkillProgressReader>(),
            Substitute.For<ICharacterCombatSkillProgressCacheMaintenance>());

        var action = await controller.Search(
            grade: 99,
            cancellationToken: CancellationToken);

        var problem = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(
            "Invalid combat-skill search parameters.",
            Assert.IsType<ProblemDetails>(problem.Value).Detail);
        Assert.Empty(source.ReceivedCalls());
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public async Task Status_preserves_unsupported_state_without_local_detail()
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>())
            .Returns(CombatSkillDefinitionSourceResult.UnsupportedVersion(
                @"Unsupported source at C:\secret\GameData.dll."));
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        var controller = new CombatSkillsController(
            source,
            repository,
            Substitute.For<ICharacterCombatSkillProgressReader>(),
            Substitute.For<ICharacterCombatSkillProgressCacheMaintenance>());

        var response = Response<CombatSkillCatalogueStatusResponse>(
            await controller.Status(CancellationToken));

        Assert.Equal(CombatSkillCatalogueStatus.UnsupportedVersion, response.Status);
        Assert.Equal(
            "The installed combat-skill source version is unsupported.",
            response.Reason);
        Assert.DoesNotContain("secret", Serialize(response));
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public async Task Details_returns_joined_progress_and_serializes_partial_fields()
    {
        var definition = Definition(456, "Black Blood Gu");
        var progress = Progress(42, 456);
        var (source, repository) = Current([definition]);
        var reader = ProgressReader([progress]);
        var controller = new CombatSkillsController(
            source,
            repository,
            reader,
            Substitute.For<ICharacterCombatSkillProgressCacheMaintenance>());

        var action = await controller.Details(
            456,
            CatalogueLanguage.English,
            characterId: null,
            CancellationToken);

        var response = Response<CombatSkillDetailsResponse>(action);
        Assert.True(response.DefinitionFound);
        Assert.Equal(CharacterProgressReadStatus.Available, response.ProgressStatus);
        Assert.NotNull(response.ProgressMetadata);
        Assert.Equal(42, response.CharacterState!.Progress!.CharacterId);
        Assert.Equal(15, response.CharacterState.Progress.StudySummary.TotalCount);
        Assert.Equal(
            113,
            Assert.IsType<int>(
                response.CharacterState.Progress.Power.Current.Value));
        Assert.Equal(
            100,
            Assert.IsType<int>(
                response.CharacterState.Progress.Power.Maximum.Value));
        Assert.Equal(
            CombatSkillPowerContext.OutOfCombat,
            response.CharacterState.Progress.Power.Context);
        Assert.Contains(
            CombatSkillQueryIssue.UnsupportedStudyMapping,
            response.Issues);
        var json = Serialize(response);
        Assert.Contains("Unavailable", json);
        Assert.DoesNotContain("local.sav", json, StringComparison.OrdinalIgnoreCase);
        await reader.Received(1).ReadAsync(
            Arg.Is<CharacterCombatSkillProgressReadRequest>(request =>
                request != null && request.CharacterId == null),
            CancellationToken);
    }

    [Fact]
    public async Task Details_sanitizes_save_failure_reason()
    {
        var definition = Definition(456, "Black Blood Gu");
        var (source, repository) = Current([definition]);
        var progressReader = Substitute.For<ICharacterCombatSkillProgressReader>();
        progressReader.ReadAsync(
                Arg.Any<CharacterCombatSkillProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(CharacterCombatSkillProgressReadResult.SaveReadFailed(
                @"Could not open C:\secret\world_1\local.sav."));
        var controller = new CombatSkillsController(
            source,
            repository,
            progressReader,
            Substitute.For<ICharacterCombatSkillProgressCacheMaintenance>());

        var response = Response<CombatSkillDetailsResponse>(
            await controller.Details(
                456,
                CatalogueLanguage.English,
                characterId: 42,
                CancellationToken));

        Assert.Equal(
            "The configured save could not be read.",
            response.ProgressFailureReason);
        Assert.DoesNotContain("secret", Serialize(response));
    }

    [Fact]
    public async Task Atlas_returns_current_progress_metadata_and_stable_entries()
    {
        var definition = Definition(456, "Black Blood Gu");
        var progress = Progress(42, 456);
        var (source, repository) = Current([definition]);
        var controller = new CharacterSkillAtlasController(
            source,
            repository,
            ProgressReader([progress]));

        var action = await controller.Read(
            characterId: 42,
            language: CatalogueLanguage.English,
            query: "blood",
            category: CombatSkillDiscipline.Finger,
            grade: 5,
            faction: 1,
            element: CombatSkillElement.Wood,
            equipmentType: CombatSkillEquipmentType.Attack,
            learned: true,
            hasProficiency: true,
            studyComplete: false,
            breakthroughReady: false,
            brokenThrough: false,
            activeDirection: null,
            attainmentMastered: null,
            simplified: false,
            activated: false,
            equipped: false,
            offset: 0,
            limit: 25,
            CancellationToken);

        var response = Response<CharacterCombatSkillAtlasResponse>(action);
        Assert.Equal(CharacterProgressReadStatus.Available, response.ProgressStatus);
        Assert.Equal(new string('E', 64), response.ProgressMetadata!.SaveSha256);
        Assert.Equal("combat-skill:456", Assert.Single(response.Entries).Reference);
    }

    [Fact]
    public async Task Atlas_preserves_missing_save_as_a_safe_result_state()
    {
        var definition = Definition(456, "Black Blood Gu");
        var (source, repository) = Current([definition]);
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();
        reader.ReadAsync(
                Arg.Any<CharacterCombatSkillProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(CharacterCombatSkillProgressReadResult.SaveMissing(
                @"Missing C:\secret\world_1\local.sav."));
        var controller = new CharacterSkillAtlasController(
            source,
            repository,
            reader);

        var response = Response<CharacterCombatSkillAtlasResponse>(
            await controller.Read(
                characterId: null,
                cancellationToken: CancellationToken));

        Assert.Equal(CharacterProgressReadStatus.SaveMissing, response.ProgressStatus);
        Assert.Equal("The configured save is unavailable.", response.ProgressFailureReason);
        Assert.Empty(response.Entries);
        Assert.DoesNotContain("secret", Serialize(response));
        await reader.Received(1).ReadAsync(
            Arg.Is<CharacterCombatSkillProgressReadRequest>(request =>
                request != null && request.CharacterId == null),
            CancellationToken);
    }

    [Fact]
    public async Task Cache_rebuild_is_explicit_and_only_replaces_derived_data()
    {
        var definition = Definition(456, "Black Blood Gu");
        var source = Source([definition]);
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns(new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Missing,
                sourceIdentity: null,
                definitionCount: 0,
                builtAtUtc: null));
        repository.ReplaceAsync(
                Arg.Any<CombatSkillCatalogueSourceIdentity>(),
                Arg.Any<IReadOnlyList<CombatSkillDefinition>>(),
                Arg.Any<IReadOnlyList<CombatSkillImportDiagnostic>>(),
                Arg.Any<CancellationToken>())
            .Returns(CatalogueReplaceResult.Success());
        var controller = new CombatSkillsController(
            source,
            repository,
            Substitute.For<ICharacterCombatSkillProgressReader>(),
            Substitute.For<ICharacterCombatSkillProgressCacheMaintenance>());

        var response = Response<CombatSkillCatalogueMaintenanceResponse>(
            await controller.RebuildCatalogueCache(CancellationToken));

        Assert.Equal(EnsureCombatSkillCatalogueStatus.Rebuilt, response.Status);
        await repository.Received(1).ReplaceAsync(
            CurrentIdentity,
            Arg.Is<IReadOnlyList<CombatSkillDefinition>>(values =>
                values != null && values.Count == 1 && values[0] == definition),
            Arg.Any<IReadOnlyList<CombatSkillImportDiagnostic>>(),
            CancellationToken);
    }

    [Fact]
    public async Task Progress_cache_clear_is_explicit_and_reports_derived_rows()
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        var maintenance =
            Substitute.For<ICharacterCombatSkillProgressCacheMaintenance>();
        maintenance.ClearAsync(Arg.Any<CancellationToken>()).Returns(2);
        var controller = new CombatSkillsController(
            source,
            repository,
            Substitute.For<ICharacterCombatSkillProgressReader>(),
            maintenance);

        var response = Response<CharacterProgressCacheMaintenanceResponse>(
            await controller.ClearProgressCache(CancellationToken));

        Assert.Equal(
            ClearCharacterCombatSkillProgressCacheStatus.Cleared,
            response.Status);
        Assert.Equal(2, response.ClearedSnapshotCount);
        Assert.Null(response.Reason);
        await maintenance.Received(1).ClearAsync(CancellationToken);
        Assert.Empty(source.ReceivedCalls());
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public void Routes_are_query_only_except_for_named_cache_maintenance()
    {
        Assert.Equal(
            "api/combat-skills",
            typeof(CombatSkillsController)
                .GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.Equal(
            "api/character-skill-atlas",
            typeof(CharacterSkillAtlasController)
                .GetCustomAttribute<RouteAttribute>()?.Template);

        var actions = new[]
            {
                typeof(CombatSkillsController),
                typeof(CharacterSkillAtlasController)
            }
            .SelectMany(type => type.GetMethods(
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.DeclaredOnly))
            .Where(method =>
                method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToArray();
        Assert.DoesNotContain(
            actions.SelectMany(action => action.GetParameters()),
            parameter => parameter.Name?.Contains(
                "path",
                StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(
            actions,
            action => action.GetCustomAttribute<HttpPutAttribute>() is not null
                || action.GetCustomAttribute<HttpPatchAttribute>() is not null
                || action.GetCustomAttribute<HttpDeleteAttribute>() is not null);
        Assert.Equal(
            ["catalogue-cache/rebuild", "progress-cache/clear"],
            actions
                .Select(action => action.GetCustomAttribute<HttpPostAttribute>())
                .Where(attribute => attribute is not null)
                .Select(attribute => attribute!.Template!)
                .Order(StringComparer.Ordinal)
                .ToArray());
        Assert.All(
            actions.Where(action =>
                action.GetCustomAttribute<HttpPostAttribute>() is not null),
            action => Assert.NotNull(
                action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()));
    }

    [Theory]
    [InlineData(CombatSkillCatalogueStatus.Missing)]
    [InlineData(CombatSkillCatalogueStatus.Stale)]
    [InlineData(CombatSkillCatalogueStatus.MissingSources)]
    [InlineData(CombatSkillCatalogueStatus.UnsupportedVersion)]
    [InlineData(CombatSkillCatalogueStatus.SourceReadFailed)]
    [InlineData(CombatSkillCatalogueStatus.RepositoryFailed)]
    [InlineData(CombatSkillCatalogueStatus.Corrupt)]
    [InlineData(CombatSkillCatalogueStatus.Rebuilding)]
    public void Catalogue_mapper_exposes_every_noncurrent_state_safely(
        CombatSkillCatalogueStatus status)
    {
        var response = CombatSkillResponseMapper.Map(
            new CombatSkillCatalogueStatusResult(
                status,
                DefinitionCount: 0,
                InstalledSource: null,
                StoredSource: null,
                BuiltAtUtc: null,
                @"Internal error at C:\secret\catalogue.db."));

        Assert.Equal(status, response.Status);
        Assert.NotNull(response.Reason);
        Assert.DoesNotContain("secret", Serialize(response));
    }

    private static T Response<T>(ActionResult<T> action)
    {
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        return Assert.IsType<T>(ok.Value);
    }

    private static string Serialize<T>(T value)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        return JsonSerializer.Serialize(value, options);
    }

    private static (
        ICombatSkillDefinitionSource Source,
        ICombatSkillCatalogueRepository Repository) Current(
            IReadOnlyList<CombatSkillDefinition> definitions)
    {
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns(new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Ready,
                CurrentIdentity,
                definitions.Count,
                DateTimeOffset.Parse("2026-08-02T12:00:00Z")));
        repository.QueryAsync(
                Arg.Any<CombatSkillCatalogueFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(definitions);
        repository.GetAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => definitions.FirstOrDefault(
                definition => definition.SkillId == call.ArgAt<int>(0)));
        return (Source(definitions), repository);
    }

    private static ICombatSkillDefinitionSource Source(
        IReadOnlyList<CombatSkillDefinition> definitions)
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>())
            .Returns(CombatSkillDefinitionSourceResult.Available(
                CurrentIdentity,
                definitions));
        return source;
    }

    private static ICharacterCombatSkillProgressReader ProgressReader(
        IReadOnlyList<CharacterCombatSkillProgress> progress)
    {
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();
        reader.ReadAsync(
                Arg.Any<CharacterCombatSkillProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(CharacterCombatSkillProgressReadResult.Available(
                new CharacterCombatSkillProgressMetadata(
                    progress[0].SaveSnapshot,
                    "1.0.0-test"),
                progress));
        return reader;
    }

    private static CombatSkillDefinition Definition(int skillId, string name)
    {
        var source = new CatalogueSourceReference(
            CatalogueSourceKind.GameData,
            "gamedata:test",
            $"combat-skill:{skillId}");
        return new CombatSkillDefinition(
            skillId,
            new CombatSkillLocalizedNames(
                [new LocalizedCombatSkillName(
                    CatalogueLanguage.English,
                    name,
                    new CatalogueSourceReference(
                        CatalogueSourceKind.EnglishLanguageResource,
                        "language-en:test",
                        $"combat-skill-name:{skillId}"))]),
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
                new CombatSkillGridCost(3),
                source),
            CatalogueField<SkillSlotContribution>.Unsupported(
                "Unsupported in test.",
                source),
            requirements: null,
            new CombatSkillTimingDefinition(
                CatalogueField<int>.Unavailable("Unavailable in test."),
                CatalogueField<int>.Unavailable("Unavailable in test."),
                CatalogueField<int>.Unavailable("Unavailable in test.")),
            new CombatSkillEffectReferences(
                CatalogueField<CombatSkillEffectId>.Unavailable(
                    "Unavailable in test."),
                CatalogueField<CombatSkillEffectId>.Unavailable(
                    "Unavailable in test."),
                CatalogueField<CombatSkillEffectId>.Unavailable(
                    "Unavailable in test.")),
            rawDescriptions: null,
            source);
    }

    private static CharacterCombatSkillProgress Progress(
        int characterId,
        int skillId)
    {
        var source = new SkillProgressSource(
            SkillProgressSourceKind.SaveSnapshot,
            $"save:{new string('E', 64)}",
            "test");
        var details = Enumerable.Range(0, 15)
            .Select(index => new CombatSkillStudyDetailProgress(
                $"outline-{index}",
                index,
                CombatSkillStudyDetailGroup.Outline,
                CatalogueField<string>.Unavailable(
                    "Label unavailable in test."),
                index == 14
                    ? SkillProgressField<CombatSkillStudyState>.Unavailable(
                        "Study mapping unavailable in test.",
                        source)
                    : SkillProgressField<CombatSkillStudyState>.Available(
                        CombatSkillStudyState.NotRead,
                        source),
                SkillProgressField<bool>.Available(false, source)))
            .ToArray();
        return new CharacterCombatSkillProgress(
            characterId,
            new SaveSnapshotIdentity(
                new string('E', 64),
                DateTimeOffset.Parse("2026-08-02T12:00:00Z")),
            skillId,
            SkillProgressField<bool>.Available(true, source),
            new CombatSkillProficiencyProgress(
                SkillProgressField<int>.Available(50, source),
                SkillProgressField<int>.Available(100, source)),
            new CombatSkillPowerProgress(
                SkillProgressField<int>.Available(113, source),
                SkillProgressField<int>.Available(100, source),
                CombatSkillPowerContext.OutOfCombat),
            details,
            SkillProgressField<BreakthroughDirectionAvailability>.Available(
                new BreakthroughDirectionAvailability(false, false, []),
                source),
            SkillProgressField<PracticeDirection>.Unavailable(
                "No active direction.",
                source),
            SkillProgressField<bool>.Unavailable(
                "Attainment mastery unavailable.",
                source),
            SkillProgressField<bool>.Available(false, source),
            SkillProgressField<bool>.Available(false, source),
            SkillProgressField<bool>.Available(false, source));
    }

    private static CombatSkillCatalogueSourceIdentity CurrentIdentity { get; } =
        new(
            "1.0.0-test",
            1,
            new string('0', 64),
            new string('A', 64),
            new string('B', 64));

    private static CancellationToken CancellationToken =>
        TestContext.Current.CancellationToken;
}
