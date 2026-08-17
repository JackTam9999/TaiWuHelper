using System.Collections.Immutable;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class RawCompanionCandidate
{
    public RawCompanionCandidate(
        int characterId,
        bool characterPresent,
        bool? domainGroupMembership,
        bool? characterGroupMembership,
        bool? livingState,
        short? currentAge,
        int? locationArea,
        int? locationBlock,
        IEnumerable<int>? featureIdentities,
        IEnumerable<short>? baseMartialQualifications,
        IEnumerable<int>? learnedMartialSkillIdentities,
        IEnumerable<int>? equippedMartialSkillIdentities,
        IEnumerable<short>? baseLifeSkillQualifications,
        IEnumerable<int>? learnedLifeSkillIdentities,
        string? failureIdentity = null)
    {
        CharacterId = characterId;
        CharacterPresent = characterPresent;
        DomainGroupMembership = domainGroupMembership;
        CharacterGroupMembership = characterGroupMembership;
        LivingState = livingState;
        CurrentAge = currentAge;
        LocationArea = locationArea;
        LocationBlock = locationBlock;
        FeatureIdentities = Copy(featureIdentities);
        BaseMartialQualifications = Copy(baseMartialQualifications);
        LearnedMartialSkillIdentities = Copy(learnedMartialSkillIdentities);
        EquippedMartialSkillIdentities = Copy(equippedMartialSkillIdentities);
        BaseLifeSkillQualifications = Copy(baseLifeSkillQualifications);
        LearnedLifeSkillIdentities = Copy(learnedLifeSkillIdentities);
        FailureIdentity = string.IsNullOrWhiteSpace(failureIdentity)
            ? null
            : failureIdentity.Trim();
    }

    public int CharacterId { get; }

    public bool CharacterPresent { get; }

    public bool? DomainGroupMembership { get; }

    public bool? CharacterGroupMembership { get; }

    public bool? LivingState { get; }

    public short? CurrentAge { get; }

    public int? LocationArea { get; }

    public int? LocationBlock { get; }

    public ImmutableArray<int>? FeatureIdentities { get; }

    public ImmutableArray<short>? BaseMartialQualifications { get; }

    public ImmutableArray<int>? LearnedMartialSkillIdentities { get; }

    public ImmutableArray<int>? EquippedMartialSkillIdentities { get; }

    public ImmutableArray<short>? BaseLifeSkillQualifications { get; }

    public ImmutableArray<int>? LearnedLifeSkillIdentities { get; }

    public string? FailureIdentity { get; }

    private static ImmutableArray<T>? Copy<T>(IEnumerable<T>? values) =>
        values is null ? null : values.ToImmutableArray();
}

internal sealed record CompanionCandidateProfileMappingResult(
    CandidateProfile Profile,
    bool IsPartial,
    ImmutableArray<CompanionCandidateSnapshotDiagnostic> Diagnostics);

internal static class CompanionCandidateSnapshotMapping
{
    internal const string ProfileMappingVersion = "1";
    internal const string DisciplineCatalogVersion = "1";
    internal const string FingerprintSchemaVersion = "1";
    internal const int MartialDisciplineCount = 14;
    internal const int LifeSkillDisciplineCount = 16;

