using NSubstitute;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CompanionCandidates;
using Xunit;

namespace TaiWu.Application.UnitTests.CompanionCandidates;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CompanionCandidateEnrichmentCollection
{
    public const string Name = "Companion candidate enrichment";
}

[Collection(CompanionCandidateEnrichmentCollection.Name)]
public sealed class EnrichCompanionCandidateProfilesTests
{
    [Fact]
    public async Task Current_catalogue_enriches_stable_membership_without_changing_profile()
    {
        var profile = Profile(
            42,
            learned: [5, 2],
            equipped: [8, 5],
            life: [12]);
        var snapshot = Snapshot([profile]);
        var definitions = new[] { Definition(8), Definition(2), Definition(5) };
        var (source, repository) = CurrentCatalogue(definitions);
        var useCase = new EnrichCompanionCandidateProfiles(source, repository);

        var result = await useCase.ExecuteAsync(
            snapshot,
            TestContext.Current.CancellationToken);

        Assert.Equal(CompanionCandidateEnrichmentStatus.Complete, result.Status);
        Assert.Equal(CombatSkillCatalogueStatus.Current, result.CatalogueStatus);
        var candidate = Assert.Single(result.Candidates);
        Assert.Same(profile, candidate.Profile);
        Assert.Equal(profile.Fingerprint, candidate.Profile.Fingerprint);
        Assert.Equal(CompanionCandidateEnrichmentState.Complete, candidate.State);
        Assert.Equal([2, 5, 8], candidate.CombatSkills.Select(item => item.SkillId));
        AssertMembership(candidate.CombatSkills[0], learned: true, equipped: false);
        AssertMembership(candidate.CombatSkills[1], learned: true, equipped: true);
        AssertMembership(candidate.CombatSkills[2], learned: false, equipped: true);
        Assert.All(candidate.CombatSkills, item =>
        {
            Assert.Equal(CompanionSkillDefinitionState.Available, item.DefinitionState);
            Assert.Equal(
                CompanionDetailedProgressState.NotRequestedByApprovedRole,
                item.DetailedProgressState);
        });
        await repository.Received(1).QueryAsync(
            Arg.Any<CombatSkillCatalogueFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Catalogue_version_mismatch_fails_closed_without_querying_definitions()
    {
        var definitions = new[] { Definition(2) };
        var identity = CatalogueIdentity(gameDataVersion: "OTHER-VERSION");
        var (source, repository) = CurrentCatalogue(definitions, identity);
        var result = await new EnrichCompanionCandidateProfiles(source, repository)
            .ExecuteAsync(
                Snapshot([Profile(42, learned: [2])]),
                TestContext.Current.CancellationToken);

        Assert.Equal(
            CompanionCandidateEnrichmentStatus.CatalogueUnsupported,
            result.Status);
        Assert.Equal(CombatSkillCatalogueStatus.UnsupportedVersion, result.CatalogueStatus);
        Assert.Equal(
            CompanionCandidateEnrichmentState.CatalogueUnsupported,
            Assert.Single(result.Candidates).State);
        await repository.DidNotReceive().QueryAsync(
            Arg.Any<CombatSkillCatalogueFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(CatalogueRepositoryState.Missing, CompanionCandidateEnrichmentStatus.CatalogueMissing)]
    [InlineData(CatalogueRepositoryState.Corrupt, CompanionCandidateEnrichmentStatus.CatalogueFailed)]
    [InlineData(CatalogueRepositoryState.Failed, CompanionCandidateEnrichmentStatus.CatalogueFailed)]
    public async Task Unavailable_catalogue_retains_membership_without_fabricating_definitions(
        CatalogueRepositoryState repositoryState,
        CompanionCandidateEnrichmentStatus expectedStatus)
    {
        var definition = Definition(2);
        var sourceIdentity = CatalogueIdentity();
        var source = DefinitionSource(sourceIdentity, [definition]);
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>()).Returns(
            new CombatSkillCatalogueRepositorySnapshot(
                repositoryState,
                sourceIdentity: null,
                definitionCount: 0,
                builtAtUtc: null,
                repositoryState is CatalogueRepositoryState.Corrupt
                    or CatalogueRepositoryState.Failed
                    ? "Repository unavailable."
                    : null));
        var result = await new EnrichCompanionCandidateProfiles(source, repository)
            .ExecuteAsync(
                Snapshot([Profile(42, learned: [2])]),
                TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, result.Status);
        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(
            expectedStatus switch
            {
                CompanionCandidateEnrichmentStatus.CatalogueMissing =>
                    CompanionCandidateEnrichmentState.CatalogueMissing,
                CompanionCandidateEnrichmentStatus.CatalogueFailed =>
                    CompanionCandidateEnrichmentState.CatalogueFailed,
                _ => throw new ArgumentOutOfRangeException(nameof(expectedStatus))
            },
            candidate.State);
        var skill = Assert.Single(candidate.CombatSkills);
        Assert.True(skill.Learned.Value);
        Assert.Equal(
            CompanionSkillDefinitionState.CatalogueUnavailable,
            skill.DefinitionState);
        Assert.Null(skill.Definition);
        await repository.DidNotReceive().QueryAsync(
            Arg.Any<CombatSkillCatalogueFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stale_catalogue_is_distinct_and_preserves_stored_source_identity()
    {
        var definition = Definition(2);
        var installed = CatalogueIdentity(hash: 'A');
        var stored = CatalogueIdentity(hash: 'B');
        var source = DefinitionSource(installed, [definition]);
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>()).Returns(
            new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Ready,
                stored,
                1,
                DateTimeOffset.Parse("2026-08-17T12:00:00Z")));

        var result = await new EnrichCompanionCandidateProfiles(source, repository)
            .ExecuteAsync(
                Snapshot([Profile(42, learned: [2])]),
                TestContext.Current.CancellationToken);

        Assert.Equal(CompanionCandidateEnrichmentStatus.CatalogueStale, result.Status);
        Assert.Equal(CombatSkillCatalogueStatus.Stale, result.CatalogueStatus);
        Assert.Equal(installed, result.CatalogueSource);
        Assert.Equal(
            CompanionCandidateEnrichmentState.CatalogueStale,
            Assert.Single(result.Candidates).State);
    }

    [Fact]
    public async Task Incomplete_membership_remains_unknown_not_false_or_empty()
    {
        var profile = Profile(
            42,
            learnedFact: CandidateProfileFact.Incomplete(
                Field(CandidateProfileField.LearnedMartialSkillIdentities),
                new CandidateUnavailableReason(
                    "LEARNED_MEMBERSHIP_MISSING",
                    "The saved learned collection is missing."),
                []),
            equipped: [8]);
        var (source, repository) = CurrentCatalogue([Definition(8)]);

        var result = await new EnrichCompanionCandidateProfiles(source, repository)
            .ExecuteAsync(
                Snapshot([profile]),
                TestContext.Current.CancellationToken);

        Assert.Equal(CompanionCandidateEnrichmentStatus.Partial, result.Status);
        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(CompanionMembershipEvidenceState.Incomplete, candidate.LearnedMartialState);
        var skill = Assert.Single(candidate.CombatSkills);
        Assert.Null(skill.Learned.Value);
        Assert.Equal(CompanionMembershipEvidenceState.Incomplete, skill.Learned.State);
        Assert.True(skill.Equipped.Value);
    }

    [Fact]
    public async Task Stale_and_conflicting_progress_provenance_are_typed()
    {
        var stale = CandidateProfileFact.Stale(
            Field(CandidateProfileField.LearnedMartialSkillIdentities),
            CandidateFactValue.Int32Set([2]),
            SaveProvenance(OtherSha),
            new CandidateUnavailableReason(
                "SAVE_REVISION_CHANGED",
                "The membership came from an older save."),
            []);
        var conflicting = CandidateProfileFact.Confirmed(
            Field(CandidateProfileField.EquippedMartialSkillIdentities),
            CandidateFactValue.Int32Set([2]),
            SaveProvenance(OtherSha),
            []);
        var profile = Profile(42, learnedFact: stale, equippedFact: conflicting);
        var (source, repository) = CurrentCatalogue([Definition(2)]);

        var result = await new EnrichCompanionCandidateProfiles(source, repository)
            .ExecuteAsync(
                Snapshot([profile]),
                TestContext.Current.CancellationToken);

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(CompanionMembershipEvidenceState.Stale, candidate.LearnedMartialState);
        Assert.Equal(CompanionMembershipEvidenceState.Conflicting, candidate.EquippedMartialState);
        Assert.Empty(candidate.CombatSkills);
        Assert.Equal(CompanionCandidateEnrichmentStatus.Partial, result.Status);
    }

    [Fact]
    public async Task Missing_definition_is_partial_but_retains_exact_membership()
    {
        var (source, repository) = CurrentCatalogue([Definition(2)]);
        var result = await new EnrichCompanionCandidateProfiles(source, repository)
            .ExecuteAsync(
                Snapshot([Profile(42, learned: [2, 9])]),
                TestContext.Current.CancellationToken);

        Assert.Equal(CompanionCandidateEnrichmentStatus.Partial, result.Status);
        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(2, candidate.CombatSkills.Length);
        var missing = candidate.CombatSkills.Single(item => item.SkillId == 9);
        Assert.Equal(CompanionSkillDefinitionState.Missing, missing.DefinitionState);
        Assert.True(missing.Learned.Value);
        Assert.Single(candidate.Diagnostics);
    }

    [Fact]
    public async Task Duplicate_query_is_typed_but_unexpected_repository_fault_propagates()
    {
        var definition = Definition(2);
        var identity = CatalogueIdentity();
        var source = DefinitionSource(identity, [definition, Definition(3)]);
        var duplicateRepository = ReadyRepository(identity, 2, [definition, definition]);
        var failedRepository = ReadyRepository(identity, 2, []);
        failedRepository.QueryAsync(
                Arg.Any<CombatSkillCatalogueFilter>(),
                Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<CombatSkillDefinition>>(_ =>
                throw new InvalidOperationException("synthetic repository failure"));

        var duplicate = await new EnrichCompanionCandidateProfiles(
                source,
                duplicateRepository)
            .ExecuteAsync(
                Snapshot([Profile(42, learned: [2])]),
                TestContext.Current.CancellationToken);
        Assert.Equal(CompanionCandidateEnrichmentStatus.CatalogueFailed, duplicate.Status);
        Assert.Equal(
            CompanionCandidateEnrichmentState.CatalogueFailed,
            Assert.Single(duplicate.Candidates).State);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new EnrichCompanionCandidateProfiles(source, failedRepository)
                .ExecuteAsync(
                    Snapshot([Profile(42, learned: [2])]),
                    TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task One_partial_candidate_does_not_suppress_unrelated_candidate()
    {
        var complete = Profile(2, learned: [2]);
        var partial = Profile(
            9,
            learnedFact: CandidateProfileFact.Unsupported(
                Field(CandidateProfileField.LearnedMartialSkillIdentities),
                new CandidateUnavailableReason(
                    "MEMBERSHIP_UNSUPPORTED",
                    "Membership mapping is unsupported."),
                []));
        var (source, repository) = CurrentCatalogue([Definition(2)]);

        var result = await new EnrichCompanionCandidateProfiles(source, repository)
            .ExecuteAsync(
                Snapshot([partial, complete]),
                TestContext.Current.CancellationToken);

        Assert.Equal([2, 9], result.Candidates.Select(item => item.Profile.Identity.CharacterId));
        Assert.Equal(CompanionCandidateEnrichmentState.Complete, result.Candidates[0].State);
        Assert.Equal(CompanionCandidateEnrichmentState.Partial, result.Candidates[1].State);
        Assert.Single(result.Candidates[0].CombatSkills);
        Assert.Empty(result.Candidates[1].CombatSkills);
    }

    [Fact]
    public async Task Join_order_and_localized_display_text_do_not_change_fingerprint()
    {
        var firstDefinitions = new[]
        {
            Definition(5, englishName: "Five"),
            Definition(2, englishName: "Two")
        };
        var secondDefinitions = new[]
        {
            Definition(2, englishName: "Localized text changed"),
            Definition(5, englishName: "Another display value")
        };
        var identity = CatalogueIdentity();
        var firstPair = CurrentCatalogue(firstDefinitions, identity);
        var secondPair = CurrentCatalogue(secondDefinitions, identity);
        var firstSnapshot = Snapshot([
            Profile(9, learned: [5]),
            Profile(2, learned: [2])
        ]);
        var secondSnapshot = Snapshot([
            Profile(2, learned: [2]),
            Profile(9, learned: [5])
        ]);

        var first = await new EnrichCompanionCandidateProfiles(
                firstPair.Source,
                firstPair.Repository)
            .ExecuteAsync(firstSnapshot, TestContext.Current.CancellationToken);
        var second = await new EnrichCompanionCandidateProfiles(
                secondPair.Source,
                secondPair.Repository)
            .ExecuteAsync(secondSnapshot, TestContext.Current.CancellationToken);

        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.Equal([2, 9], first.Candidates.Select(item => item.Profile.Identity.CharacterId));
    }

    [Fact]
    public async Task Enrichment_never_mutates_or_adds_role_evaluable_profile_facts()
    {
        var profile = Profile(42, learned: [2], features: [7, 3]);
        var originalFingerprint = profile.Fingerprint;
        var originalFacts = profile.Facts;
        var (source, repository) = CurrentCatalogue([Definition(2)]);

        var result = await new EnrichCompanionCandidateProfiles(source, repository)
            .ExecuteAsync(
                Snapshot([profile]),
                TestContext.Current.CancellationToken);

        var enriched = Assert.Single(result.Candidates);
        Assert.Same(profile, enriched.Profile);
        Assert.Equal(originalFingerprint, enriched.Profile.Fingerprint);
        Assert.Equal(originalFacts, enriched.Profile.Facts);
        Assert.Equal(
            [3, 7],
            enriched.Profile.FindFact(Field(CandidateProfileField.FeatureIdentities))!
                .Value!.Identities);
    }

    private const string Sha =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private const string OtherSha =
        "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

    private const string GameDataVersion =
        "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20";

    private static CompanionCandidateSnapshot Snapshot(
        IEnumerable<CandidateProfile> profiles) => new(
            DateTimeOffset.Parse("2026-08-17T12:00:00Z"),
            Versions(),
            profiles,
            [],
            [],
            []);

    private static CandidateProfile Profile(
        int characterId,
        IEnumerable<int>? learned = null,
        IEnumerable<int>? equipped = null,
        IEnumerable<int>? life = null,
        IEnumerable<int>? features = null,
        CandidateProfileFact? learnedFact = null,
        CandidateProfileFact? equippedFact = null) => new(
            new CandidateIdentity(characterId),
            CandidateUniverseState.Eligible,
            Versions(),
            new[]
            {
                learnedFact ?? SetFact(
                    CandidateProfileField.LearnedMartialSkillIdentities,
                    learned ?? []),
                equippedFact ?? SetFact(
                    CandidateProfileField.EquippedMartialSkillIdentities,
                    equipped ?? []),
                SetFact(
                    CandidateProfileField.LearnedLifeSkillIdentities,
                    life ?? []),
                SetFact(
                    CandidateProfileField.FeatureIdentities,
                    features ?? [])
            },
            []);

    private static CandidateProfileFact SetFact(
        CandidateProfileField field,
        IEnumerable<int> values) => CandidateProfileFact.Confirmed(
            Field(field),
            CandidateFactValue.Int32Set(values),
            SaveProvenance(Sha),
            []);

    private static CandidateProfileFieldIdentity Field(
        CandidateProfileField field) => new(field);

    private static CandidateFactProvenance SaveProvenance(string revision) => new(
        CandidateEvidenceSourceKind.ConfiguredSave,
        "TAIWU_CONFIGURED_SAVE",
        "1",
        revision);

    private static CandidateProfileSourceVersions Versions() => new(
        Sha,
        GameDataVersion,
        "1",
        "1",
        "1");

    private static void AssertMembership(
        CompanionCombatSkillEnrichment skill,
        bool learned,
        bool equipped)
    {
        Assert.Equal(CompanionMembershipEvidenceState.Available, skill.Learned.State);
        Assert.Equal(learned, skill.Learned.Value);
        Assert.Equal(CompanionMembershipEvidenceState.Available, skill.Equipped.State);
        Assert.Equal(equipped, skill.Equipped.Value);
    }

    private static (
        ICombatSkillDefinitionSource Source,
        ICombatSkillCatalogueRepository Repository) CurrentCatalogue(
            IReadOnlyList<CombatSkillDefinition> definitions,
            CombatSkillCatalogueSourceIdentity? identity = null)
    {
        identity ??= CatalogueIdentity();
        return (
            DefinitionSource(identity, definitions),
            ReadyRepository(identity, definitions.Count, definitions));
    }

    private static ICombatSkillDefinitionSource DefinitionSource(
        CombatSkillCatalogueSourceIdentity identity,
        IReadOnlyList<CombatSkillDefinition> definitions)
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            CombatSkillDefinitionSourceResult.Available(identity, definitions));
        return source;
    }

    private static ICombatSkillCatalogueRepository ReadyRepository(
        CombatSkillCatalogueSourceIdentity identity,
        int definitionCount,
        IReadOnlyList<CombatSkillDefinition> definitions)
    {
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>()).Returns(
            new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Ready,
                identity,
                definitionCount,
                DateTimeOffset.Parse("2026-08-17T12:00:00Z")));
        repository.QueryAsync(
                Arg.Any<CombatSkillCatalogueFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(definitions);
        return repository;
    }

    private static CombatSkillCatalogueSourceIdentity CatalogueIdentity(
        string gameDataVersion = GameDataVersion,
        char hash = 'A') => new(
            gameDataVersion,
            importerVersion: 3,
            new string(hash, 64),
            new string(hash, 64),
            new string(hash, 64));

    private static CombatSkillDefinition Definition(
        int skillId,
        string englishName = "Synthetic skill")
    {
        var source = new CatalogueSourceReference(
            CatalogueSourceKind.GameData,
            "gamedata:test",
            $"combat-skill:{skillId}");
        return new CombatSkillDefinition(
            skillId,
            new CombatSkillLocalizedNames([
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
            CatalogueField<CombatSkillGrade>.Available(new CombatSkillGrade(5), source),
            CatalogueField<CombatSkillFactionId>.Available(new CombatSkillFactionId(1), source),
            CatalogueField<CombatSkillElement>.Available(CombatSkillElement.Wood, source),
            CatalogueField<CombatSkillEquipmentType>.Available(
                CombatSkillEquipmentType.Attack,
                source),
            CatalogueField<CombatSkillGridCost>.Available(new CombatSkillGridCost(2), source),
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
                CatalogueField<CombatSkillEffectId>.Unavailable("not required")),
            rawDescriptions: null,
            source);
    }
}
