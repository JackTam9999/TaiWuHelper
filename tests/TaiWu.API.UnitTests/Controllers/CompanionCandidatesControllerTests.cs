using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NSubstitute;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.Localization;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using TaiWuAPI.Contracts.CompanionCandidates;
using TaiWuAPI.Controllers;
using Xunit;

namespace TaiWu.API.UnitTests.Controllers;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CompanionCandidatesApiCollection
{
    public const string Name = "Companion candidates API";
}

[Collection(CompanionCandidatesApiCollection.Name)]
public sealed class CompanionCandidatesControllerTests
{
    [Fact]
    public void Role_discovery_localizes_the_same_stable_presets()
    {
        var english = CompanionFinderResponseMapper.MapRoles(TaiwuLanguage.English);
        var chinese = CompanionFinderResponseMapper.MapRoles(TaiwuLanguage.Chinese);

        Assert.Equal(2, english.Roles.Count);
        Assert.Equal(
            english.Roles.Select(item => item.Identity),
            chinese.Roles.Select(item => item.Identity));
        Assert.Equal(
            english.Roles.Select(item => item.Reference),
            chinese.Roles.Select(item => item.Reference));
        Assert.All(english.Roles, item =>
        {
            Assert.Equal(CompanionRolePresetStatus.Supported, item.Status);
            Assert.Equal("1", item.RoleVersion);
            Assert.Equal("1", item.EvaluationRuleVersion);
            Assert.Contains("not a universal ranking", item.ScoreLimitation);
        });
        Assert.All(
            english.Roles.Zip(chinese.Roles),
            pair =>
            {
                Assert.NotEqual(pair.First.Purpose, pair.Second.Purpose);
                Assert.NotEqual(pair.First.ScoreLimitation, pair.Second.ScoreLimitation);
            });
    }

    [Fact]
    public async Task Complete_response_maps_source_components_evidence_enrichment_and_comparison()
    {
        var location = LocationFact(11);
        var snapshot = Snapshot([
            Profile(2, facts: [ScoreFact(90), location]),
            Profile(1, score: 70)
        ]);
        var result = await Execute(
            Workflow(snapshot),
            Request(
                firstComparisonCharacterId: 2,
                secondComparisonCharacterId: 1));

        var response = CompanionFinderResponseMapper.Map(
            result,
            TaiwuLanguage.English);

        Assert.Equal(CompanionFinderStatus.Complete, response.Status);
        Assert.Equal(
            CompanionCandidateSnapshotReadStatus.Complete,
            response.Source!.SnapshotReadStatus);
        Assert.Equal(Sha, response.Source!.SaveFingerprint);
        Assert.Equal(CombatSkillCatalogueStatus.Current, response.Source.CatalogueStatus);
        Assert.NotNull(response.Source.CatalogueSource);
        Assert.Equal("MARTIAL_DISCIPLINE_APTITUDE", response.Role!.Identity);
        Assert.Contains("not a universal ranking", response.Role.ScoreLimitation);
        Assert.Equal(2, response.Counts!.Total);
        Assert.Equal(2, response.Counts.Eligible);
        Assert.Equal(2, response.Counts.Visible);
        Assert.Equal(["companion-candidate:2", "companion-candidate:1"],
            response.Candidates.Select(item => item.Reference));
        var first = response.Candidates[0];
        Assert.Equal(1, first.CompetitionRank);
        Assert.Equal(90m, first.TotalScore);
        Assert.All(first.Gates, gate => Assert.Equal(CompanionRoleGateOutcome.Passed, gate.Outcome));
        var component = Assert.Single(first.Components);
        Assert.Equal("BASE_MARTIAL_QUALIFICATION", component.DimensionIdentity);
        Assert.Equal((short)90, component.RawValue);
        Assert.Equal(90m, component.Contribution);
        Assert.Single(component.Evidence);
        Assert.Equal(CompanionFactEvidenceState.Confirmed, Assert.Single(first.ScoreFacts).EvidenceState);
        Assert.Equal(
            CompanionCapabilitySummaryState.Complete,
            first.CapabilitySummary.State);
        Assert.Equal(
            CompanionCapabilitySummaryFormula.EqualCategoryMean,
            first.CapabilitySummary.Formula);
        Assert.Equal(48.64m, first.CapabilitySummary.BreadthIndex);
        Assert.Equal(6, first.CapabilitySummary.MainAttributes.ConfirmedCount);
        Assert.Equal(14, first.CapabilitySummary.MartialDisciplines.ExpectedCount);
        Assert.Equal(16, first.CapabilitySummary.LifeSkillDisciplines.ExpectedCount);
        Assert.Contains(
            first.CapabilitySummary.MainAttributes.Components,
            item => item.MainAttribute == CandidateMainAttribute.Intelligence
                && item.Value == 57);
        Assert.Same(location, snapshot.Profiles[1].Facts.Single(item =>
            item.Identity.Field == CandidateProfileField.CurrentLocationArea));
        Assert.Single(first.LocationEvidence);
        Assert.Single(first.AvailableLocationFacts);
        Assert.Equal(CompanionCandidateEnrichmentState.Complete, first.Enrichment.State);
        Assert.Contains(response.Diagnostics, item => item.Identity == "ROLE_SCORE_IS_ROLE_LOCAL");
        Assert.Equal(CompanionRoleComparisonOutcome.FirstAdvantage, response.Comparison!.Outcome);
        Assert.Equal((short)90, Assert.Single(response.Comparison.Rows).First.Value);
        Assert.NotNull(response.Fingerprint);
        Assert.Null(response.Failure);
    }

