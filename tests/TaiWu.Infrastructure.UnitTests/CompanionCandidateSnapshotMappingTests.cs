using Microsoft.Extensions.DependencyInjection;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests;

public sealed class CompanionCandidateSnapshotMappingTests
{
    [Fact]
    public void Complete_raw_candidate_maps_every_approved_fact_and_exact_role_inputs()
    {
        var mapped = CompanionCandidateSnapshotMapping.Map(Complete(), Versions());
        var profile = mapped.Profile;

        Assert.False(mapped.IsPartial);
        Assert.Equal(CandidateUniverseState.Eligible, profile.UniverseState);
        Assert.Empty(mapped.Diagnostics);
        Assert.Equal(108, profile.Facts.Length);
        Assert.True(profile.FindFact(Field(CandidateProfileField.RosterMembership))!
            .Value!.BooleanValue);
        Assert.False(profile.FindFact(Field(
            CandidateProfileField.VillageWorkCandidateMembership))!
            .Value!.BooleanValue);
        Assert.Equal(
            (short)13,
            profile.FindFact(MartialField(13))!.Value!.Int16Value);
        Assert.Equal(
            (short)115,
            profile.FindFact(LifeField(15))!.Value!.Int16Value);
        Assert.Equal(
            (short)60,
            profile.FindFact(AttributeField(CandidateMainAttribute.Intelligence))!
                .Value!.Int16Value);
        Assert.Equal(
            CandidateEvidenceState.Unsupported,
            profile.FindFact(new CandidateProfileFieldIdentity(
                CandidateProfileField.CurrentMartialQualification,
                Martial(0)))!.State);
        Assert.Equal(
            [2, 5, 9],
            profile.FindFact(Field(CandidateProfileField.FeatureIdentities))!
                .Value!.Identities);
        Assert.All(profile.Facts, fact => Assert.NotEmpty(fact.Evidence));
    }

    [Fact]
    public void Missing_character_is_retained_as_incomplete_profile()
    {
        var mapped = CompanionCandidateSnapshotMapping.Map(new RawCompanionCandidate(
            42,
            rosterMembership: true,
            villageWorkCandidateMembership: false,
            characterPresent: false,
            domainGroupMembership: true,
            characterGroupMembership: null,
            livingState: null,
            currentAge: null,
            locationArea: null,
            locationBlock: null,
            featureIdentities: null,
            baseMainAttributes: null,
            baseMartialQualifications: null,
            learnedMartialSkillIdentities: null,
            equippedMartialSkillIdentities: null,
            baseLifeSkillQualifications: null,
            learnedLifeSkillIdentities: null,
            failureIdentity: "CANDIDATE_CHARACTER_MISSING"), Versions());

        Assert.True(mapped.IsPartial);
        Assert.Equal(CandidateUniverseState.Incomplete, mapped.Profile.UniverseState);
        Assert.Equal(
            CandidateEvidenceState.Incomplete,
            mapped.Profile.FindFact(Field(CandidateProfileField.LivingState))!.State);
        Assert.Null(mapped.Profile.FindFact(MartialField(0))!.Value);
        Assert.Single(mapped.Profile.Diagnostics);
        Assert.Single(mapped.Diagnostics);
    }

    [Fact]
    public void Confirmed_dead_candidate_is_ineligible_not_incomplete()
    {
        var raw = Complete(livingState: false);
        var mapped = CompanionCandidateSnapshotMapping.Map(raw, Versions());

        Assert.False(mapped.IsPartial);
        Assert.Equal(CandidateUniverseState.Ineligible, mapped.Profile.UniverseState);
        Assert.False(mapped.Profile.FindFact(Field(CandidateProfileField.LivingState))!
            .Value!.BooleanValue);
    }

    [Fact]
    public void Membership_disagreement_is_explicit_conflict_state()
    {
        var mapped = CompanionCandidateSnapshotMapping.Map(
            Complete(domainMembership: false),
            Versions());

        Assert.False(mapped.IsPartial);
        Assert.Equal(CandidateUniverseState.Conflicting, mapped.Profile.UniverseState);
        Assert.False(mapped.Profile.FindFact(
            Field(CandidateProfileField.DomainGroupMembership))!.Value!.BooleanValue);
    }

    [Fact]
    public void Verified_village_work_candidate_is_eligible_without_group_membership()
    {
        var mapped = CompanionCandidateSnapshotMapping.Map(
            Complete(
                domainMembership: false,
                rosterMembership: false,
                villageWorkCandidateMembership: true,
                characterGroupMembership: false),
            Versions());

        Assert.Equal(CandidateUniverseState.Eligible, mapped.Profile.UniverseState);
        Assert.False(mapped.Profile.FindFact(Field(
            CandidateProfileField.RosterMembership))!.Value!.BooleanValue);
        Assert.True(mapped.Profile.FindFact(Field(
            CandidateProfileField.VillageWorkCandidateMembership))!
            .Value!.BooleanValue);
    }