    public static CompanionCandidateProfileMappingResult Map(
        RawCompanionCandidate raw,
        CandidateProfileSourceVersions versions)
    {
        ArgumentNullException.ThrowIfNull(raw);
        ArgumentNullException.ThrowIfNull(versions);
        var identity = new CandidateIdentity(raw.CharacterId);
        var saveProvenance = new CandidateFactProvenance(
            CandidateEvidenceSourceKind.ConfiguredSave,
            "TAIWU_CONFIGURED_SAVE",
            ProfileMappingVersion,
            versions.SaveSha256);
        var gameDataProvenance = new CandidateFactProvenance(
            CandidateEvidenceSourceKind.InstalledGameData,
            "INSTALLED_GAMEDATA",
            versions.GameDataVersion,
            versions.GameDataVersion);
        var facts = new List<CandidateProfileFact>();
        var diagnostics = new List<CompanionCandidateSnapshotDiagnostic>();
        var profileDiagnostics = new List<CandidateProfileDiagnostic>();
        var partial = raw.FailureIdentity is not null || !raw.CharacterPresent;

        AddConfirmed(
            facts,
            Field(CandidateProfileField.RosterMembership),
            CandidateFactValue.Boolean(true),
            saveProvenance);
        AddNullableBoolean(
            facts,
            CandidateProfileField.DomainGroupMembership,
            raw.DomainGroupMembership,
            "DOMAIN_GROUP_MEMBERSHIP_UNAVAILABLE",
            saveProvenance,
            ref partial);
        AddNullableBoolean(
            facts,
            CandidateProfileField.CharacterGroupMembership,
            raw.CharacterGroupMembership,
            "CHARACTER_GROUP_MEMBERSHIP_UNAVAILABLE",
            saveProvenance,
            ref partial);
        AddNullableBoolean(
            facts,
            CandidateProfileField.LivingState,
            raw.LivingState,
            "LIVING_STATE_UNAVAILABLE",
            saveProvenance,
            ref partial);
        AddNullableInt16(
            facts,
            CandidateProfileField.CurrentAge,
            raw.CurrentAge,
            "CURRENT_AGE_UNAVAILABLE",
            saveProvenance,
            ref partial);
        AddLocation(
            facts,
            CandidateProfileField.CurrentLocationArea,
            raw.LocationArea,
            saveProvenance,
            ref partial);
        AddLocation(
            facts,
            CandidateProfileField.CurrentLocationBlock,
            raw.LocationBlock,
            saveProvenance,
            ref partial);
        AddIdentitySet(
            facts,
            CandidateProfileField.FeatureIdentities,
            raw.FeatureIdentities,
            saveProvenance,
            ref partial);
        AddIdentitySet(
            facts,
            CandidateProfileField.LearnedMartialSkillIdentities,
            raw.LearnedMartialSkillIdentities,
            saveProvenance,
            ref partial);
        AddIdentitySet(
            facts,
            CandidateProfileField.EquippedMartialSkillIdentities,
            raw.EquippedMartialSkillIdentities,
            saveProvenance,
            ref partial);
        AddIdentitySet(
            facts,
            CandidateProfileField.LearnedLifeSkillIdentities,
            raw.LearnedLifeSkillIdentities,
            saveProvenance,
            ref partial);

        AddDisciplineFacts(
            facts,
            CandidateDisciplineDomain.Martial,
            CandidateProfileField.BaseMartialQualification,
            CandidateProfileField.CurrentMartialQualification,
            CandidateProfileField.CurrentMartialAttainment,
            raw.BaseMartialQualifications,
            MartialDisciplineCount,
            saveProvenance,
            gameDataProvenance,
            ref partial);
        AddDisciplineFacts(
            facts,
            CandidateDisciplineDomain.LifeSkill,
            CandidateProfileField.BaseLifeSkillQualification,
            CandidateProfileField.CurrentLifeSkillQualification,
            CandidateProfileField.CurrentLifeSkillAttainment,
            raw.BaseLifeSkillQualifications,
            LifeSkillDisciplineCount,
            saveProvenance,
            gameDataProvenance,
            ref partial);

        if (raw.FailureIdentity is not null)
        {
            profileDiagnostics.Add(new CandidateProfileDiagnostic(
                raw.FailureIdentity,
                CandidateProfileDiagnosticSeverity.Error,
                "One candidate could not be projected completely.",
                field: null,
                []));
            diagnostics.Add(new CompanionCandidateSnapshotDiagnostic(
                raw.FailureIdentity,
                CompanionCandidateSnapshotDiagnosticSeverity.Error,
                "A candidate was retained with incomplete saved evidence.",
                identity));
        }

        var universeState = ResolveUniverseState(raw);
        var profile = new CandidateProfile(
            identity,
            universeState,
            versions,
            facts,
            profileDiagnostics);
        return new CompanionCandidateProfileMappingResult(
            profile,
            partial,
            [.. diagnostics]);
    }

    private static CandidateUniverseState ResolveUniverseState(
        RawCompanionCandidate raw)
    {
        if (!raw.CharacterPresent
            || raw.DomainGroupMembership is null
            || raw.CharacterGroupMembership is null
            || raw.LivingState is null
            || raw.FailureIdentity is not null)
        {
            return CandidateUniverseState.Incomplete;
        }

        if (!raw.DomainGroupMembership.Value
            || !raw.CharacterGroupMembership.Value)
        {
            return CandidateUniverseState.Conflicting;
        }

        return raw.LivingState.Value
            ? CandidateUniverseState.Eligible
            : CandidateUniverseState.Ineligible;
    }