    [Fact]
    public async Task Missing_and_conflicting_score_facts_remain_typed_and_unscored()
    {
        var conflict = ConflictingScoreFact();
        var result = await Execute(
            Workflow(Snapshot([
                Profile(1),
                Profile(2, facts: [conflict])
            ])),
            Request(
                firstComparisonCharacterId: 1,
                secondComparisonCharacterId: 2));

        var response = CompanionFinderResponseMapper.Map(result, TaiwuLanguage.English);

        Assert.Equal(1, response.Counts!.Incomplete);
        Assert.Equal(1, response.Counts.Conflicting);
        var missing = response.Candidates.Single(item => item.CharacterId == 1);
        var conflicting = response.Candidates.Single(item => item.CharacterId == 2);
        Assert.Null(missing.TotalScore);
        Assert.Equal(
            CompanionFactEvidenceState.Missing,
            Assert.Single(missing.ScoreFacts).EvidenceState);
        Assert.Null(conflicting.TotalScore);
        var conflictFact = Assert.Single(conflicting.ScoreFacts);
        Assert.Equal(CompanionFactEvidenceState.Conflicting, conflictFact.EvidenceState);
        Assert.Equal(2, conflictFact.Conflicts.Count);
        Assert.Equal(
            CandidateConflictDecisionKind.Unresolved,
            conflictFact.ConflictDecision!.Kind);
        Assert.Equal(CompanionRoleComparisonOutcome.Conflicting, response.Comparison!.Outcome);
    }

    [Fact]
    public async Task Eligible_count_uses_candidate_universe_not_role_rankability()
    {
        var result = await Execute(
            Workflow(Snapshot([
                Profile(1, score: 90),
                Profile(2),
                Profile(
                    3,
                    universeState: CandidateUniverseState.Incomplete),
                Profile(
                    4,
                    score: 99,
                    universeState: CandidateUniverseState.Ineligible)
            ])),
            Request());

        var response = CompanionFinderResponseMapper.Map(
            result,
            TaiwuLanguage.English);

        Assert.Equal(4, response.Counts!.Total);
        Assert.Equal(2, response.Counts.Eligible);
        Assert.Equal(1, response.Counts.Ranked);
        Assert.Equal(2, response.Counts.Incomplete);
        Assert.Equal(1, response.Counts.Ineligible);
    }

    [Fact]
    public async Task Partial_catalogue_result_retains_ranked_candidates_and_typed_enrichment()
    {
        var snapshot = Snapshot([Profile(1, score: 70)]);
        var installed = CatalogueIdentity(hash: 'A');
        var stored = CatalogueIdentity(hash: 'B');
        var result = await Execute(
            Workflow(snapshot, installed, stored),
            Request());

        var response = CompanionFinderResponseMapper.Map(result, TaiwuLanguage.English);

        Assert.Equal(CompanionFinderStatus.Partial, response.Status);
        Assert.Equal(CompanionCandidateEnrichmentStatus.CatalogueStale, response.Enrichment!.Status);
        Assert.Equal(CompanionCandidateEnrichmentState.CatalogueStale, Assert.Single(response.Candidates).Enrichment.State);
        Assert.Equal(1, response.Counts!.Eligible);
        Assert.Equal(1, response.Counts!.Ranked);
    }

