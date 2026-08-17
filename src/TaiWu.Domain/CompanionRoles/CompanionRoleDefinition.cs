using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Domain.CompanionRoles;

public sealed class CompanionRoleDefinition
{
    public CompanionRoleDefinition(
        CompanionRoleIdentity identity,
        string roleVersion,
        string evaluationRuleVersion,
        IEnumerable<string> supportedGameDataVersions,
        string supportedProfileMappingVersion,
        string supportedFingerprintSchemaVersion,
        CandidateDisciplineDomain disciplineDomain,
        short minimumDisciplineType,
        short maximumDisciplineType,
        IEnumerable<CompanionRoleScoreDimension> scoreDimensions,
        CompanionRoleTiePolicy tiePolicy)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        RoleVersion = CompanionRoleText.Stable(roleVersion, nameof(roleVersion));
        EvaluationRuleVersion = CompanionRoleText.Stable(
            evaluationRuleVersion,
            nameof(evaluationRuleVersion));
        SupportedProfileMappingVersion = CompanionRoleText.Stable(
            supportedProfileMappingVersion,
            nameof(supportedProfileMappingVersion));
        SupportedFingerprintSchemaVersion = CompanionRoleText.Stable(
            supportedFingerprintSchemaVersion,
            nameof(supportedFingerprintSchemaVersion));
        if (!Enum.IsDefined(disciplineDomain))
        {
            throw new ArgumentOutOfRangeException(
                nameof(disciplineDomain),
                disciplineDomain,
                "Unknown discipline domain.");
        }

        if (minimumDisciplineType < 0 || maximumDisciplineType < minimumDisciplineType)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumDisciplineType),
                "A discipline range must be non-negative and ordered.");
        }

        if (!Enum.IsDefined(tiePolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(tiePolicy), tiePolicy, "Unknown role tie policy.");
        }

        ArgumentNullException.ThrowIfNull(supportedGameDataVersions);
        var versions = supportedGameDataVersions
            .Select(version => CompanionRoleText.Stable(version, nameof(supportedGameDataVersions)))
            .ToImmutableArray();
        if (versions.IsEmpty)
        {
            throw new ArgumentException("A role must support at least one GameData version.", nameof(supportedGameDataVersions));
        }

        if (versions.Distinct(StringComparer.Ordinal).Count() != versions.Length)
        {
            throw new ArgumentException("Supported GameData versions cannot contain duplicates.", nameof(supportedGameDataVersions));
        }

        ArgumentNullException.ThrowIfNull(scoreDimensions);
        var dimensions = scoreDimensions.ToImmutableArray();
        if (dimensions.IsEmpty || dimensions.Any(item => item is null))
        {
            throw new ArgumentException("A role requires at least one non-null score dimension.", nameof(scoreDimensions));
        }

        if (dimensions.GroupBy(item => item.Identity, StringComparer.Ordinal).Any(group => group.Count() > 1)
            || dimensions.GroupBy(item => item.Field).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Role score dimensions require unique identities and fields.", nameof(scoreDimensions));
        }

        if (dimensions.Any(item => !FieldMatchesDomain(item.Field, disciplineDomain)))
        {
            throw new ArgumentException("Every score field must be a typed base-qualification field in the role discipline domain.", nameof(scoreDimensions));
        }

        DisciplineDomain = disciplineDomain;
        MinimumDisciplineType = minimumDisciplineType;
        MaximumDisciplineType = maximumDisciplineType;
        TiePolicy = tiePolicy;
        SupportedGameDataVersions = [.. versions.Order(StringComparer.Ordinal)];
        ScoreDimensions = [.. dimensions.OrderBy(item => item.Identity, StringComparer.Ordinal)];
        HardRequirements = CreateRequirements(ScoreDimensions);
        Fingerprint = CreateFingerprint();
    }

    public CompanionRoleIdentity Identity { get; }

    public string RoleVersion { get; }

    public string EvaluationRuleVersion { get; }

    public ImmutableArray<string> SupportedGameDataVersions { get; }

    public string SupportedProfileMappingVersion { get; }

    public string SupportedFingerprintSchemaVersion { get; }

    public CandidateDisciplineDomain DisciplineDomain { get; }

    public short MinimumDisciplineType { get; }

    public short MaximumDisciplineType { get; }

    public ImmutableArray<CompanionRoleHardRequirement> HardRequirements { get; }

    public ImmutableArray<CompanionRoleScoreDimension> ScoreDimensions { get; }

    public CompanionRoleTiePolicy TiePolicy { get; }

    public string Fingerprint { get; }

    private static bool FieldMatchesDomain(
        CandidateProfileField field,
        CandidateDisciplineDomain domain) =>
        (field, domain) switch
        {
            (CandidateProfileField.BaseMartialQualification, CandidateDisciplineDomain.Martial) => true,
            (CandidateProfileField.BaseLifeSkillQualification, CandidateDisciplineDomain.LifeSkill) => true,
            _ => false
        };

    private static ImmutableArray<CompanionRoleHardRequirement> CreateRequirements(
        ImmutableArray<CompanionRoleScoreDimension> dimensions)
    {
        var requirements = new List<CompanionRoleHardRequirement>
        {
            new(1, CompanionRoleRequirementKind.CandidateUniverseEligible, "CANDIDATE_UNIVERSE_ELIGIBLE", null),
            new(2, CompanionRoleRequirementKind.SourceVersionsSupported, "SOURCE_VERSIONS_SUPPORTED", null),
            new(3, CompanionRoleRequirementKind.DisciplineSupported, "DISCIPLINE_SUPPORTED", null)
        };
        var order = 4;
        foreach (var dimension in dimensions)
        {
            requirements.Add(new CompanionRoleHardRequirement(
                order++,
                CompanionRoleRequirementKind.RequiredFactConfirmed,
                $"{dimension.Identity}_CONFIRMED",
                dimension.Field));
            requirements.Add(new CompanionRoleHardRequirement(
                order++,
                CompanionRoleRequirementKind.FactProvenanceCompatible,
                $"{dimension.Identity}_PROVENANCE_COMPATIBLE",
                dimension.Field));
        }

        return [.. requirements];
    }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("COMPANION_ROLE_DEFINITION_V1\n")
            .Append(Identity.Value).Append('|').Append(RoleVersion).Append('|')
            .Append(EvaluationRuleVersion).Append('\n')
            .Append(SupportedProfileMappingVersion).Append('|')
            .Append(SupportedFingerprintSchemaVersion).Append('|')
            .Append(CompanionRoleText.EnumKey(DisciplineDomain)).Append('|')
            .Append(MinimumDisciplineType).Append('|').Append(MaximumDisciplineType)
            .Append('|').Append(CompanionRoleText.EnumKey(TiePolicy)).Append('\n');
        foreach (var version in SupportedGameDataVersions)
        {
            canonical.Append("GAME_DATA|").Append(version).Append('\n');
        }

        foreach (var requirement in HardRequirements)
        {
            canonical.Append("REQUIREMENT|").Append(requirement.StableKey).Append('\n');
        }

        foreach (var dimension in ScoreDimensions)
        {
            canonical.Append("DIMENSION|").Append(dimension.StableKey).Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}
