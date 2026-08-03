using NSubstitute;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Application.UnitTests.CombatSkills;

public sealed class CombatSkillCatalogueUseCaseTests
{
    [Fact]
    public void Progress_read_request_carries_validated_language_selection()
    {
        var defaultRequest = new CharacterCombatSkillProgressReadRequest(42);
        var english = new CharacterCombatSkillProgressReadRequest(
            42,
            CatalogueLanguage.English);

        Assert.Equal(
            CatalogueLanguage.TraditionalChinese,
            defaultRequest.PreferredLanguage);
        Assert.Equal(CatalogueLanguage.English, english.PreferredLanguage);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CharacterCombatSkillProgressReadRequest(
                42,
                (CatalogueLanguage)999));
    }

    [Fact]
    public async Task Status_is_current_only_when_manifest_and_count_match()
    {
        var definitions = Definitions();
        var repository = Repository(Ready(CurrentIdentity, definitions.Length));
        var useCase = new ReadCombatSkillCatalogueStatus(
            Source(Available(CurrentIdentity, definitions)),
            repository);

        var result = await useCase.ExecuteAsync(CancellationToken);

        Assert.Equal(CombatSkillCatalogueStatus.Current, result.Status);
        Assert.Equal(definitions.Length, result.DefinitionCount);
        Assert.Equal(CurrentIdentity, result.InstalledSource);
        Assert.Equal(CurrentIdentity, result.StoredSource);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Status_reports_stale_manifest_or_definition_count(
        bool differentManifest,
        bool differentCount)
    {
        var definitions = Definitions();
        var storedIdentity = differentManifest ? OlderIdentity : CurrentIdentity;
        var storedCount = differentCount
            ? definitions.Length - 1
            : definitions.Length;
        var useCase = new ReadCombatSkillCatalogueStatus(
            Source(Available(CurrentIdentity, definitions)),
            Repository(Ready(storedIdentity, storedCount)));

        var result = await useCase.ExecuteAsync(CancellationToken);

        Assert.Equal(CombatSkillCatalogueStatus.Stale, result.Status);
        Assert.Equal(CurrentIdentity, result.InstalledSource);
        Assert.Equal(storedIdentity, result.StoredSource);
    }

    [Theory]
    [InlineData(DefinitionSourceReadStatus.MissingSources,
        CombatSkillCatalogueStatus.MissingSources)]
    [InlineData(DefinitionSourceReadStatus.UnsupportedVersion,
        CombatSkillCatalogueStatus.UnsupportedVersion)]
    [InlineData(DefinitionSourceReadStatus.Failed,
        CombatSkillCatalogueStatus.SourceReadFailed)]
    public async Task Status_preserves_definition_source_failures(
        DefinitionSourceReadStatus sourceStatus,
        CombatSkillCatalogueStatus expected)
    {
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        var useCase = new ReadCombatSkillCatalogueStatus(
            Source(FailedSource(sourceStatus)),
            repository);

        var result = await useCase.ExecuteAsync(CancellationToken);

        Assert.Equal(expected, result.Status);
        Assert.Equal("source diagnostic", result.Reason);
        await repository.DidNotReceive().ReadStateAsync(
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(CatalogueRepositoryState.Missing,
        CombatSkillCatalogueStatus.Missing)]
    [InlineData(CatalogueRepositoryState.Corrupt,
        CombatSkillCatalogueStatus.Corrupt)]
    [InlineData(CatalogueRepositoryState.Failed,
        CombatSkillCatalogueStatus.RepositoryFailed)]
    public async Task Status_preserves_repository_state(
        CatalogueRepositoryState repositoryState,
        CombatSkillCatalogueStatus expected)
    {
        var snapshot = repositoryState == CatalogueRepositoryState.Missing
            ? new CombatSkillCatalogueRepositorySnapshot(
                repositoryState,
                sourceIdentity: null,
                definitionCount: 0,
                builtAtUtc: null)
            : new CombatSkillCatalogueRepositorySnapshot(
                repositoryState,
                sourceIdentity: null,
                definitionCount: 0,
                builtAtUtc: null,
                "repository diagnostic");
        var result = await new ReadCombatSkillCatalogueStatus(
                Source(Available(CurrentIdentity, Definitions())),
                Repository(snapshot))
            .ExecuteAsync(CancellationToken);

        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public async Task Status_converts_unexpected_port_failures_to_typed_results()
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CombatSkillDefinitionSourceResult>(
                new IOException("source exploded")));
        var sourceFailure = await new ReadCombatSkillCatalogueStatus(
                source,
                Repository(Ready(CurrentIdentity, 3)))
            .ExecuteAsync(CancellationToken);
        Assert.Equal(
            CombatSkillCatalogueStatus.SourceReadFailed,
            sourceFailure.Status);

        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CombatSkillCatalogueRepositorySnapshot>(
                new IOException("repository exploded")));
        var repositoryFailure = await new ReadCombatSkillCatalogueStatus(
                Source(Available(CurrentIdentity, Definitions())),
                repository)
            .ExecuteAsync(CancellationToken);
        Assert.Equal(
            CombatSkillCatalogueStatus.RepositoryFailed,
            repositoryFailure.Status);
    }

    [Fact]
    public async Task Ensure_does_not_replace_a_current_catalogue()
    {
        var definitions = Definitions();
        var repository = Repository(Ready(CurrentIdentity, definitions.Length));
        var result = await new EnsureCombatSkillCatalogue(
                Source(Available(CurrentIdentity, definitions)),
                repository)
            .ExecuteAsync(CancellationToken);

        Assert.Equal(EnsureCombatSkillCatalogueStatus.Current, result.Status);
        await repository.DidNotReceive().ReplaceAsync(
            Arg.Any<CombatSkillCatalogueSourceIdentity>(),
            Arg.Any<IReadOnlyList<CombatSkillDefinition>>(),
            Arg.Any<IReadOnlyList<CombatSkillImportDiagnostic>>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(CatalogueRepositoryState.Missing)]
    [InlineData(CatalogueRepositoryState.Corrupt)]
    [InlineData(CatalogueRepositoryState.Ready)]
    public async Task Ensure_replaces_missing_stale_or_corrupt_catalogue(
        CatalogueRepositoryState state)
    {
        var definitions = Definitions();
        var snapshot = state switch
        {
            CatalogueRepositoryState.Missing => new(
                state, null, 0, null),
            CatalogueRepositoryState.Ready => Ready(
                OlderIdentity,
                definitions.Length),
            _ => new CombatSkillCatalogueRepositorySnapshot(
                state, null, 0, null, "unhealthy")
        };
        var repository = Repository(snapshot);
        repository.ReplaceAsync(
                Arg.Any<CombatSkillCatalogueSourceIdentity>(),
                Arg.Any<IReadOnlyList<CombatSkillDefinition>>(),
                Arg.Any<IReadOnlyList<CombatSkillImportDiagnostic>>(),
                Arg.Any<CancellationToken>())
            .Returns(CatalogueReplaceResult.Success());

        var result = await new EnsureCombatSkillCatalogue(
                Source(Available(CurrentIdentity, definitions)),
                repository)
            .ExecuteAsync(CancellationToken);

        Assert.Equal(EnsureCombatSkillCatalogueStatus.Rebuilt, result.Status);
        Assert.Equal(definitions.Length, result.DefinitionCount);
        await repository.Received(1).ReplaceAsync(
            CurrentIdentity,
            Arg.Is<IReadOnlyList<CombatSkillDefinition>>(values =>
                values != null
                && values.Select(value => value.SkillId)
                    .SequenceEqual(new[] { 1, 2, 3 })),
            Arg.Is<IReadOnlyList<CombatSkillImportDiagnostic>>(values =>
                values != null && values.Count == 0),
            CancellationToken);
    }

    [Fact]
    public async Task Ensure_does_not_write_when_repository_is_unavailable()
    {
        var repository = Repository(new CombatSkillCatalogueRepositorySnapshot(
            CatalogueRepositoryState.Failed,
            sourceIdentity: null,
            definitionCount: 0,
            builtAtUtc: null,
            "access denied"));

        var result = await new EnsureCombatSkillCatalogue(
                Source(Available(CurrentIdentity, Definitions())),
                repository)
            .ExecuteAsync(CancellationToken);

        Assert.Equal(
            EnsureCombatSkillCatalogueStatus.RebuildFailed,
            result.Status);
        Assert.Equal(
            CatalogueRecoveryStatus.RepositoryUnavailable,
            result.RecoveryStatus);
        await repository.DidNotReceive().ReplaceAsync(
            Arg.Any<CombatSkillCatalogueSourceIdentity>(),
            Arg.Any<IReadOnlyList<CombatSkillDefinition>>(),
            Arg.Any<IReadOnlyList<CombatSkillImportDiagnostic>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ensure_converts_repository_read_exception_to_unavailable()
    {
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CombatSkillCatalogueRepositorySnapshot>(
                new IOException("read failed")));

        var result = await new EnsureCombatSkillCatalogue(
                Source(Available(CurrentIdentity, Definitions())),
                repository)
            .ExecuteAsync(CancellationToken);

        Assert.Equal(
            EnsureCombatSkillCatalogueStatus.RebuildFailed,
            result.Status);
        Assert.Equal(
            CatalogueRecoveryStatus.RepositoryUnavailable,
            result.RecoveryStatus);
        await repository.DidNotReceive().ReplaceAsync(
            Arg.Any<CombatSkillCatalogueSourceIdentity>(),
            Arg.Any<IReadOnlyList<CombatSkillDefinition>>(),
            Arg.Any<IReadOnlyList<CombatSkillImportDiagnostic>>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(DefinitionSourceReadStatus.MissingSources,
        EnsureCombatSkillCatalogueStatus.MissingSources)]
    [InlineData(DefinitionSourceReadStatus.UnsupportedVersion,
        EnsureCombatSkillCatalogueStatus.UnsupportedVersion)]
    [InlineData(DefinitionSourceReadStatus.Failed,
        EnsureCombatSkillCatalogueStatus.SourceReadFailed)]
    public async Task Ensure_preserves_source_failure_without_writing(
        DefinitionSourceReadStatus sourceStatus,
        EnsureCombatSkillCatalogueStatus expected)
    {
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        var result = await new EnsureCombatSkillCatalogue(
                Source(FailedSource(sourceStatus)),
                repository)
            .ExecuteAsync(CancellationToken);

        Assert.Equal(expected, result.Status);
        await repository.DidNotReceive().ReplaceAsync(
            Arg.Any<CombatSkillCatalogueSourceIdentity>(),
            Arg.Any<IReadOnlyList<CombatSkillDefinition>>(),
            Arg.Any<IReadOnlyList<CombatSkillImportDiagnostic>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ensure_reports_rebuild_failure_from_result_or_exception()
    {
        var definitions = Definitions();
        var repository = Repository(MissingRepository());
        repository.ReplaceAsync(
                Arg.Any<CombatSkillCatalogueSourceIdentity>(),
                Arg.Any<IReadOnlyList<CombatSkillDefinition>>(),
                Arg.Any<IReadOnlyList<CombatSkillImportDiagnostic>>(),
                Arg.Any<CancellationToken>())
            .Returns(CatalogueReplaceResult.Failure("disk full"));
        var failedResult = await new EnsureCombatSkillCatalogue(
                Source(Available(CurrentIdentity, definitions)),
                repository)
            .ExecuteAsync(CancellationToken);
        Assert.Equal(
            EnsureCombatSkillCatalogueStatus.RebuildFailed,
            failedResult.Status);
        Assert.Equal("disk full", failedResult.Reason);

        repository.ReplaceAsync(
                Arg.Any<CombatSkillCatalogueSourceIdentity>(),
                Arg.Any<IReadOnlyList<CombatSkillDefinition>>(),
                Arg.Any<IReadOnlyList<CombatSkillImportDiagnostic>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CatalogueReplaceResult>(
                new IOException("replace exploded")));
        var thrownFailure = await new EnsureCombatSkillCatalogue(
                Source(Available(CurrentIdentity, definitions)),
                repository)
            .ExecuteAsync(CancellationToken);
        Assert.Equal(
            EnsureCombatSkillCatalogueStatus.RebuildFailed,
            thrownFailure.Status);
        Assert.Equal("replace exploded", thrownFailure.Reason);
    }

    [Fact]
    public async Task Rebuild_failure_reports_preserved_catalogue_as_stale()
    {
        var definitions = Definitions();
        var repository = Repository(Ready(OlderIdentity, definitions.Length));
        repository.ReplaceAsync(
                Arg.Any<CombatSkillCatalogueSourceIdentity>(),
                Arg.Any<IReadOnlyList<CombatSkillDefinition>>(),
                Arg.Any<IReadOnlyList<CombatSkillImportDiagnostic>>(),
                Arg.Any<CancellationToken>())
            .Returns(CatalogueReplaceResult.Failure("injected failure"));

        var result = await new EnsureCombatSkillCatalogue(
                Source(Available(CurrentIdentity, definitions)),
                repository)
            .ExecuteAsync(CancellationToken);

        Assert.Equal(
            EnsureCombatSkillCatalogueStatus.RebuildFailed,
            result.Status);
        Assert.Equal(
            CatalogueRecoveryStatus.StaleCataloguePreserved,
            result.RecoveryStatus);
        Assert.Equal(OlderIdentity, result.RetainedSourceIdentity);
        Assert.Equal(definitions.Length, result.RetainedDefinitionCount);
        Assert.Contains("stale", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Every_source_identity_input_invalidates_the_catalogue()
    {
        var definitions = Definitions();
        var storedIdentities = new[]
        {
            Identity("different-version", 1, '0', 'A', 'B'),
            Identity("1.0.0-current", 2, '0', 'A', 'B'),
            Identity("1.0.0-current", 1, '1', 'A', 'B'),
            Identity("1.0.0-current", 1, '0', 'C', 'B'),
            Identity("1.0.0-current", 1, '0', 'A', 'D')
        };

        foreach (var storedIdentity in storedIdentities)
        {
            var status = await new ReadCombatSkillCatalogueStatus(
                    Source(Available(CurrentIdentity, definitions)),
                    Repository(Ready(storedIdentity, definitions.Length)))
                .ExecuteAsync(CancellationToken);

            Assert.Equal(CombatSkillCatalogueStatus.Stale, status.Status);
        }
    }

    [Fact]
    public async Task Concurrent_ensure_requests_perform_one_controlled_rebuild()
    {
        var definitions = Definitions();
        var source = Source(Available(CurrentIdentity, definitions));
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        var current = false;
        var replacements = 0;
        repository.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns(_ => current
                ? Ready(CurrentIdentity, definitions.Length)
                : MissingRepository());
        repository.ReplaceAsync(
                Arg.Any<CombatSkillCatalogueSourceIdentity>(),
                Arg.Any<IReadOnlyList<CombatSkillDefinition>>(),
                Arg.Any<IReadOnlyList<CombatSkillImportDiagnostic>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref replacements);
                current = true;
                return CatalogueReplaceResult.Success();
            });

        var results = await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(_ => new EnsureCombatSkillCatalogue(source, repository)
                    .ExecuteAsync(CancellationToken)));

        Assert.Equal(1, replacements);
        Assert.Single(
            results,
            result => result.Status == EnsureCombatSkillCatalogueStatus.Rebuilt);
        Assert.Equal(
            7,
            results.Count(result =>
                result.Status == EnsureCombatSkillCatalogueStatus.Current));
    }

    [Fact]
    public async Task Status_explicitly_reports_an_active_rebuild()
    {
        var definitions = Definitions();
        var repository = Repository(MissingRepository());
        var entered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        repository.ReplaceAsync(
                Arg.Any<CombatSkillCatalogueSourceIdentity>(),
                Arg.Any<IReadOnlyList<CombatSkillDefinition>>(),
                Arg.Any<IReadOnlyList<CombatSkillImportDiagnostic>>(),
                Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                entered.SetResult();
                await release.Task;
                return CatalogueReplaceResult.Success();
            });
        var ensure = new EnsureCombatSkillCatalogue(
                Source(Available(CurrentIdentity, definitions)),
                repository)
            .ExecuteAsync(CancellationToken);
        await entered.Task;

        var statusSource = Substitute.For<ICombatSkillDefinitionSource>();
        var statusRepository = Substitute.For<ICombatSkillCatalogueRepository>();
        CombatSkillCatalogueStatusResult status;
        EnsureCombatSkillCatalogueResult ensureResult;
        try
        {
            status = await new ReadCombatSkillCatalogueStatus(
                    statusSource,
                    statusRepository)
                .ExecuteAsync(CancellationToken);
        }
        finally
        {
            release.TrySetResult();
            ensureResult = await ensure;
        }

        Assert.Equal(CombatSkillCatalogueStatus.Rebuilding, status.Status);
        await statusSource.DidNotReceive().ReadAsync(
            Arg.Any<CancellationToken>());
        await statusRepository.DidNotReceive().ReadStateAsync(
            Arg.Any<CancellationToken>());
        Assert.Equal(
            EnsureCombatSkillCatalogueStatus.Rebuilt,
            ensureResult.Status);
    }

    [Fact]
    public async Task Search_matches_both_languages_and_resolves_requested_language()
    {
        var definitions = Definitions();
        var repository = CurrentRepository(definitions);
        var filter = new CombatSkillCatalogueFilter(
            category: CombatSkillDiscipline.Finger,
            candidateLimit: 10);
        var result = await new SearchCombatSkillDefinitions(
                Source(Available(CurrentIdentity, definitions)),
                repository)
            .ExecuteAsync(
                new CombatSkillSearchRequest(
                    CatalogueLanguage.TraditionalChinese,
                    query: "Corruptive",
                    filter),
                CancellationToken);

        var item = Assert.Single(result.Items);
        Assert.Equal(1, item.Definition.SkillId);
        Assert.Equal("黑血蠱降", item.DisplayName.Value.Value.Text);
        Assert.False(item.DisplayName.UsedFallback);
        await repository.Received(1).QueryAsync(filter, CancellationToken);
    }

    [Fact]
    public async Task Search_applies_fallback_exact_ranking_and_deterministic_paging()
    {
        var definitions = new[]
        {
            Definition(3, (CatalogueLanguage.English, "Alpha")),
            Definition(2, (CatalogueLanguage.TraditionalChinese, "Alpha Blade")),
            Definition(1, (CatalogueLanguage.TraditionalChinese, "Alpha"))
        };
        var repository = CurrentRepository(definitions);
        var result = await new SearchCombatSkillDefinitions(
                Source(Available(CurrentIdentity, definitions)),
                repository)
            .ExecuteAsync(
                new CombatSkillSearchRequest(
                    CatalogueLanguage.English,
                    "alpha",
                    new CombatSkillCatalogueFilter(candidateLimit: 3),
                    offset: 1,
                    limit: 2),
                CancellationToken);

        Assert.Equal(3, result.TotalMatches);
        Assert.True(result.CandidateSetMayBeTruncated);
        Assert.Collection(
            result.Items,
            first => Assert.Equal(3, first.Definition.SkillId),
            second =>
            {
                Assert.Equal(2, second.Definition.SkillId);
                Assert.True(second.DisplayName.UsedFallback);
            });
        Assert.True(result.Issues.HasFlag(
            CombatSkillQueryIssue.PartialLocalization));
    }

    [Fact]
    public async Task Search_normalizes_compatibility_width_case_and_whitespace()
    {
        var definitions = Definitions();
        var result = await new SearchCombatSkillDefinitions(
                Source(Available(CurrentIdentity, definitions)),
                CurrentRepository(definitions))
            .ExecuteAsync(
                new CombatSkillSearchRequest(
                    CatalogueLanguage.TraditionalChinese,
                    "  Ｃｏｒｒｕｐｔｉｖｅ　Ｇｕ  "),
                CancellationToken);

        var match = Assert.Single(result.Items);
        Assert.Equal(1, match.Definition.SkillId);
        Assert.Equal("combat-skill:1", match.StableKey);
    }

    [Fact]
    public async Task Search_supports_stable_skill_id_and_grade_sorting()
    {
        var definitions = new[]
        {
            DefinitionWithFields(
                30,
                CombatSkillDiscipline.Finger,
                grade: 1,
                faction: 1,
                CombatSkillElement.Wood,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Zulu")),
            DefinitionWithFields(
                10,
                CombatSkillDiscipline.Finger,
                grade: 5,
                faction: 1,
                CombatSkillElement.Wood,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Alpha")),
            DefinitionWithFields(
                20,
                CombatSkillDiscipline.Finger,
                grade: 1,
                faction: 1,
                CombatSkillElement.Wood,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Middle"))
        };
        var useCase = new SearchCombatSkillDefinitions(
            Source(Available(CurrentIdentity, definitions)),
            CurrentRepository(definitions));

        var byId = await useCase.ExecuteAsync(
            new CombatSkillSearchRequest(
                CatalogueLanguage.English,
                sort: CombatSkillSearchSort.SkillId),
            CancellationToken);
        Assert.Equal(
            [10, 20, 30],
            byId.Items.Select(item => item.Definition.SkillId));

        var byGrade = await useCase.ExecuteAsync(
            new CombatSkillSearchRequest(
                CatalogueLanguage.English,
                sort: CombatSkillSearchSort.Grade),
            CancellationToken);
        Assert.Equal(
            [20, 30, 10],
            byGrade.Items.Select(item => item.Definition.SkillId));
    }

    [Fact]
    public async Task Search_does_not_query_a_noncurrent_catalogue()
    {
        var definitions = Definitions();
        var repository = Repository(MissingRepository());
        var result = await new SearchCombatSkillDefinitions(
                Source(Available(CurrentIdentity, definitions)),
                repository)
            .ExecuteAsync(
                new CombatSkillSearchRequest(CatalogueLanguage.English),
                CancellationToken);

        Assert.Equal(CombatSkillCatalogueStatus.Missing, result.Catalogue.Status);
        Assert.Empty(result.Items);
        await repository.DidNotReceive().QueryAsync(
            Arg.Any<CombatSkillCatalogueFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_reports_repository_query_failure()
    {
        var definitions = Definitions();
        var repository = CurrentRepository(definitions);
        repository.QueryAsync(
                Arg.Any<CombatSkillCatalogueFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<CombatSkillDefinition>>(
                new IOException("query exploded")));

        var result = await new SearchCombatSkillDefinitions(
                Source(Available(CurrentIdentity, definitions)),
                repository)
            .ExecuteAsync(
                new CombatSkillSearchRequest(CatalogueLanguage.English),
                CancellationToken);

        Assert.Equal(
            CombatSkillCatalogueStatus.RepositoryFailed,
            result.Catalogue.Status);
        Assert.Equal("query exploded", result.Catalogue.Reason);
    }

    [Fact]
    public async Task Details_returns_fallback_or_explicit_not_found()
    {
        var definitions = Definitions();
        var repository = CurrentRepository(definitions);
        var useCase = new ReadCombatSkillDetails(
            Source(Available(CurrentIdentity, definitions)),
            repository);

        var found = await useCase.ExecuteAsync(
            new CombatSkillDetailsRequest(2, CatalogueLanguage.English),
            CancellationToken);
        Assert.True(found.Found);
        Assert.Equal("鐵鼎金身功", found.DisplayName!.Value.Value.Text);
        Assert.True(found.DisplayName.UsedFallback);

        var absent = await useCase.ExecuteAsync(
            new CombatSkillDetailsRequest(999, CatalogueLanguage.English),
            CancellationToken);
        Assert.False(absent.Found);
        Assert.Null(absent.DisplayName);
    }

    [Theory]
    [InlineData(CharacterProgressReadStatus.SaveMissing)]
    [InlineData(CharacterProgressReadStatus.SaveReadFailed)]
    [InlineData(CharacterProgressReadStatus.UnsupportedVersion)]
    public async Task Atlas_preserves_progress_reader_failures(
        CharacterProgressReadStatus status)
    {
        var definitions = Definitions();
        var progressReader = Substitute.For<ICharacterCombatSkillProgressReader>();
        progressReader.ReadAsync(
                Arg.Any<CharacterCombatSkillProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(FailedProgress(status));
        var result = await new ReadCharacterCombatSkillAtlas(
                Source(Available(CurrentIdentity, definitions)),
                CurrentRepository(definitions),
                progressReader)
            .ExecuteAsync(
                new CharacterCombatSkillAtlasRequest(
                    42,
                    CatalogueLanguage.English),
                CancellationToken);

        Assert.Equal(status, result.ProgressStatus);
        Assert.Equal("save diagnostic", result.ProgressFailureReason);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task Atlas_joins_progress_and_preserves_missing_definition()
    {
        var definitions = Definitions();
        var progress = new[]
        {
            Progress(42, 2),
            Progress(42, 999)
        };
        var progressReader = Substitute.For<ICharacterCombatSkillProgressReader>();
        progressReader.ReadAsync(
                Arg.Any<CharacterCombatSkillProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(CharacterCombatSkillProgressReadResult.Available(
                new CharacterCombatSkillProgressMetadata(
                    progress[0].SaveSnapshot,
                    "1.0.0-test"),
                progress));
        var result = await new ReadCharacterCombatSkillAtlas(
                Source(Available(CurrentIdentity, definitions)),
                CurrentRepository(definitions),
                progressReader)
            .ExecuteAsync(
                new CharacterCombatSkillAtlasRequest(
                    42,
                    CatalogueLanguage.English),
                CancellationToken);

        Assert.Equal(CharacterProgressReadStatus.Available, result.ProgressStatus);
        Assert.Equal(4, result.TotalMatches);
        Assert.NotNull(result.ProgressMetadata);
        var known = Assert.Single(
            result.Entries,
            entry => entry.SkillId == 2);
        Assert.NotNull(known.Progress);
        Assert.Same(definitions[1], known.Definition);
        Assert.True(known.DisplayName.UsedFallback);
        Assert.Equal(3, known.BaseGridCost.Value.Value);
        Assert.Equal(3, known.CurrentEffectiveGridCost.Value);

        var unlearned = Assert.Single(
            result.Entries,
            entry => entry.SkillId == 1);
        Assert.Null(unlearned.Progress);
        Assert.False(unlearned.Learned.Value);
        Assert.False(unlearned.CurrentEffectiveGridCost.IsAvailable);

        var unknown = Assert.Single(
            result.Entries,
            entry => entry.SkillId == 999);
        Assert.Null(unknown.Definition);
        Assert.False(unknown.DisplayName.Value.IsAvailable);
        Assert.Contains(
            unknown.Diagnostics,
            diagnostic => diagnostic.Code == "STATIC_DEFINITION_MISSING");
        Assert.True(result.Issues.HasFlag(
            CombatSkillQueryIssue.MissingDefinition));
        Assert.True(result.Issues.HasFlag(
            CombatSkillQueryIssue.UnsupportedStudyMapping));
        await progressReader.Received(1).ReadAsync(
            Arg.Is<CharacterCombatSkillProgressReadRequest>(request =>
                request != null
                && request.CharacterId == 42
                && request.PreferredLanguage == CatalogueLanguage.English),
            CancellationToken);
    }

    [Fact]
    public async Task Atlas_applies_all_static_filters_and_bilingual_query()
    {
        var definitions = new[]
        {
            DefinitionWithFields(
                10,
                CombatSkillDiscipline.Finger,
                grade: 5,
                faction: 1,
                CombatSkillElement.Wood,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Finger Art")),
            DefinitionWithFields(
                11,
                CombatSkillDiscipline.Sword,
                grade: 3,
                faction: 2,
                CombatSkillElement.Metal,
                CombatSkillEquipmentType.Defense,
                (CatalogueLanguage.TraditionalChinese, "劍法"),
                (CatalogueLanguage.English, "Silver Blade"))
        };
        var result = await new ReadCharacterCombatSkillAtlas(
                Source(Available(CurrentIdentity, definitions)),
                CurrentRepository(definitions),
                ProgressReader([]))
            .ExecuteAsync(
                new CharacterCombatSkillAtlasRequest(
                    42,
                    CatalogueLanguage.TraditionalChinese,
                    query: "silver",
                    definitionFilter: new CombatSkillCatalogueFilter(
                        category: CombatSkillDiscipline.Sword,
                        grade: new CombatSkillGrade(3),
                        faction: new CombatSkillFactionId(2),
                        element: CombatSkillElement.Metal,
                        equipmentType: CombatSkillEquipmentType.Defense)),
                CancellationToken);

        var entry = Assert.Single(result.Entries);
        Assert.Equal(11, entry.SkillId);
        Assert.False(entry.Learned.Value);
        Assert.Equal("劍法", entry.DisplayName.Value.Value.Text);
    }

    [Fact]
    public async Task Atlas_filters_every_independent_progress_fact()
    {
        var definitions = Definitions();
        var completed = new BreakthroughDirectionAvailability(
            isBrokenOut: true,
            canBreakthroughNow: false,
            availableDirections: []);
        var ready = new BreakthroughDirectionAvailability(
            isBrokenOut: false,
            canBreakthroughNow: true,
            [PracticeDirection.Reverse]);
        var progress = new[]
        {
            Progress(
                42,
                1,
                studyComplete: true,
                breakthrough: completed,
                activeDirection: PracticeDirection.Direct,
                attainmentMastered: true,
                simplified: true,
                activated: true,
                equipped: true),
            Progress(
                42,
                2,
                studyComplete: false,
                breakthrough: ready)
        };
        var useCase = new ReadCharacterCombatSkillAtlas(
            Source(Available(CurrentIdentity, definitions)),
            CurrentRepository(definitions),
            ProgressReader(
                progress,
                [new("TEST_WARNING", "A partial progress warning.")]));

        var completedResult = await useCase.ExecuteAsync(
            new CharacterCombatSkillAtlasRequest(
                42,
                CatalogueLanguage.English,
                progressFilter: new CharacterCombatSkillProgressFilter(
                    learned: true,
                    hasProficiency: true,
                    studyComplete: true,
                    brokenThrough: true,
                    activeDirection: PracticeDirection.Direct,
                    attainmentMastered: true,
                    simplified: true,
                    activated: true,
                    equipped: true)),
            CancellationToken);
        var completedEntry = Assert.Single(completedResult.Entries);
        Assert.Equal(1, completedEntry.SkillId);
        Assert.Equal(3, completedEntry.BaseGridCost.Value.Value);
        Assert.Equal(2, completedEntry.CurrentEffectiveGridCost.Value);
        Assert.True(completedResult.Issues.HasFlag(
            CombatSkillQueryIssue.ProgressWarnings));

        var readyResult = await useCase.ExecuteAsync(
            new CharacterCombatSkillAtlasRequest(
                42,
                CatalogueLanguage.English,
                progressFilter: new CharacterCombatSkillProgressFilter(
                    breakthroughReady: true,
                    brokenThrough: false)),
            CancellationToken);
        Assert.Equal(2, Assert.Single(readyResult.Entries).SkillId);

        var unlearnedResult = await useCase.ExecuteAsync(
            new CharacterCombatSkillAtlasRequest(
                42,
                CatalogueLanguage.English,
                progressFilter: new CharacterCombatSkillProgressFilter(
                    learned: false)),
            CancellationToken);
        var unlearned = Assert.Single(unlearnedResult.Entries);
        Assert.Equal(3, unlearned.SkillId);
        Assert.Null(unlearned.Progress);
    }

    [Fact]
    public async Task Atlas_filters_do_not_treat_unavailable_boolean_facts_as_false()
    {
        var definitions = Definitions();
        var progress = new[]
        {
            Progress(
                42,
                1,
                proficiencyAvailable: false,
                attainmentMastered: false),
            Progress(42, 2)
        };
        var useCase = new ReadCharacterCombatSkillAtlas(
            Source(Available(CurrentIdentity, definitions)),
            CurrentRepository(definitions),
            ProgressReader(progress));

        var explicitFalse = await useCase.ExecuteAsync(
            new CharacterCombatSkillAtlasRequest(
                42,
                CatalogueLanguage.English,
                progressFilter: new CharacterCombatSkillProgressFilter(
                    attainmentMastered: false)),
            CancellationToken);
        Assert.Equal(1, Assert.Single(explicitFalse.Entries).SkillId);

        var unavailableProficiency = await useCase.ExecuteAsync(
            new CharacterCombatSkillAtlasRequest(
                42,
                CatalogueLanguage.English,
                progressFilter: new CharacterCombatSkillProgressFilter(
                    hasProficiency: false)),
            CancellationToken);
        Assert.Equal(1, Assert.Single(unavailableProficiency.Entries).SkillId);
    }

    [Fact]
    public async Task Atlas_paging_and_virtualization_keys_are_repeatable()
    {
        var definitions = Definitions();
        var useCase = new ReadCharacterCombatSkillAtlas(
            Source(Available(CurrentIdentity, definitions)),
            CurrentRepository(definitions),
            ProgressReader([Progress(42, 2)]));
        var request = new CharacterCombatSkillAtlasRequest(
            42,
            CatalogueLanguage.English,
            offset: 1,
            limit: 2);

        var first = await useCase.ExecuteAsync(request, CancellationToken);
        var second = await useCase.ExecuteAsync(request, CancellationToken);

        Assert.Equal(3, first.TotalMatches);
        Assert.Equal(
            first.Entries.Select(entry => entry.StableKey),
            second.Entries.Select(entry => entry.StableKey));
        Assert.Equal(2, first.Entries.Length);
    }

    [Fact]
    public async Task Atlas_category_and_grade_sort_is_applied_before_paging()
    {
        var definitions = new[]
        {
            DefinitionWithFields(
                10,
                CombatSkillDiscipline.Neigong,
                grade: 2,
                faction: 1,
                CombatSkillElement.Wood,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Alpha Low Grade")),
            DefinitionWithFields(
                11,
                CombatSkillDiscipline.Finger,
                grade: 8,
                faction: 1,
                CombatSkillElement.Wood,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Zulu High Grade")),
            DefinitionWithFields(
                12,
                CombatSkillDiscipline.Neigong,
                grade: 5,
                faction: 1,
                CombatSkillElement.Wood,
                CombatSkillEquipmentType.Attack,
                (CatalogueLanguage.English, "Bravo Middle Grade"))
        };
        var useCase = new ReadCharacterCombatSkillAtlas(
            Source(Available(CurrentIdentity, definitions)),
            CurrentRepository(definitions),
            ProgressReader([]));

        var result = await useCase.ExecuteAsync(
            new CharacterCombatSkillAtlasRequest(
                42,
                CatalogueLanguage.English,
                offset: 0,
                limit: 2,
                sort: CharacterCombatSkillAtlasSort.CategoryThenGrade),
            CancellationToken);

        Assert.Equal(3, result.TotalMatches);
        Assert.Equal([10, 12], result.Entries.Select(entry => entry.SkillId));
    }

    [Fact]
    public async Task Details_can_join_definition_and_character_progress()
    {
        var definitions = Definitions();
        var progress = Progress(42, 2, simplified: true);
        var reader = ProgressReader([progress]);
        var useCase = new ReadCombatSkillDetails(
            Source(Available(CurrentIdentity, definitions)),
            CurrentRepository(definitions),
            reader);

        var result = await useCase.ExecuteAsync(
            new CombatSkillDetailsRequest(
                2,
                CatalogueLanguage.English,
                characterId: null),
            CancellationToken);

        Assert.True(result.Found);
        Assert.Equal(CharacterProgressReadStatus.Available, result.ProgressStatus);
        Assert.NotNull(result.ProgressMetadata);
        Assert.Same(progress, result.CharacterState!.Progress);
        Assert.Equal(3, result.CharacterState.BaseGridCost.Value.Value);
        Assert.Equal(2, result.CharacterState.CurrentEffectiveGridCost.Value);
        Assert.True(result.Issues.HasFlag(
            CombatSkillQueryIssue.PartialLocalization));
        await reader.Received(1).ReadAsync(
            Arg.Is<CharacterCombatSkillProgressReadRequest>(request =>
                request != null && request.CharacterId == null),
            CancellationToken);
    }

    [Fact]
    public async Task Details_preserves_progress_without_a_static_definition()
    {
        var definitions = Definitions();
        var progress = Progress(42, 999);
        var result = await new ReadCombatSkillDetails(
                Source(Available(CurrentIdentity, definitions)),
                CurrentRepository(definitions),
                ProgressReader([progress]))
            .ExecuteAsync(
                new CombatSkillDetailsRequest(
                    999,
                    CatalogueLanguage.English,
                    characterId: 42),
                CancellationToken);

        Assert.False(result.Found);
        Assert.Same(progress, result.CharacterState!.Progress);
        Assert.True(result.Issues.HasFlag(
            CombatSkillQueryIssue.MissingDefinition));
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "STATIC_DEFINITION_MISSING");
    }

    [Theory]
    [InlineData(CharacterProgressReadStatus.SaveMissing)]
    [InlineData(CharacterProgressReadStatus.SaveReadFailed)]
    [InlineData(CharacterProgressReadStatus.UnsupportedVersion)]
    public async Task Details_preserves_explicit_progress_failure_state(
        CharacterProgressReadStatus status)
    {
        var definitions = Definitions();
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();
        reader.ReadAsync(
                Arg.Any<CharacterCombatSkillProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(FailedProgress(status));
        var result = await new ReadCombatSkillDetails(
                Source(Available(CurrentIdentity, definitions)),
                CurrentRepository(definitions),
                reader)
            .ExecuteAsync(
                new CombatSkillDetailsRequest(
                    1,
                    CatalogueLanguage.English,
                    characterId: 42),
                CancellationToken);

        Assert.Equal(status, result.ProgressStatus);
        Assert.Equal("save diagnostic", result.ProgressFailureReason);
        Assert.Null(result.CharacterState);
    }

    [Fact]
    public async Task Atlas_does_not_read_save_when_catalogue_is_not_current()
    {
        var definitions = Definitions();
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();
        var result = await new ReadCharacterCombatSkillAtlas(
                Source(Available(CurrentIdentity, definitions)),
                Repository(MissingRepository()),
                reader)
            .ExecuteAsync(
                new CharacterCombatSkillAtlasRequest(
                    42,
                    CatalogueLanguage.English),
                CancellationToken);

        Assert.Equal(CharacterProgressReadStatus.NotRead, result.ProgressStatus);
        await reader.DidNotReceive().ReadAsync(
            Arg.Any<CharacterCombatSkillProgressReadRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancellation_is_propagated_without_invoking_ports()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new ReadCombatSkillCatalogueStatus(source, repository)
                .ExecuteAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new EnsureCombatSkillCatalogue(source, repository)
                .ExecuteAsync(cancellation.Token));
        await source.DidNotReceive().ReadAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Contracts_are_bounded_immutable_and_path_free()
    {
        Assert.Null(new CharacterCombatSkillProgressReadRequest().CharacterId);
        Assert.Null(
            new CharacterCombatSkillAtlasRequest(
                characterId: null,
                CatalogueLanguage.English).CharacterId);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatSkillCatalogueFilter(candidateLimit: 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatSkillSearchRequest(
                CatalogueLanguage.English,
                limit: CombatSkillSearchRequest.MaximumPageSize + 1));
        Assert.Throws<ArgumentException>(
            () => new CombatSkillSearchRequest(
                CatalogueLanguage.English,
                new string('x', CombatSkillSearchRequest.MaximumQueryLength + 1)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CharacterCombatSkillAtlasRequest(
                42,
                CatalogueLanguage.English,
                limit: CombatSkillSearchRequest.MaximumPageSize + 1));
        Assert.Throws<ArgumentException>(
            () => new CharacterCombatSkillAtlasRequest(
                42,
                CatalogueLanguage.English,
                new string(
                    'x',
                    CombatSkillSearchRequest.MaximumQueryLength + 1)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CharacterCombatSkillAtlasRequest(
                42,
                CatalogueLanguage.English,
                sort: (CharacterCombatSkillAtlasSort)int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CharacterCombatSkillProgressFilter(
                activeDirection: PracticeDirection.Neutral));

        var portParameters = typeof(ICombatSkillCatalogueRepository)
            .GetMethods()
            .SelectMany(method => method.GetParameters())
            .ToArray();
        Assert.DoesNotContain(
            portParameters,
            parameter => parameter.Name?.Contains(
                "path",
                StringComparison.OrdinalIgnoreCase) == true);
        Assert.All(
            typeof(CharacterCombatSkillProgressReadResult).GetProperties(),
            property => Assert.False(property.CanWrite));
    }

    [Fact]
    public void Progress_result_preserves_metadata_and_normalizes_skill_order()
    {
        var first = Progress(7, 1);
        var second = Progress(7, 2);
        List<CharacterCombatSkillProgressWarning> warnings =
        [
            new("TEST_WARNING", "A sanitized warning.")
        ];
        var metadata = new CharacterCombatSkillProgressMetadata(
            first.SaveSnapshot,
            "1.0.0-test",
            warnings);

        var result = CharacterCombatSkillProgressReadResult.Available(
            metadata,
            [second, first]);
        warnings.Clear();

        var actualMetadata = Assert.IsType<CharacterCombatSkillProgressMetadata>(
            result.Metadata);
        Assert.Same(metadata, actualMetadata);
        Assert.Equal([1, 2], result.Progress.Select(value => value.SkillId));
        Assert.Single(actualMetadata.Warnings);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Progress_result_rejects_mixed_snapshot_or_character_data()
    {
        var first = Progress(7, 1);
        var metadata = new CharacterCombatSkillProgressMetadata(
            new SaveSnapshotIdentity(
                new string('F', 64),
                first.SaveSnapshot.ReadAtUtc),
            "1.0.0-test");

        Assert.Throws<ArgumentException>(
            () => CharacterCombatSkillProgressReadResult.Available(
                metadata,
                [first]));
        Assert.Throws<ArgumentException>(
            () => CharacterCombatSkillProgressReadResult.Available(
                new CharacterCombatSkillProgressMetadata(
                    first.SaveSnapshot,
                    "1.0.0-test"),
                [first, Progress(8, 2)]));
    }

    [Fact]
    public void Definition_source_identity_and_diagnostics_are_complete_and_ordered()
    {
        Assert.Equal(
            new string('0', 64),
            CurrentIdentity.GameDataFingerprint);
        Assert.Equal(1, CurrentIdentity.ImporterVersion);
        Assert.Throws<ArgumentException>(
            () => new CombatSkillCatalogueSourceIdentity(
                "version",
                1,
                "not-a-hash",
                new string('A', 64),
                new string('B', 64)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatSkillCatalogueSourceIdentity(
                "version",
                0,
                new string('0', 64),
                new string('A', 64),
                new string('B', 64)));

        var result = CombatSkillDefinitionSourceResult.Available(
            CurrentIdentity,
            Definitions(),
            [
                new CombatSkillImportDiagnostic(
                    CombatSkillImportDiagnosticSeverity.Warning,
                    "SECOND",
                    "combat-skill:2",
                    "second"),
                new CombatSkillImportDiagnostic(
                    CombatSkillImportDiagnosticSeverity.Error,
                    "FIRST",
                    "combat-skill:1",
                    "first")
            ]);

        Assert.Collection(
            result.Diagnostics,
            first => Assert.Equal("combat-skill:1", first.SourceRecordIdentity),
            second => Assert.Equal("combat-skill:2", second.SourceRecordIdentity));
    }

    private static CancellationToken CancellationToken =>
        TestContext.Current.CancellationToken;

    private static CombatSkillCatalogueSourceIdentity CurrentIdentity { get; } =
        new(
            "1.0.0-current",
            1,
            new string('0', 64),
            new string('A', 64),
            new string('B', 64));

    private static CombatSkillCatalogueSourceIdentity OlderIdentity { get; } =
        new(
            "1.0.0-older",
            1,
            new string('1', 64),
            new string('C', 64),
            new string('D', 64));

    private static CombatSkillCatalogueSourceIdentity Identity(
        string gameDataVersion,
        int importerVersion,
        char gameDataFingerprint,
        char traditionalChineseFingerprint,
        char englishFingerprint) => new(
            gameDataVersion,
            importerVersion,
            new string(gameDataFingerprint, 64),
            new string(traditionalChineseFingerprint, 64),
            new string(englishFingerprint, 64));

    private static CombatSkillDefinition[] Definitions() =>
    [
        Definition(
            1,
            (CatalogueLanguage.TraditionalChinese, "黑血蠱降"),
            (CatalogueLanguage.English, "Corruptive Gu Infection")),
        Definition(2, (CatalogueLanguage.TraditionalChinese, "鐵鼎金身功")),
        Definition(3, (CatalogueLanguage.English, "Lion Roar"))
    ];

    private static CombatSkillDefinition Definition(
        int skillId,
        params (CatalogueLanguage Language, string Text)[] names) =>
        DefinitionWithFields(
            skillId,
            CombatSkillDiscipline.Finger,
            grade: 5,
            faction: 1,
            CombatSkillElement.Wood,
            CombatSkillEquipmentType.Attack,
            names);

    private static CombatSkillDefinition DefinitionWithFields(
        int skillId,
        CombatSkillDiscipline category,
        int grade,
        int faction,
        CombatSkillElement element,
        CombatSkillEquipmentType equipmentType,
        params (CatalogueLanguage Language, string Text)[] names)
    {
        var source = new CatalogueSourceReference(
            CatalogueSourceKind.GameData,
            "gamedata:test",
            $"combat-skill:{skillId}");
        return new CombatSkillDefinition(
            skillId,
            new CombatSkillLocalizedNames(names.Select(name =>
                new LocalizedCombatSkillName(
                    name.Language,
                    name.Text,
                    new CatalogueSourceReference(
                        name.Language == CatalogueLanguage.English
                            ? CatalogueSourceKind.EnglishLanguageResource
                            : CatalogueSourceKind.TraditionalChineseLanguageResource,
                        name.Language == CatalogueLanguage.English
                            ? "language-en:test"
                            : "language-cnh:test",
                        $"combat-skill-name:{skillId}")))),
            CatalogueField<CombatSkillDiscipline>.Available(
                category,
                source),
            CatalogueField<CombatSkillGrade>.Available(
                new CombatSkillGrade(grade),
                source),
            CatalogueField<CombatSkillFactionId>.Available(
                new CombatSkillFactionId(faction),
                source),
            CatalogueField<CombatSkillElement>.Available(
                element,
                source),
            CatalogueField<CombatSkillEquipmentType>.Available(
                equipmentType,
                source),
            CatalogueField<CombatSkillGridCost>.Available(
                new CombatSkillGridCost(3),
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
                CatalogueField<CombatSkillEffectId>.Unavailable("test"),
                CatalogueField<CombatSkillEffectId>.Unavailable("test"),
                CatalogueField<CombatSkillEffectId>.Unavailable("test")),
            rawDescriptions: null,
            source);
    }

    private static CharacterCombatSkillProgress Progress(
        int characterId,
        int skillId,
        bool learned = true,
        bool proficiencyAvailable = true,
        bool? studyComplete = null,
        BreakthroughDirectionAvailability? breakthrough = null,
        PracticeDirection? activeDirection = null,
        bool? attainmentMastered = null,
        bool simplified = false,
        bool activated = false,
        bool equipped = false)
    {
        var progressSource = new SkillProgressSource(
            SkillProgressSourceKind.SaveSnapshot,
            $"save:{new string('E', 64)}",
            "test");
        var actualBreakthrough = breakthrough is null
            ? SkillProgressField<BreakthroughDirectionAvailability>.Unavailable(
                "test")
            : SkillProgressField<BreakthroughDirectionAvailability>.Available(
                breakthrough,
                progressSource);
        var details = studyComplete is null
            ? null
            : new[]
            {
                new CombatSkillStudyDetailProgress(
                    "outline-0",
                    0,
                    CombatSkillStudyDetailGroup.Outline,
                    CatalogueField<string>.Available(
                        "Outline",
                        new CatalogueSourceReference(
                            CatalogueSourceKind.EnglishLanguageResource,
                            "language-en:test",
                            "LK_CombatSkill_First_Page_Type_0")),
                    SkillProgressField<CombatSkillStudyState>.Available(
                        studyComplete.Value
                            ? CombatSkillStudyState.Read
                            : CombatSkillStudyState.NotRead,
                        progressSource),
                    SkillProgressField<bool>.Available(
                        activated,
                        progressSource))
            };
        return new CharacterCombatSkillProgress(
            characterId,
            new SaveSnapshotIdentity(
                new string('E', 64),
                DateTimeOffset.Parse("2026-08-02T12:00:00Z")),
            skillId,
            SkillProgressField<bool>.Available(learned, progressSource),
            new CombatSkillProficiencyProgress(
                proficiencyAvailable
                    ? SkillProgressField<int>.Available(50, progressSource)
                    : SkillProgressField<int>.Unavailable("test"),
                SkillProgressField<int>.Available(100, progressSource),
                SkillProgressField<decimal>.Available(50m, progressSource)),
            details,
            actualBreakthrough,
            activeDirection is null
                ? SkillProgressField<PracticeDirection>.Unavailable("test")
                : SkillProgressField<PracticeDirection>.Available(
                    activeDirection.Value,
                    progressSource),
            attainmentMastered is null
                ? SkillProgressField<bool>.Unavailable("test")
                : SkillProgressField<bool>.Available(
                    attainmentMastered.Value,
                    progressSource),
            SkillProgressField<bool>.Available(simplified, progressSource),
            SkillProgressField<bool>.Available(activated, progressSource),
            SkillProgressField<bool>.Available(equipped, progressSource));
    }

    private static CombatSkillDefinitionSourceResult Available(
        CombatSkillCatalogueSourceIdentity identity,
        IReadOnlyList<CombatSkillDefinition> definitions) =>
        CombatSkillDefinitionSourceResult.Available(identity, definitions);

    private static CombatSkillDefinitionSourceResult FailedSource(
        DefinitionSourceReadStatus status) => status switch
        {
            DefinitionSourceReadStatus.MissingSources =>
                CombatSkillDefinitionSourceResult.MissingSources(
                    "source diagnostic"),
            DefinitionSourceReadStatus.UnsupportedVersion =>
                CombatSkillDefinitionSourceResult.UnsupportedVersion(
                    "source diagnostic"),
            DefinitionSourceReadStatus.Failed =>
                CombatSkillDefinitionSourceResult.Failed("source diagnostic"),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

    private static CharacterCombatSkillProgressReadResult FailedProgress(
        CharacterProgressReadStatus status) => status switch
        {
            CharacterProgressReadStatus.SaveMissing =>
                CharacterCombatSkillProgressReadResult.SaveMissing(
                    "save diagnostic"),
            CharacterProgressReadStatus.SaveReadFailed =>
                CharacterCombatSkillProgressReadResult.SaveReadFailed(
                    "save diagnostic"),
            CharacterProgressReadStatus.UnsupportedVersion =>
                CharacterCombatSkillProgressReadResult.UnsupportedVersion(
                    "save diagnostic"),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

    private static ICombatSkillDefinitionSource Source(
        CombatSkillDefinitionSourceResult result)
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>()).Returns(result);
        return source;
    }

    private static ICharacterCombatSkillProgressReader ProgressReader(
        IReadOnlyList<CharacterCombatSkillProgress> progress,
        IReadOnlyList<CharacterCombatSkillProgressWarning>? warnings = null)
    {
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();
        var snapshot = progress.FirstOrDefault()?.SaveSnapshot
            ?? new SaveSnapshotIdentity(
                new string('E', 64),
                DateTimeOffset.Parse("2026-08-02T12:00:00Z"));
        reader.ReadAsync(
                Arg.Any<CharacterCombatSkillProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(CharacterCombatSkillProgressReadResult.Available(
                new CharacterCombatSkillProgressMetadata(
                    snapshot,
                    "1.0.0-test",
                    warnings),
                progress));
        return reader;
    }

    private static ICombatSkillCatalogueRepository Repository(
        CombatSkillCatalogueRepositorySnapshot snapshot)
    {
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        return repository;
    }

    private static ICombatSkillCatalogueRepository CurrentRepository(
        IReadOnlyList<CombatSkillDefinition> definitions)
    {
        var repository = Repository(Ready(CurrentIdentity, definitions.Count));
        repository.QueryAsync(
                Arg.Any<CombatSkillCatalogueFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(definitions);
        repository.GetAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => definitions.FirstOrDefault(
                definition => definition.SkillId == call.ArgAt<int>(0)));
        return repository;
    }

    private static CombatSkillCatalogueRepositorySnapshot Ready(
        CombatSkillCatalogueSourceIdentity identity,
        int count) => new(
            CatalogueRepositoryState.Ready,
            identity,
            count,
            DateTimeOffset.Parse("2026-08-02T12:30:00Z"));

    private static CombatSkillCatalogueRepositorySnapshot MissingRepository() =>
        new(
            CatalogueRepositoryState.Missing,
            sourceIdentity: null,
            definitionCount: 0,
            builtAtUtc: null);
}