    [Fact]
    public async Task Localization_changes_display_text_not_semantic_facts()
    {
        var result = await Execute(
            Workflow(Snapshot([Profile(1, score: 70)])),
            Request());

        var english = CompanionFinderResponseMapper.Map(result, TaiwuLanguage.English);
        var chinese = CompanionFinderResponseMapper.Map(result, TaiwuLanguage.Chinese);

        Assert.Equal(english.Fingerprint, chinese.Fingerprint);
        Assert.Equal(english.Role!.Identity, chinese.Role!.Identity);
        Assert.Equal(
            english.Candidates.Select(item => item.Reference),
            chinese.Candidates.Select(item => item.Reference));
        Assert.Equal(
            english.Candidates.Select(item => item.TotalScore),
            chinese.Candidates.Select(item => item.TotalScore));
        Assert.NotEqual(english.Role.Purpose, chinese.Role.Purpose);
        Assert.NotEqual(
            english.Candidates[0].RankingStateLabel,
            chinese.Candidates[0].RankingStateLabel);
    }

    [Fact]
    public void Controller_contract_has_role_discovery_and_finder_only()
    {
        var controller = typeof(CompanionCandidatesController);
        Assert.Equal(
            "api/companion-candidates",
            controller.GetCustomAttribute<RouteAttribute>()?.Template);
        var actions = controller.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(2, actions.Length);
        Assert.Equal("Find", actions[0].Name);
        Assert.NotNull(actions[0].GetCustomAttribute<HttpPostAttribute>());
        Assert.Equal("find", actions[0].GetCustomAttribute<HttpPostAttribute>()?.Template);
        Assert.Equal("Roles", actions[1].Name);
        Assert.NotNull(actions[1].GetCustomAttribute<HttpGetAttribute>());
        Assert.Equal("roles", actions[1].GetCustomAttribute<HttpGetAttribute>()?.Template);
    }

    [Fact]
    public void Roles_endpoint_rejects_invalid_language_without_a_source_read()
    {
        var controller = Controller(Workflow(Snapshot([])));

        var action = controller.Roles((TaiwuLanguage)99);

        var problem = Assert.IsType<BadRequestObjectResult>(action.Result);
        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(
            "LANGUAGE_INVALID",
            Assert.IsType<ProblemDetails>(problem.Value).Extensions["code"]);
    }

    [Fact]
    public async Task Controller_returns_200_for_complete_and_206_for_partial()
    {
        var snapshot = Snapshot([Profile(1, score: 70)]);
        var complete = await Controller(Workflow(snapshot)).Find(
            ApiRequest(),
            TestContext.Current.CancellationToken);
        var installed = CatalogueIdentity(hash: 'A');
        var stored = CatalogueIdentity(hash: 'B');
        var partial = await Controller(Workflow(snapshot, installed, stored)).Find(
            ApiRequest(),
            TestContext.Current.CancellationToken);

        Assert.IsType<OkObjectResult>(complete.Result);
        var partialResult = Assert.IsType<ObjectResult>(partial.Result);
        Assert.Equal(206, partialResult.StatusCode);
        Assert.Equal(
            CompanionFinderStatus.Partial,
            Assert.IsType<CompanionFinderResponse>(partialResult.Value).Status);
    }

    [Theory]
    [InlineData(CompanionCandidateSnapshotReadStatus.SaveUnavailable, 404)]
    [InlineData(CompanionCandidateSnapshotReadStatus.UnsupportedVersion, 422)]
    [InlineData(CompanionCandidateSnapshotReadStatus.ChangedRevision, 409)]
    [InlineData(CompanionCandidateSnapshotReadStatus.ReadFailed, 500)]
    public async Task Controller_maps_snapshot_failures_to_distinct_http_statuses(
        CompanionCandidateSnapshotReadStatus readStatus,
        int expectedHttpStatus)
    {
        var failure = CompanionCandidateSnapshotReadResult.Failed(
            readStatus,
            "SYNTHETIC_READ_FAILURE",
            "Synthetic read failure.");
        var action = await Controller(Workflow(failure)).Find(
            ApiRequest(),
            TestContext.Current.CancellationToken);

        var problem = Assert.IsAssignableFrom<ObjectResult>(action.Result);
        Assert.Equal(expectedHttpStatus, problem.StatusCode);
        var details = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.NotNull(details.Extensions["code"]);
        Assert.DoesNotContain("Synthetic", details.Detail);
    }

