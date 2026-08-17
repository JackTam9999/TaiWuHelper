using System.Collections.Immutable;
using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Domain.CompanionRoles;

public static class VerifiedCompanionRoleDefinitions
{
    public const string SupportedGameDataVersion =
        "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20";

    public const string RoleVersion = "1";
    public const string EvaluationRuleVersion = "1";
    public const string ProfileMappingVersion = "2";
    public const string FingerprintSchemaVersion = "2";

    public static CompanionRoleDefinition MartialDisciplineAptitude { get; } =
        Create(
            "MARTIAL_DISCIPLINE_APTITUDE",
            CandidateDisciplineDomain.Martial,
            maximumDisciplineType: 13,
            CandidateProfileField.BaseMartialQualification,
            "BASE_MARTIAL_QUALIFICATION");

    public static CompanionRoleDefinition LifeSkillDisciplineAptitude { get; } =
        Create(
            "LIFE_SKILL_DISCIPLINE_APTITUDE",
            CandidateDisciplineDomain.LifeSkill,
            maximumDisciplineType: 15,
            CandidateProfileField.BaseLifeSkillQualification,
            "BASE_LIFE_SKILL_QUALIFICATION");

    public static ImmutableArray<CompanionRoleDefinition> All { get; } =
        [MartialDisciplineAptitude, LifeSkillDisciplineAptitude];

    public static CompanionRoleDefinitionResolution Resolve(
        CompanionRoleIdentity identity,
        string roleVersion)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var normalizedVersion = CompanionRoleText.Stable(roleVersion, nameof(roleVersion));
        var identityMatches = All
            .Where(item => item.Identity == identity)
            .ToArray();
        if (identityMatches.Length == 0)
        {
            return new CompanionRoleDefinitionResolution(
                CompanionRoleDefinitionResolutionState.UnknownIdentity,
                definition: null,
                "ROLE_IDENTITY_UNKNOWN");
        }

        var versionMatch = identityMatches.SingleOrDefault(item =>
            string.Equals(item.RoleVersion, normalizedVersion, StringComparison.Ordinal));
        return versionMatch is null
            ? new CompanionRoleDefinitionResolution(
                CompanionRoleDefinitionResolutionState.UnsupportedVersion,
                definition: null,
                "ROLE_VERSION_UNSUPPORTED")
            : new CompanionRoleDefinitionResolution(
                CompanionRoleDefinitionResolutionState.Supported,
                versionMatch,
                "ROLE_DEFINITION_SUPPORTED");
    }

    private static CompanionRoleDefinition Create(
        string identity,
        CandidateDisciplineDomain domain,
        short maximumDisciplineType,
        CandidateProfileField field,
        string dimensionIdentity) => new(
            new CompanionRoleIdentity(identity),
            RoleVersion,
            EvaluationRuleVersion,
            [SupportedGameDataVersion],
            ProfileMappingVersion,
            FingerprintSchemaVersion,
            domain,
            minimumDisciplineType: 0,
            maximumDisciplineType,
            [new CompanionRoleScoreDimension(
                dimensionIdentity,
                field,
                "BASE_QUALIFICATION_POINT",
                CompanionRoleScoreDirection.HigherIsBetter,
                CompanionRoleNormalizationKind.Identity,
                short.MinValue,
                short.MaxValue,
                weight: 1m,
                CompanionRoleMissingEvidenceBehavior.EvaluationIncomplete,
                $"{dimensionIdentity}_EXACT_SAVED_VALUE")],
            CompanionRoleTiePolicy.ExactTotalRemainsTie);
}