    [Fact]
    public void Short_saved_buffers_create_partial_missing_facts_without_zero()
    {
        var raw = Complete(
            attributes: Enumerable.Range(50, 5).Select(value => (short)value),
            martial: Enumerable.Range(0, 13).Select(value => (short)value),
            life: Enumerable.Range(100, 15).Select(value => (short)value));
        var mapped = CompanionCandidateSnapshotMapping.Map(raw, Versions());

        Assert.True(mapped.IsPartial);
        var martialMissing = mapped.Profile.FindFact(MartialField(13))!;
        var lifeMissing = mapped.Profile.FindFact(LifeField(15))!;
        var attributeMissing = mapped.Profile.FindFact(AttributeField(
            CandidateMainAttribute.Intelligence))!;
        Assert.Equal(CandidateEvidenceState.Incomplete, martialMissing.State);
        Assert.Null(martialMissing.Value);
        Assert.Equal(CandidateEvidenceState.Incomplete, lifeMissing.State);
        Assert.Null(lifeMissing.Value);
        Assert.Equal(CandidateEvidenceState.Incomplete, attributeMissing.State);
        Assert.Null(attributeMissing.Value);
        Assert.Equal(
            (short)12,
            mapped.Profile.FindFact(MartialField(12))!.Value!.Int16Value);
    }

    [Fact]
    public void Invalid_identity_set_is_localized_to_one_incomplete_fact()
    {
        var mapped = CompanionCandidateSnapshotMapping.Map(
            Complete(features: [5, 5]),
            Versions());

        Assert.True(mapped.IsPartial);
        var featureFact = mapped.Profile.FindFact(
            Field(CandidateProfileField.FeatureIdentities))!;
        Assert.Equal(CandidateEvidenceState.Incomplete, featureFact.State);
        Assert.Equal("IDENTITY_SET_INVALID", featureFact.UnavailableReason!.Code);
        Assert.Equal(CandidateEvidenceState.Confirmed, mapped.Profile.FindFact(MartialField(0))!.State);
    }

    [Fact]
    public void Mapping_copies_inputs_and_has_deterministic_profile_fingerprint()
    {
        var features = new List<int> { 9, 2, 5 };
        var first = CompanionCandidateSnapshotMapping.Map(
            Complete(features: features),
            Versions());
        features.Clear();
        var second = CompanionCandidateSnapshotMapping.Map(
            Complete(features: [5, 9, 2]),
            Versions());

        Assert.Equal(first.Profile.Fingerprint, second.Profile.Fingerprint);
        Assert.Equal(
            [2, 5, 9],
            first.Profile.FindFact(Field(CandidateProfileField.FeatureIdentities))!
                .Value!.Identities);
    }

    [Fact]
    public void Infrastructure_registers_the_read_only_candidate_snapshot_port()
    {
        var services = new ServiceCollection();
        services.AddTaiwuInfrastructure();
        using var provider = services.BuildServiceProvider();

        var reader = provider.GetRequiredService<ICompanionCandidateSnapshotReader>();

        Assert.IsType<TaiwuCompanionCandidateSnapshotReader>(reader);
    }

    [Fact]
    public async Task Reader_returns_typed_save_unavailable_without_opening_an_archive()
    {
        var reader = new TaiwuCompanionCandidateSnapshotReader(
            readSession: null!,
            new MissingSavePathProvider(),
            new TaiwuGameTextResolver(),
            revisionProvider: null!,
            TimeProvider.System);

        var result = await reader.ReadAsync(
            CompanionCandidateSnapshotReadRequest.Current,
            TestContext.Current.CancellationToken);

        Assert.Equal(CompanionCandidateSnapshotReadStatus.SaveUnavailable, result.Status);
        Assert.Equal("CONFIGURED_SAVE_UNAVAILABLE", result.FailureIdentity);
        Assert.Null(result.Snapshot);
    }

    private const string Sha =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private static RawCompanionCandidate Complete(
        bool domainMembership = true,
        bool livingState = true,
        bool rosterMembership = true,
        bool villageWorkCandidateMembership = false,
        bool characterGroupMembership = true,
        IEnumerable<int>? features = null,
        IEnumerable<short>? attributes = null,
        IEnumerable<short>? martial = null,
        IEnumerable<short>? life = null) => new(
            42,
            rosterMembership,
            villageWorkCandidateMembership,
            characterPresent: true,
            domainGroupMembership: domainMembership,
            characterGroupMembership: characterGroupMembership,
            livingState: livingState,
            currentAge: 24,
            locationArea: 3,
            locationBlock: 7,
            featureIdentities: features ?? [9, 2, 5],
            baseMainAttributes: attributes
                ?? Enumerable.Range(55, 6).Select(value => (short)value),
            baseMartialQualifications: martial
                ?? Enumerable.Range(0, 14).Select(value => (short)value),
            learnedMartialSkillIdentities: [8, 3],
            equippedMartialSkillIdentities: [8],
            baseLifeSkillQualifications: life
                ?? Enumerable.Range(100, 16).Select(value => (short)value),
            learnedLifeSkillIdentities: [12, 4]);

    private static CandidateProfileSourceVersions Versions() => new(
        Sha,
        "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20",
        "3",
        "1",
        "3");

    private static CandidateProfileFieldIdentity Field(
        CandidateProfileField field) => new(field);

    private static CandidateProfileFieldIdentity MartialField(short type) => new(
        CandidateProfileField.BaseMartialQualification,
        Martial(type));

    private static CandidateProfileFieldIdentity LifeField(short type) => new(
        CandidateProfileField.BaseLifeSkillQualification,
        new CandidateDisciplineIdentity(CandidateDisciplineDomain.LifeSkill, type));

    private static CandidateProfileFieldIdentity AttributeField(
        CandidateMainAttribute attribute) => new(
        CandidateProfileField.BaseMainAttribute,
        attribute);

    private static CandidateDisciplineIdentity Martial(short type) => new(
        CandidateDisciplineDomain.Martial,
        type);

    private sealed class MissingSavePathProvider : ITaiwuSaveFilePathProvider
    {
        public TaiwuSaveFilePathResult Resolve() => new(
            SaveFilePath: null,
            "Trusted save configuration is unavailable.");
    }
}