    [Theory]
    [InlineData("", "1")]
    [InlineData("MARTIAL_DISCIPLINE_APTITUDE", "")]
    [InlineData("UNKNOWN_ROLE", "1")]
    [InlineData("MARTIAL_DISCIPLINE_APTITUDE", "999")]
    public async Task Invalid_unknown_or_unsupported_role_returns_400(
        string roleIdentity,
        string roleVersion)
    {
        var action = await Controller(Workflow(Snapshot([]))).Find(
            ApiRequest(roleIdentity, roleVersion),
            TestContext.Current.CancellationToken);

        var problem = Assert.IsAssignableFrom<ObjectResult>(action.Result);
        Assert.Equal(400, problem.StatusCode);
        Assert.IsType<ProblemDetails>(problem.Value);
    }

    [Fact]
    public async Task Unknown_comparison_identity_returns_typed_400_response_with_source_result()
    {
        var request = new CompanionFinderApiRequest
        {
            RoleIdentity = "MARTIAL_DISCIPLINE_APTITUDE",
            RoleVersion = "1",
            DisciplineDomain = CandidateDisciplineDomain.Martial,
            DisciplineType = 0,
            FirstComparisonCharacterId = 1,
            SecondComparisonCharacterId = 999,
            Language = TaiwuLanguage.English
        };
        var action = await Controller(
                Workflow(Snapshot([Profile(1, score: 70)])))
            .Find(request, TestContext.Current.CancellationToken);

        var result = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(400, result.StatusCode);
        var response = Assert.IsType<CompanionFinderResponse>(result.Value);
        Assert.Equal(CompanionFinderStatus.InvalidComparison, response.Status);
        Assert.NotNull(response.Source);
        Assert.NotNull(response.Fingerprint);
        Assert.Equal("COMPARISON_CANDIDATE_NOT_FOUND", response.Failure!.Identity);
    }

    [Fact]
    public async Task Cancellation_returns_distinct_499_problem()
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
        var action = await Controller(Workflow(reader)).Find(
            ApiRequest(),
            cancellation.Token);

