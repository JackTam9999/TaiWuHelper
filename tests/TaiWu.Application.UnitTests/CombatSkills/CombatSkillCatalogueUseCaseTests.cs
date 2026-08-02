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
        Assert.Collection(
            result.Entries,
            known =>
            {
                Assert.Equal(2, known.Progress.SkillId);
                Assert.NotNull(known.Definition);
                Assert.True(known.DisplayName.UsedFallback);
            },
            unknown =>
            {
                Assert.Equal(999, unknown.Progress.SkillId);
                Assert.Null(unknown.Definition);
                Assert.False(unknown.DisplayName.Value.IsAvailable);
            });
        await progressReader.Received(1).ReadAsync(
            Arg.Is<CharacterCombatSkillProgressReadRequest>(request =>
                request != null
                && request.CharacterId == 42
                && request.PreferredLanguage == CatalogueLanguage.English),
            CancellationToken);
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
        int skillId)
    {
        var progressSource = new SkillProgressSource(
            SkillProgressSourceKind.SaveSnapshot,
            $"save:{new string('E', 64)}",
            "test");
        return new CharacterCombatSkillProgress(
            characterId,
            new SaveSnapshotIdentity(
                new string('E', 64),
                DateTimeOffset.Parse("2026-08-02T12:00:00Z")),
            skillId,
            SkillProgressField<bool>.Available(true, progressSource),
            new CombatSkillProficiencyProgress(
                SkillProgressField<int>.Available(50, progressSource),
                SkillProgressField<int>.Available(100, progressSource),
                SkillProgressField<decimal>.Available(50m, progressSource)),
            studyDetails: null,
            SkillProgressField<BreakthroughDirectionAvailability>.Unavailable(
                "test"),
            SkillProgressField<PracticeDirection>.Unavailable("test"),
            SkillProgressField<bool>.Unavailable("test"),
            SkillProgressField<bool>.Available(false, progressSource),
            SkillProgressField<bool>.Available(false, progressSource),
            SkillProgressField<bool>.Available(false, progressSource));
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
