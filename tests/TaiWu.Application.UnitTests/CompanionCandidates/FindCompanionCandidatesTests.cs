using NSubstitute;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using Xunit;

namespace TaiWu.Application.UnitTests.CompanionCandidates;

[Collection(CompanionCandidateEnrichmentCollection.Name)]
public sealed class FindCompanionCandidatesTests
{
    [Fact]
    public async Task Complete_workflow_binds_source_ranking_filter_and_comparison()
    {
        var snapshot = Snapshot([
            Profile(2, score: 90),
            Profile(1, score: 70)
        ]);
        var (reader, source, repository) = CurrentWorkflow(snapshot);
        var request = Request(
            filter: CompanionRoleShortlistFilter.Ranked,
            firstComparisonCharacterId: 2,
            secondComparisonCharacterId: 1);

        var result = await new FindCompanionCandidates(reader, source, repository)
            .ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(CompanionFinderStatus.Complete, result.Status);
        Assert.True(result.HasAuthoritativeResult);
        Assert.Same(snapshot, result.Snapshot);
        Assert.Same(snapshot, result.Enrichment!.Snapshot);
        Assert.Same(result.Ranking, result.Shortlist!.Ranking);
        Assert.Same(result.Shortlist, result.View!.Source);
        Assert.Same(result.Shortlist, result.Comparison!.Shortlist);
        Assert.Equal(CompanionRoleComparisonOutcome.FirstAdvantage, result.Comparison.Outcome);
        Assert.Equal(2, result.View.VisibleCount);
        Assert.Equal(Sha, result.SourceIdentity!.CandidateSourceVersions.SaveSha256);
        Assert.Equal(
            VerifiedCompanionRoleDefinitions.SupportedGameDataVersion,
            result.SourceIdentity.CandidateSourceVersions.GameDataVersion);
        Assert.Equal("1", result.SourceIdentity.RoleVersion);
        Assert.Equal("1", result.SourceIdentity.EvaluationRuleVersion);
        Assert.Equal(CombatSkillCatalogueStatus.Current, result.SourceIdentity.CatalogueStatus);
        Assert.NotNull(result.SourceIdentity.CatalogueSource);
        Assert.NotNull(result.Fingerprint);
        Assert.Null(result.FailureIdentity);
        await reader.Received(1).ReadAsync(
            CompanionCandidateSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comprehensive_objective_ranks_complete_breadth_and_retains_missing_evidence()
    {
        var snapshot = Snapshot([
            Profile(1, facts: CapabilityFacts(50)),
            Profile(2, facts: CapabilityFacts(80)),
            Profile(3, facts: CapabilityFacts(90).Skip(1))
        ]);
        var (reader, source, repository) = CurrentWorkflow(snapshot);
        var request = Request(
            roleIdentity: "COMPREHENSIVE_BASE_CAPABILITY",
            domain: CandidateDisciplineDomain.Capability,
            firstComparisonCharacterId: 2,
            secondComparisonCharacterId: 1);

        var result = await new FindCompanionCandidates(reader, source, repository)
            .ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(CompanionFinderStatus.Complete, result.Status);
        Assert.Equal([2, 1], result.Ranking!.RankedCandidates.Select(candidate =>
            candidate.Evaluation.Profile.Identity.CharacterId));
        Assert.Equal([80m, 50m], result.Ranking.RankedCandidates.Select(candidate =>
            candidate.Evaluation.TotalScore));
        Assert.Equal(
            CompanionRoleCandidateRankingState.Incomplete,
            Assert.Single(result.Ranking.UnrankedCandidates).State);
        Assert.Equal(
            CompanionRoleComparisonOutcome.FirstAdvantage,
            result.Comparison!.Outcome);
    }

    [Fact]
    public async Task Empty_snapshot_returns_a_typed_empty_authoritative_result()
    {
        var snapshot = Snapshot([]);
        var (reader, source, repository) = CurrentWorkflow(snapshot);

        var result = await new FindCompanionCandidates(reader, source, repository)
            .ExecuteAsync(Request(), TestContext.Current.CancellationToken);

        Assert.Equal(CompanionFinderStatus.Empty, result.Status);
        Assert.Equal(0, result.Shortlist!.Counts.Total);
        Assert.Empty(result.View!.Entries);
        Assert.NotNull(result.Fingerprint);
    }

    [Fact]
    public async Task Partial_snapshot_retains_the_complete_authoritative_chain()
    {
        var snapshot = Snapshot([Profile(1, score: 70)]);
        var (reader, source, repository) = CurrentWorkflow(
            snapshot,
            CompanionCandidateSnapshotReadStatus.Partial);

        var result = await new FindCompanionCandidates(reader, source, repository)
            .ExecuteAsync(Request(), TestContext.Current.CancellationToken);

        Assert.Equal(CompanionFinderStatus.Partial, result.Status);
        Assert.Equal(CompanionCandidateSnapshotReadStatus.Partial, result.SnapshotReadStatus);
        Assert.NotNull(result.Enrichment);
        Assert.NotNull(result.Shortlist);
        Assert.Single(result.Shortlist.Entries);
    }

    [Fact]
    public async Task Catalogue_staleness_is_partial_without_suppressing_role_results()
    {
        var snapshot = Snapshot([Profile(1, score: 70)]);
        var reader = Reader(CompanionCandidateSnapshotReadResult.Complete(snapshot));
        var installed = CatalogueIdentity(hash: 'A');
        var stored = CatalogueIdentity(hash: 'B');
        var source = DefinitionSource(installed);
        var repository = Repository(
            CatalogueRepositoryState.Ready,
            stored);

        var result = await new FindCompanionCandidates(reader, source, repository)
            .ExecuteAsync(Request(), TestContext.Current.CancellationToken);

        Assert.Equal(CompanionFinderStatus.Partial, result.Status);
        Assert.Equal(CompanionCandidateEnrichmentStatus.CatalogueStale, result.Enrichment!.Status);
        Assert.Equal(CompanionCandidateEnrichmentState.CatalogueStale, result.Enrichment.Candidates[0].State);
        Assert.Single(result.Shortlist!.RankedEntries);
        Assert.Same(snapshot.Profiles[0], result.Shortlist.Entries[0].Evaluation.Profile);
        await repository.DidNotReceive().QueryAsync(
            Arg.Any<CombatSkillCatalogueFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_catalogue_is_partial_and_preserves_candidate_source_evidence()
    {
        var snapshot = Snapshot([Profile(1, score: 70)]);
        var reader = Reader(CompanionCandidateSnapshotReadResult.Complete(snapshot));
        var identity = CatalogueIdentity();
        var source = DefinitionSource(identity);
        var repository = Repository(CatalogueRepositoryState.Missing);

        var result = await new FindCompanionCandidates(reader, source, repository)
            .ExecuteAsync(Request(), TestContext.Current.CancellationToken);

        Assert.Equal(CompanionFinderStatus.Partial, result.Status);
        Assert.Equal(CompanionCandidateEnrichmentStatus.CatalogueMissing, result.Enrichment!.Status);
        Assert.Same(snapshot, result.Snapshot);
        Assert.Same(snapshot.Profiles[0], result.Ranking!.Candidates[0].Evaluation.Profile);
        Assert.NotNull(result.Fingerprint);
    }

    [Fact]
    public async Task Conflicting_candidate_remains_typed_in_a_complete_result()
    {
        var snapshot = Snapshot([Profile(1, facts: [ConflictingScoreFact()])]);
        var (reader, source, repository) = CurrentWorkflow(snapshot);

        var result = await new FindCompanionCandidates(reader, source, repository)
            .ExecuteAsync(Request(), TestContext.Current.CancellationToken);

        Assert.Equal(CompanionFinderStatus.Complete, result.Status);
        Assert.Equal(1, result.Shortlist!.Counts.Conflicting);
        Assert.Empty(result.Shortlist.RankedEntries);
        Assert.Equal(
            CompanionRoleCandidateRankingState.Conflicting,
            Assert.Single(result.Shortlist.Entries).Candidate.State);
    }

    [Theory]
    [InlineData("UNKNOWN_ROLE", "1", CompanionFinderStatus.UnknownRole)]
    [InlineData("MARTIAL_DISCIPLINE_APTITUDE", "999", CompanionFinderStatus.UnsupportedRoleVersion)]
    public async Task Unsupported_role_selection_is_typed_without_reading_the_save(
        string roleIdentity,
        string roleVersion,
        CompanionFinderStatus expectedStatus)
    {
        var reader = Substitute.For<ICompanionCandidateSnapshotReader>();
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();

        var result = await new FindCompanionCandidates(reader, source, repository)
            .ExecuteAsync(
                Request(roleIdentity, roleVersion),
                TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, result.Status);
        Assert.False(result.HasAuthoritativeResult);
        Assert.NotNull(result.FailureIdentity);
        await reader.DidNotReceive().ReadAsync(
            Arg.Any<CompanionCandidateSnapshotReadRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(99, null, null)]
    [InlineData(0, 1, null)]
    [InlineData(0, 1, 1)]
    public async Task Invalid_filter_or_comparison_shape_is_typed_before_source_read(
        int filter,
        int? first,
        int? second)
    {
        var reader = Substitute.For<ICompanionCandidateSnapshotReader>();
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        var request = Request(
            filter: (CompanionRoleShortlistFilter)filter,
            firstComparisonCharacterId: first,
            secondComparisonCharacterId: second);

        var result = await new FindCompanionCandidates(reader, source, repository)
            .ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(CompanionFinderStatus.InvalidRequest, result.Status);
        Assert.Equal("COMPANION_FINDER_REQUEST_INVALID", result.FailureIdentity);
        await reader.DidNotReceive().ReadAsync(
            Arg.Any<CompanionCandidateSnapshotReadRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_comparison_candidate_retains_authoritative_result_identity()
    {
        var snapshot = Snapshot([Profile(1, score: 70)]);
        var (reader, source, repository) = CurrentWorkflow(snapshot);

        var result = await new FindCompanionCandidates(reader, source, repository)
            .ExecuteAsync(
                Request(
                    firstComparisonCharacterId: 1,
                    secondComparisonCharacterId: 999),
                TestContext.Current.CancellationToken);

        Assert.Equal(CompanionFinderStatus.InvalidComparison, result.Status);
        Assert.True(result.HasAuthoritativeResult);
        Assert.Equal("COMPARISON_CANDIDATE_NOT_FOUND", result.FailureIdentity);
        Assert.NotNull(result.Shortlist);
        Assert.NotNull(result.View);
        Assert.Null(result.Comparison);
        Assert.NotNull(result.Fingerprint);
    }

    [Theory]
    [InlineData(CompanionCandidateSnapshotReadStatus.SaveUnavailable, CompanionFinderStatus.SaveUnavailable)]
    [InlineData(CompanionCandidateSnapshotReadStatus.UnsupportedVersion, CompanionFinderStatus.UnsupportedSourceVersion)]
    [InlineData(CompanionCandidateSnapshotReadStatus.ChangedRevision, CompanionFinderStatus.ChangedRevision)]
    [InlineData(CompanionCandidateSnapshotReadStatus.ReadFailed, CompanionFinderStatus.ReadFailed)]
    public async Task Snapshot_failures_are_typed_and_do_not_start_enrichment(
        CompanionCandidateSnapshotReadStatus readStatus,
        CompanionFinderStatus expectedStatus)
    {
        var reader = Reader(CompanionCandidateSnapshotReadResult.Failed(
            readStatus,
            "SYNTHETIC_FAILURE",
            "Synthetic read failure."));
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();

        var result = await new FindCompanionCandidates(reader, source, repository)
            .ExecuteAsync(Request(), TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(readStatus, result.SnapshotReadStatus);
        Assert.False(result.HasAuthoritativeResult);
        await source.DidNotReceive().ReadAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancellation_reaches_snapshot_projection_and_is_not_converted_to_failure()
    {
        using var cancellation = new CancellationTokenSource();
        var reader = Substitute.For<ICompanionCandidateSnapshotReader>();
        reader.ReadAsync(
                CompanionCandidateSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<CompanionCandidateSnapshotReadResult>(
                    call.ArgAt<CancellationToken>(1));
            });
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new FindCompanionCandidates(reader, source, repository)
                .ExecuteAsync(Request(), cancellation.Token));

        await reader.Received(1).ReadAsync(
            CompanionCandidateSnapshotReadRequest.Current,
            cancellation.Token);
    }

    [Fact]
    public async Task Filters_and_comparison_selection_do_not_change_authoritative_fingerprint()
    {
        var snapshot = Snapshot([
            Profile(1, score: 90),
            Profile(2, score: 80)
        ]);
        var (reader, source, repository) = CurrentWorkflow(snapshot);
        var workflow = new FindCompanionCandidates(reader, source, repository);

        var all = await workflow.ExecuteAsync(
            Request(filter: CompanionRoleShortlistFilter.All),
            TestContext.Current.CancellationToken);
        var filteredAndCompared = await workflow.ExecuteAsync(
            Request(
                filter: CompanionRoleShortlistFilter.Ranked,
                firstComparisonCharacterId: 1,
                secondComparisonCharacterId: 2),
            TestContext.Current.CancellationToken);

        Assert.Equal(all.Fingerprint, filteredAndCompared.Fingerprint);
        Assert.Equal(all.Shortlist!.Fingerprint, filteredAndCompared.Shortlist!.Fingerprint);
        Assert.Null(all.Comparison);
        Assert.NotNull(filteredAndCompared.Comparison);
        Assert.Equal(all.Shortlist.Counts, filteredAndCompared.View!.UnfilteredCounts);
    }

    [Fact]
    public async Task New_save_revision_builds_a_complete_new_result_without_mixing_profiles()
    {
        var firstSnapshot = Snapshot([Profile(1, score: 70)], Sha);
        var secondSnapshot = Snapshot([Profile(1, score: 90, revision: OtherSha)], OtherSha);
        var reader = Substitute.For<ICompanionCandidateSnapshotReader>();
        reader.ReadAsync(
                CompanionCandidateSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns(
                CompanionCandidateSnapshotReadResult.Complete(firstSnapshot),
                CompanionCandidateSnapshotReadResult.Complete(secondSnapshot));
        var identity = CatalogueIdentity();
        var source = DefinitionSource(identity);
        var repository = Repository(CatalogueRepositoryState.Ready, identity);
        var workflow = new FindCompanionCandidates(reader, source, repository);

        var first = await workflow.ExecuteAsync(
            Request(),
            TestContext.Current.CancellationToken);
        var second = await workflow.ExecuteAsync(
            Request(),
            TestContext.Current.CancellationToken);

        Assert.NotEqual(first.Fingerprint, second.Fingerprint);
        Assert.Equal(Sha, first.SourceIdentity!.CandidateSourceVersions.SaveSha256);
        Assert.Equal(OtherSha, second.SourceIdentity!.CandidateSourceVersions.SaveSha256);
        Assert.All(
            first.Ranking!.Candidates,
            item => Assert.Equal(Sha, item.Evaluation.Profile.SourceVersions.SaveSha256));
        Assert.All(
            second.Ranking!.Candidates,
            item => Assert.Equal(OtherSha, item.Evaluation.Profile.SourceVersions.SaveSha256));
    }

    private const string Sha =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private const string OtherSha =
        "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

    private static CompanionFinderRequest Request(
        string roleIdentity = "MARTIAL_DISCIPLINE_APTITUDE",
        string roleVersion = "1",
        CandidateDisciplineDomain domain = CandidateDisciplineDomain.Martial,
        CompanionRoleShortlistFilter filter = CompanionRoleShortlistFilter.All,
        int? firstComparisonCharacterId = null,
        int? secondComparisonCharacterId = null) => new(
            roleIdentity,
            roleVersion,
            domain,
            0,
            filter,
            firstComparisonCharacterId,
            secondComparisonCharacterId);

    private static (
        ICompanionCandidateSnapshotReader Reader,
        ICombatSkillDefinitionSource Source,
        ICombatSkillCatalogueRepository Repository) CurrentWorkflow(
            CompanionCandidateSnapshot snapshot,
            CompanionCandidateSnapshotReadStatus readStatus = CompanionCandidateSnapshotReadStatus.Complete)
    {
        var result = readStatus == CompanionCandidateSnapshotReadStatus.Complete
            ? CompanionCandidateSnapshotReadResult.Complete(snapshot)
            : CompanionCandidateSnapshotReadResult.Partial(snapshot);
        var identity = CatalogueIdentity();
        return (
            Reader(result),
            DefinitionSource(identity),
            Repository(CatalogueRepositoryState.Ready, identity));
    }

    private static ICompanionCandidateSnapshotReader Reader(
        CompanionCandidateSnapshotReadResult result)
    {
        var reader = Substitute.For<ICompanionCandidateSnapshotReader>();
        reader.ReadAsync(
                CompanionCandidateSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns(result);
        return reader;
    }

    private static ICombatSkillDefinitionSource DefinitionSource(
        CombatSkillCatalogueSourceIdentity identity)
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            CombatSkillDefinitionSourceResult.Available(identity, []));
        return source;
    }

    private static ICombatSkillCatalogueRepository Repository(
        CatalogueRepositoryState state,
        CombatSkillCatalogueSourceIdentity? identity = null)
    {
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>()).Returns(
            new CombatSkillCatalogueRepositorySnapshot(
                state,
                identity,
                0,
                state == CatalogueRepositoryState.Ready
                    ? DateTimeOffset.Parse("2026-08-17T12:00:00Z")
                    : null,
                state is CatalogueRepositoryState.Corrupt or CatalogueRepositoryState.Failed
                    ? "Synthetic repository failure."
                    : null));
        repository.QueryAsync(
                Arg.Any<CombatSkillCatalogueFilter>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        return repository;
    }

    private static CombatSkillCatalogueSourceIdentity CatalogueIdentity(
        char hash = 'A') => new(
            VerifiedCompanionRoleDefinitions.SupportedGameDataVersion,
            3,
            new string(hash, 64),
            new string(hash, 64),
            new string(hash, 64));

    private static CompanionCandidateSnapshot Snapshot(
        IEnumerable<CandidateProfile> profiles,
        string revision = Sha) => new(
            DateTimeOffset.Parse("2026-08-17T12:00:00Z"),
            Versions(revision),
            profiles,
            [],
            [],
            []);

    private static CandidateProfile Profile(
        int characterId,
        short? score = null,
        IEnumerable<CandidateProfileFact>? facts = null,
        string revision = Sha) => new(
            new CandidateIdentity(characterId),
            CandidateUniverseState.Eligible,
            Versions(revision),
            (facts ?? (score.HasValue ? [ScoreFact(score.Value, revision)] : []))
                .Concat(MembershipFacts(revision)),
            []);

    private static CandidateProfileFact ScoreFact(
        short score,
        string revision) => CandidateProfileFact.Confirmed(
            ScoreField(),
            CandidateFactValue.Int16(score),
            Provenance(revision),
            [new CandidateEvidenceReference("E6-SAVE-SCORE", Provenance(revision))]);

    private static CandidateProfileFact ConflictingScoreFact()
    {
        var first = Provenance(Sha);
        var second = Provenance(OtherSha);
        return CandidateProfileFact.Conflicting(
            ScoreField(),
            [
                new CandidateConflictValue(
                    CandidateFactValue.Int16(70),
                    first,
                    [new CandidateEvidenceReference("E6-SAVE-001", first)]),
                new CandidateConflictValue(
                    CandidateFactValue.Int16(80),
                    second,
                    [new CandidateEvidenceReference("E6-SAVE-002", second)])
            ],
            new CandidateConflictDecision(
                CandidateConflictDecisionKind.Unresolved,
                "NO_SAFE_PRECEDENCE"),
            []);
    }

    private static IEnumerable<CandidateProfileFact> MembershipFacts(string revision) =>
        new[]
        {
            SetFact(CandidateProfileField.LearnedMartialSkillIdentities, revision),
            SetFact(CandidateProfileField.EquippedMartialSkillIdentities, revision),
            SetFact(CandidateProfileField.LearnedLifeSkillIdentities, revision)
        };

    private static IEnumerable<CandidateProfileFact> CapabilityFacts(short value)
    {
        foreach (var attribute in Enum.GetValues<CandidateMainAttribute>())
        {
            yield return ScalarFact(
                new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseMainAttribute,
                    attribute),
                value);
        }

        for (short type = 0;
             type < CompanionCapabilitySummary.MartialDisciplineCount;
             type++)
        {
            yield return ScalarFact(
                new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseMartialQualification,
                    new CandidateDisciplineIdentity(
                        CandidateDisciplineDomain.Martial,
                        type)),
                value);
        }

        for (short type = 0;
             type < CompanionCapabilitySummary.LifeSkillDisciplineCount;
             type++)
        {
            yield return ScalarFact(
                new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseLifeSkillQualification,
                    new CandidateDisciplineIdentity(
                        CandidateDisciplineDomain.LifeSkill,
                        type)),
                value);
        }
    }

    private static CandidateProfileFact ScalarFact(
        CandidateProfileFieldIdentity field,
        short value) => CandidateProfileFact.Confirmed(
        field,
        CandidateFactValue.Int16(value),
        Provenance(Sha),
        []);

    private static CandidateProfileFact SetFact(
        CandidateProfileField field,
        string revision) => CandidateProfileFact.Confirmed(
            new CandidateProfileFieldIdentity(field),
            CandidateFactValue.Int32Set([]),
            Provenance(revision),
            []);

    private static CandidateProfileFieldIdentity ScoreField() => new(
        CandidateProfileField.BaseMartialQualification,
        new CandidateDisciplineIdentity(CandidateDisciplineDomain.Martial, 0));

    private static CandidateFactProvenance Provenance(string revision) => new(
        CandidateEvidenceSourceKind.ConfiguredSave,
        "CONFIGURED_SAVE",
        VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
        revision);

    private static CandidateProfileSourceVersions Versions(string revision) => new(
        revision,
        VerifiedCompanionRoleDefinitions.SupportedGameDataVersion,
        VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
        "1",
        VerifiedCompanionRoleDefinitions.FingerprintSchemaVersion);
}