    private static void AddDisciplineFacts(
        List<CandidateProfileFact> facts,
        CandidateDisciplineDomain domain,
        CandidateProfileField baseField,
        CandidateProfileField currentField,
        CandidateProfileField attainmentField,
        ImmutableArray<short>? baseValues,
        int expectedCount,
        CandidateFactProvenance saveProvenance,
        CandidateFactProvenance gameDataProvenance,
        ref bool partial)
    {
        if (baseValues is null || baseValues.Value.Length != expectedCount)
        {
            partial = true;
        }

        for (short type = 0; type < expectedCount; type++)
        {
            var discipline = new CandidateDisciplineIdentity(domain, type);
            var baseIdentity = new CandidateProfileFieldIdentity(baseField, discipline);
            if (baseValues is { } available && type < available.Length)
            {
                AddConfirmed(
                    facts,
                    baseIdentity,
                    CandidateFactValue.Int16(available[type]),
                    saveProvenance);
            }
            else
            {
                AddIncomplete(
                    facts,
                    baseIdentity,
                    "BASE_QUALIFICATION_UNAVAILABLE",
                    saveProvenance);
            }

            AddUnsupported(
                facts,
                new CandidateProfileFieldIdentity(currentField, discipline),
                "STANDALONE_MODIFIED_VALUE_UNAVAILABLE",
                gameDataProvenance);
            AddUnsupported(
                facts,
                new CandidateProfileFieldIdentity(attainmentField, discipline),
                "STANDALONE_MODIFIED_VALUE_UNAVAILABLE",
                gameDataProvenance);
        }
    }

    private static void AddNullableBoolean(
        List<CandidateProfileFact> facts,
        CandidateProfileField field,
        bool? value,
        string reason,
        CandidateFactProvenance provenance,
        ref bool partial)
    {
        if (value.HasValue)
        {
            AddConfirmed(facts, Field(field), CandidateFactValue.Boolean(value.Value), provenance);
        }
        else
        {
            partial = true;
            AddIncomplete(facts, Field(field), reason, provenance);
        }
    }

    private static void AddNullableInt16(
        List<CandidateProfileFact> facts,
        CandidateProfileField field,
        short? value,
        string reason,
        CandidateFactProvenance provenance,
        ref bool partial)
    {
        if (value.HasValue)
        {
            AddConfirmed(facts, Field(field), CandidateFactValue.Int16(value.Value), provenance);
        }
        else
        {
            partial = true;
            AddIncomplete(facts, Field(field), reason, provenance);
        }
    }

    private static void AddLocation(
        List<CandidateProfileFact> facts,
        CandidateProfileField field,
        int? value,
        CandidateFactProvenance provenance,
        ref bool partial)
    {
        if (value >= 0)
        {
            AddConfirmed(facts, Field(field), CandidateFactValue.Int32(value.Value), provenance);
        }
        else
        {
            partial = true;
            AddIncomplete(facts, Field(field), "CURRENT_LOCATION_UNAVAILABLE", provenance);
        }
    }

    private static void AddIdentitySet(
        List<CandidateProfileFact> facts,
        CandidateProfileField field,
        ImmutableArray<int>? values,
        CandidateFactProvenance provenance,
        ref bool partial)
    {
        if (values is null)
        {
            partial = true;
            AddIncomplete(facts, Field(field), "IDENTITY_SET_UNAVAILABLE", provenance);
            return;
        }

        try
        {
            AddConfirmed(
                facts,
                Field(field),
                CandidateFactValue.Int32Set(values.Value),
                provenance);
        }
        catch (ArgumentException)
        {
            partial = true;
            AddIncomplete(facts, Field(field), "IDENTITY_SET_INVALID", provenance);
        }
    }

    private static void AddConfirmed(
        List<CandidateProfileFact> facts,
        CandidateProfileFieldIdentity field,
        CandidateFactValue value,
        CandidateFactProvenance provenance) =>
        facts.Add(CandidateProfileFact.Confirmed(
            field,
            value,
            provenance,
            [Evidence(field, provenance)]));

    private static void AddIncomplete(
        List<CandidateProfileFact> facts,
        CandidateProfileFieldIdentity field,
        string reason,
        CandidateFactProvenance provenance) =>
        facts.Add(CandidateProfileFact.Incomplete(
            field,
            new CandidateUnavailableReason(
                reason,
                "The configured save did not provide a complete value."),
            [Evidence(field, provenance)]));

    private static void AddUnsupported(
        List<CandidateProfileFact> facts,
        CandidateProfileFieldIdentity field,
        string reason,
        CandidateFactProvenance provenance) =>
        facts.Add(CandidateProfileFact.Unsupported(
            field,
            new CandidateUnavailableReason(
                reason,
                "The standalone runtime cannot evaluate this modified value."),
            [Evidence(field, provenance)]));

    private static CandidateEvidenceReference Evidence(
        CandidateProfileFieldIdentity field,
        CandidateFactProvenance provenance)
    {
        var discipline = field.Discipline is null
            ? "NONE"
            : $"{(int)field.Discipline.Domain}_{field.Discipline.Type}";
        return new CandidateEvidenceReference(
            $"E6_FIELD_{(int)field.Field}_{discipline}",
            provenance);
    }

    private static CandidateProfileFieldIdentity Field(CandidateProfileField field) =>
        new(field);
}