        var problem = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(CompanionCandidatesController.ClientClosedRequestStatusCode, problem.StatusCode);
        Assert.Equal(
            "COMPANION_FINDER_CANCELLED",
            Assert.IsType<ProblemDetails>(problem.Value).Extensions["code"]);
    }

    [Fact]
    public async Task Response_serialization_contains_no_paths_runtime_types_or_raw_objects()
    {
        var result = await Execute(
            Workflow(Snapshot([Profile(1, score: 70)])),
            Request());
        var response = CompanionFinderResponseMapper.Map(result, TaiwuLanguage.English);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());

        var json = JsonSerializer.Serialize(response, options);

        Assert.DoesNotContain(@"C:\", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TaiWu.Domain", json, StringComparison.Ordinal);
        Assert.DoesNotContain("TaiWu.Infrastructure", json, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Reflection", json, StringComparison.Ordinal);
        Assert.DoesNotContain("rawContent", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MARTIAL_DISCIPLINE_APTITUDE", json);
        Assert.Contains("not a universal ranking", json);
        Assert.Contains("EqualCategoryMean", json);
        Assert.Contains("BreadthIndex", json);
    }

    [Fact]
    public async Task Candidate_display_context_localizes_without_changing_evaluation_facts()
    {
        var profile = Profile(7, score: 82);
        var display = new CompanionCandidateDisplay(
            profile.Identity,
            "範例人物",
            "Synthetic Person",
            "範例地點",
            "Synthetic Place");
        var result = await Execute(
            Workflow(Snapshot([profile], [display])),
            Request());

        var english = CompanionFinderResponseMapper.Map(
            result,
            TaiwuLanguage.English);
        var chinese = CompanionFinderResponseMapper.Map(
            result,
            TaiwuLanguage.Chinese);
        var englishCandidate = Assert.Single(english.Candidates);
        var chineseCandidate = Assert.Single(chinese.Candidates);

        Assert.Equal("Synthetic Person", englishCandidate.DisplayName);
        Assert.Equal("Synthetic Place", englishCandidate.LocationName);
        Assert.Equal("範例人物", chineseCandidate.DisplayName);
        Assert.Equal("範例地點", chineseCandidate.LocationName);
        Assert.Equal(englishCandidate.CharacterId, chineseCandidate.CharacterId);
        Assert.Equal(englishCandidate.RankingState, chineseCandidate.RankingState);
        Assert.Equal(englishCandidate.TotalScore, chineseCandidate.TotalScore);
        Assert.Equal(english.Fingerprint, chinese.Fingerprint);
    }

    [Fact]
    public void Api_contracts_expose_no_infrastructure_game_or_reflection_types()
    {
        var types = typeof(CompanionFinderResponse).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "TaiWuAPI.Contracts.CompanionCandidates")
            .ToArray();
        var signatures = types
            .SelectMany(type => type.GetProperties())
            .Select(property => Unwrap(property.PropertyType))
            .ToArray();

        Assert.NotEmpty(types);
        Assert.DoesNotContain(signatures, type =>
            type.Namespace?.StartsWith("TaiWu.Infrastructure", StringComparison.Ordinal) == true
            || type.Namespace?.StartsWith("GameData", StringComparison.Ordinal) == true
            || typeof(MemberInfo).IsAssignableFrom(type)
            || type == typeof(Type)
            || type == typeof(object));
        Assert.DoesNotContain(
            types.SelectMany(type => type.GetProperties()),
            property => property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Handle", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("RawContent", StringComparison.OrdinalIgnoreCase));
    }

    private const string Sha =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private const string OtherSha =
        "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

    private static CompanionCandidatesController Controller(
        FindCompanionCandidates workflow) => new(workflow);

    private static CompanionFinderApiRequest ApiRequest(
        string roleIdentity = "MARTIAL_DISCIPLINE_APTITUDE",
        string roleVersion = "1") => new()
        {
            RoleIdentity = roleIdentity,
            RoleVersion = roleVersion,
            DisciplineDomain = CandidateDisciplineDomain.Martial,
            DisciplineType = 0,
            Language = TaiwuLanguage.English
        };

    private static CompanionFinderRequest Request(
        int? firstComparisonCharacterId = null,
        int? secondComparisonCharacterId = null) => new(
            "MARTIAL_DISCIPLINE_APTITUDE",
            "1",
            CandidateDisciplineDomain.Martial,
            0,
            CompanionRoleShortlistFilter.All,
            firstComparisonCharacterId,
            secondComparisonCharacterId);

    private static async Task<CompanionFinderResult> Execute(
        FindCompanionCandidates workflow,
        CompanionFinderRequest request) => await workflow.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

    private static FindCompanionCandidates Workflow(
        CompanionCandidateSnapshot snapshot,
        CombatSkillCatalogueSourceIdentity? installed = null,
        CombatSkillCatalogueSourceIdentity? stored = null)
    {
        installed ??= CatalogueIdentity();
        stored ??= installed;
        return Workflow(
            Reader(CompanionCandidateSnapshotReadResult.Complete(snapshot)),
            installed,
            stored);
    }

    private static FindCompanionCandidates Workflow(
        CompanionCandidateSnapshotReadResult result) =>
        Workflow(Reader(result));

    private static FindCompanionCandidates Workflow(
        ICompanionCandidateSnapshotReader reader,
        CombatSkillCatalogueSourceIdentity? installed = null,
        CombatSkillCatalogueSourceIdentity? stored = null)
    {
        installed ??= CatalogueIdentity();
        stored ??= installed;
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            CombatSkillDefinitionSourceResult.Available(installed, []));
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>()).Returns(
            new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Ready,
                stored,
                0,
                DateTimeOffset.Parse("2026-08-17T12:00:00Z")));
        repository.QueryAsync(
                Arg.Any<CombatSkillCatalogueFilter>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        return new FindCompanionCandidates(reader, source, repository);
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

    private static CombatSkillCatalogueSourceIdentity CatalogueIdentity(
        char hash = 'A') => new(
            VerifiedCompanionRoleDefinitions.SupportedGameDataVersion,
            3,
            new string(hash, 64),
            new string(hash, 64),
            new string(hash, 64));

    private static CompanionCandidateSnapshot Snapshot(
        IEnumerable<CandidateProfile> profiles,
        IEnumerable<CompanionCandidateDisplay>? displays = null) => new(
            DateTimeOffset.Parse("2026-08-17T12:00:00Z"),
            Versions(),
            profiles,
            [],
            [],
            [],
            displays);

    private static CandidateProfile Profile(
        int characterId,
        short? score = null,
        IEnumerable<CandidateProfileFact>? facts = null,
        CandidateUniverseState universeState =
            CandidateUniverseState.Eligible) => new(
            new CandidateIdentity(characterId),
            universeState,
            Versions(),
            (facts ?? (score.HasValue ? [ScoreFact(score.Value)] : []))
                .Concat(MembershipFacts())
                .Concat(CapabilityFacts(characterId)),
            []);

    private static CandidateProfileFact ScoreFact(short value) =>
        CandidateProfileFact.Confirmed(
            ScoreField(),
            CandidateFactValue.Int16(value),
            Provenance(Sha),
            [new CandidateEvidenceReference("E6-SAVE-SCORE", Provenance(Sha))]);

    private static CandidateProfileFact LocationFact(int areaId) =>
        CandidateProfileFact.Confirmed(
            new CandidateProfileFieldIdentity(CandidateProfileField.CurrentLocationArea),
            CandidateFactValue.Int32(areaId),
            Provenance(Sha),
            [new CandidateEvidenceReference("E6-SAVE-LOCATION", Provenance(Sha))]);

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

    private static IEnumerable<CandidateProfileFact> MembershipFacts() =>
        new[]
        {
            SetFact(CandidateProfileField.LearnedMartialSkillIdentities),
            SetFact(CandidateProfileField.EquippedMartialSkillIdentities),
            SetFact(CandidateProfileField.LearnedLifeSkillIdentities)
        };

    private static IEnumerable<CandidateProfileFact> CapabilityFacts(
        int characterId)
    {
        var offset = characterId % 10;
        foreach (var attribute in Enum.GetValues<CandidateMainAttribute>())
        {
            yield return ScalarFact(
                new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseMainAttribute,
                    attribute),
                checked((short)(50 + (int)attribute + offset)));
        }

        for (short type = 1; type < 14; type++)
        {
            yield return ScalarFact(
                new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseMartialQualification,
                    new CandidateDisciplineIdentity(
                        CandidateDisciplineDomain.Martial,
                        type)),
                checked((short)(40 + type + offset)));
        }

        for (short type = 0; type < 16; type++)
        {
            yield return ScalarFact(
                new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseLifeSkillQualification,
                    new CandidateDisciplineIdentity(
                        CandidateDisciplineDomain.LifeSkill,
                        type)),
                checked((short)(30 + type + offset)));
        }
    }

    private static CandidateProfileFact ScalarFact(
        CandidateProfileFieldIdentity field,
        short value) => CandidateProfileFact.Confirmed(
        field,
        CandidateFactValue.Int16(value),
        Provenance(Sha),
        evidence: []);

    private static CandidateProfileFact SetFact(CandidateProfileField field) =>
        CandidateProfileFact.Confirmed(
            new CandidateProfileFieldIdentity(field),
            CandidateFactValue.Int32Set([]),
            Provenance(Sha),
            []);

    private static CandidateProfileFieldIdentity ScoreField() => new(
        CandidateProfileField.BaseMartialQualification,
        new CandidateDisciplineIdentity(CandidateDisciplineDomain.Martial, 0));

    private static CandidateFactProvenance Provenance(string revision) => new(
        CandidateEvidenceSourceKind.ConfiguredSave,
        "CONFIGURED_SAVE",
        VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
        revision);

    private static CandidateProfileSourceVersions Versions() => new(
        Sha,
        VerifiedCompanionRoleDefinitions.SupportedGameDataVersion,
        VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
        "1",
        VerifiedCompanionRoleDefinitions.FingerprintSchemaVersion);

    private static Type Unwrap(Type type)
    {
        if (type.IsGenericType)
        {
            return Unwrap(type.GetGenericArguments().Last());
        }

        return Nullable.GetUnderlyingType(type) ?? type;
    }
}
