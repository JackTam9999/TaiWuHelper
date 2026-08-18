using System.Collections.Immutable;
using System.Text;

namespace TaiWu.Domain.VillageWorkforce;

public sealed record WorkforceSupportedSourceVersion
{
    public WorkforceSupportedSourceVersion(
        string gameDataVersion,
        string mappingVersion,
        string candidateUniverseVersion,
        string fingerprintSchemaVersion)
    {
        GameDataVersion = WorkforceText.Version(
            gameDataVersion,
            nameof(gameDataVersion));
        MappingVersion = WorkforceText.Version(
            mappingVersion,
            nameof(mappingVersion));
        CandidateUniverseVersion = WorkforceText.Version(
            candidateUniverseVersion,
            nameof(candidateUniverseVersion));
        FingerprintSchemaVersion = WorkforceText.Version(
            fingerprintSchemaVersion,
            nameof(fingerprintSchemaVersion));
    }

    public string GameDataVersion { get; }

    public string MappingVersion { get; }

    public string CandidateUniverseVersion { get; }

    public string FingerprintSchemaVersion { get; }

    internal string StableKey => string.Join('|',
        GameDataVersion,
        MappingVersion,
        CandidateUniverseVersion,
        FingerprintSchemaVersion);
}

public sealed record WorkforceRequirementDefinition
{
    public WorkforceRequirementDefinition(
        int order,
        WorkforceRequirementKind requirement,
        WorkforceEvidenceRequirementKind evidenceRequirement,
        WorkforceFactIdentity? sourceFact)
    {
        if (order <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(order),
                order,
                "A workforce requirement order must be positive.");
        }

        WorkforceText.Defined(requirement, nameof(requirement));
        WorkforceText.Defined(
            evidenceRequirement,
            nameof(evidenceRequirement));
        Order = order;
        Requirement = requirement;
        EvidenceRequirement = evidenceRequirement;
        SourceFact = sourceFact;
        ValidateShape();
    }

    public int Order { get; }

    public WorkforceRequirementKind Requirement { get; }

    public WorkforceEvidenceRequirementKind EvidenceRequirement { get; }

    public WorkforceFactIdentity? SourceFact { get; }

    internal string StableKey => string.Join('|',
        WorkforceText.Number(Order),
        WorkforceText.EnumKey(Requirement),
        WorkforceText.EnumKey(EvidenceRequirement),
        SourceFact?.StableKey ?? "NONE");

    private void ValidateShape()
    {
        var valid = (Requirement, EvidenceRequirement, SourceFact?.Kind) switch
        {
            (WorkforceRequirementKind.SupportedSourceVersion,
                WorkforceEvidenceRequirementKind.SourceVersions,
                null) => true,
            (WorkforceRequirementKind.SupportedShopTarget,
                WorkforceEvidenceRequirementKind.SupportedTarget,
                null) => true,
            (WorkforceRequirementKind.AlternativeWorkCandidate,
                WorkforceEvidenceRequirementKind.ConfirmedFact,
                WorkforceFactKind.CandidateUniverseMembership) => true,
            (WorkforceRequirementKind.CharacterProfileAvailable,
                WorkforceEvidenceRequirementKind.ConfirmedFact,
                WorkforceFactKind.BaseLifeSkillQualification) => true,
            (WorkforceRequirementKind.QualificationProvenanceMatch,
                WorkforceEvidenceRequirementKind.MatchingProvenance,
                WorkforceFactKind.BaseLifeSkillQualification) => true,
            _ => false
        };

        if (!valid)
        {
            throw new ArgumentException(
                "The requirement identity, evidence kind, and source fact do not form a supported workforce gate.",
                nameof(SourceFact));
        }
    }
}

public sealed record WorkforceComponentDefinition
{
    public WorkforceComponentDefinition(
        WorkforceComponentIdentity identity,
        WorkforceFactIdentity sourceFact,
        WorkforceNormalizationKind normalization,
        WorkforceUnit unit,
        WorkforceScoreDirection direction,
        decimal weight,
        string explanationIdentity)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        SourceFact = sourceFact
            ?? throw new ArgumentNullException(nameof(sourceFact));
        WorkforceText.Defined(normalization, nameof(normalization));
        WorkforceText.Defined(unit, nameof(unit));
        WorkforceText.Defined(direction, nameof(direction));

        if (sourceFact.Kind
                != WorkforceFactKind.BaseLifeSkillQualification
            || sourceFact.Discipline != identity.Discipline)
        {
            throw new ArgumentException(
                "The version-1 component source must be the target discipline's exact base life-skill qualification.",
                nameof(sourceFact));
        }

        if (normalization != WorkforceNormalizationKind.Identity)
        {
            throw new ArgumentException(
                "The version-1 component uses identity normalization.",
                nameof(normalization));
        }

        if (unit != WorkforceUnit.BaseQualificationPoint)
        {
            throw new ArgumentException(
                "A base qualification source requires the base-qualification-point unit.",
                nameof(unit));
        }

        if (direction != WorkforceScoreDirection.HigherIsBetter)
        {
            throw new ArgumentException(
                "The version-1 component orders higher exact qualification first.",
                nameof(direction));
        }

        if (weight != 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weight),
                weight,
                "The version-1 component weight must be exactly one.");
        }

        Normalization = normalization;
        Unit = unit;
        Direction = direction;
        Weight = weight;
        ExplanationIdentity = WorkforceText.Stable(
            explanationIdentity,
            nameof(explanationIdentity));
    }

    public WorkforceComponentIdentity Identity { get; }

    public WorkforceFactIdentity SourceFact { get; }

    public WorkforceNormalizationKind Normalization { get; }

    public WorkforceUnit Unit { get; }

    public WorkforceScoreDirection Direction { get; }

    public decimal Weight { get; }

    public string ExplanationIdentity { get; }

    internal string StableKey => string.Join('|',
        Identity.StableKey,
        SourceFact.StableKey,
        WorkforceText.EnumKey(Normalization),
        WorkforceText.EnumKey(Unit),
        WorkforceText.EnumKey(Direction),
        WorkforceText.Number(Weight),
        ExplanationIdentity);
}

public sealed class WorkforceRuleDefinition
{
    public WorkforceRuleDefinition(
        WorkforceRuleIdentity identity,
        WorkforceRuleVersion version,
        WorkforceObjectiveIdentity objective,
        WorkforceSupportedSourceVersion supportedSource,
        WorkforceTargetKind targetKind,
        IEnumerable<WorkforceRequirementDefinition> requirements,
        IEnumerable<WorkforceComponentDefinition> components,
        IEnumerable<WorkforceRuleLimitation> limitations)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Objective = objective ?? throw new ArgumentNullException(nameof(objective));
        SupportedSource = supportedSource
            ?? throw new ArgumentNullException(nameof(supportedSource));
        WorkforceText.Defined(targetKind, nameof(targetKind));
        TargetKind = targetKind;
        Requirements = CopyRequirements(requirements);
        Components = CopyComponents(components);
        Limitations = CopyLimitations(limitations);
        ValidateSourceUse();
        Fingerprint = CreateFingerprint();
    }

    public WorkforceRuleIdentity Identity { get; }

    public WorkforceRuleVersion Version { get; }

    public WorkforceObjectiveIdentity Objective { get; }

    public WorkforceSupportedSourceVersion SupportedSource { get; }

    public WorkforceTargetKind TargetKind { get; }

    public ImmutableArray<WorkforceRequirementDefinition> Requirements
    {
        get;
    }

    public ImmutableArray<WorkforceComponentDefinition> Components { get; }

    public ImmutableArray<WorkforceRuleLimitation> Limitations { get; }

    public string Fingerprint { get; }

    private static ImmutableArray<WorkforceRequirementDefinition>
        CopyRequirements(IEnumerable<WorkforceRequirementDefinition> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copied = values.ToImmutableArray();
        if (copied.Any(item => item is null))
        {
            throw new ArgumentException(
                "Rule requirements cannot contain null entries.",
                nameof(values));
        }

        if (copied.GroupBy(item => item.Requirement).Any(group => group.Count() > 1)
            || copied.GroupBy(item => item.Order).Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Rule requirement identities and orders must be unique.",
                nameof(values));
        }

        var expected = Enum.GetValues<WorkforceRequirementKind>();
        if (copied.Length != expected.Length
            || expected.Any(requirement =>
                copied.All(item => item.Requirement != requirement)))
        {
            throw new ArgumentException(
                "A version-1 rule must define every approved hard requirement exactly once.",
                nameof(values));
        }

        return [.. copied.OrderBy(item => item.Order)];
    }

    private static ImmutableArray<WorkforceComponentDefinition>
        CopyComponents(IEnumerable<WorkforceComponentDefinition> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copied = values.ToImmutableArray();
        if (copied.Any(item => item is null))
        {
            throw new ArgumentException(
                "Rule components cannot contain null entries.",
                nameof(values));
        }

        if (copied.GroupBy(item => item.Identity.StableKey, StringComparer.Ordinal)
                .Any(group => group.Count() > 1)
            || copied.GroupBy(item => item.SourceFact.StableKey, StringComparer.Ordinal)
                .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Rule component identities and source fields must be unique.",
                nameof(values));
        }

        if (copied.Length != 1)
        {
            throw new ArgumentException(
                "The version-1 rule requires exactly one numeric component.",
                nameof(values));
        }

        return [.. copied.OrderBy(
            item => item.Identity.StableKey,
            StringComparer.Ordinal)];
    }

    private static ImmutableArray<WorkforceRuleLimitation> CopyLimitations(
        IEnumerable<WorkforceRuleLimitation> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copied = values.ToImmutableArray();
        if (copied.IsEmpty || copied.Any(item => item is null))
        {
            throw new ArgumentException(
                "A rule requires at least one non-null limitation.",
                nameof(values));
        }

        if (copied.GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Rule limitation identities must be unique.",
                nameof(values));
        }

        return [.. copied.OrderBy(
            item => item.StableKey,
            StringComparer.Ordinal)];
    }

    private void ValidateSourceUse()
    {
        var component = Components[0];
        var profileRequirement = Requirements.Single(item =>
            item.Requirement
                == WorkforceRequirementKind.CharacterProfileAvailable);
        var provenanceRequirement = Requirements.Single(item =>
            item.Requirement
                == WorkforceRequirementKind.QualificationProvenanceMatch);
        if (profileRequirement.SourceFact != component.SourceFact
            || provenanceRequirement.SourceFact != component.SourceFact)
        {
            throw new ArgumentException(
                "Profile, provenance, and component rules must reference the same exact source fact.",
                nameof(Requirements));
        }
    }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("VILLAGE_WORKFORCE_RULE_V1\n")
            .Append(Identity.StableKey).Append('|')
            .Append(Version.StableKey).Append('|')
            .Append(Objective.StableKey).Append('|')
            .Append(SupportedSource.StableKey).Append('|')
            .Append(WorkforceText.EnumKey(TargetKind)).Append('\n');
        foreach (var requirement in Requirements)
        {
            canonical.Append("REQUIREMENT|")
                .Append(requirement.StableKey).Append('\n');
        }

        foreach (var component in Components)
        {
            canonical.Append("COMPONENT|")
                .Append(component.StableKey).Append('\n');
        }

        foreach (var limitation in Limitations)
        {
            canonical.Append("LIMITATION|")
                .Append(limitation.StableKey).Append('\n');
        }

        return WorkforceText.Fingerprint(canonical.ToString());
    }
}

public sealed record WorkforceRuleResolution
{
    internal WorkforceRuleResolution(
        WorkforceRuleResolutionStatus status,
        WorkforceRuleDefinition? rule,
        string reasonIdentity)
    {
        WorkforceText.Defined(status, nameof(status));
        if ((status == WorkforceRuleResolutionStatus.Resolved) != (rule is not null))
        {
            throw new ArgumentException(
                "Only a resolved workforce rule result may contain a rule.",
                nameof(rule));
        }

        Status = status;
        Rule = rule;
        ReasonIdentity = WorkforceText.Stable(
            reasonIdentity,
            nameof(reasonIdentity));
    }

    public WorkforceRuleResolutionStatus Status { get; }

    public WorkforceRuleDefinition? Rule { get; }

    public string ReasonIdentity { get; }

    public bool IsResolved => Status == WorkforceRuleResolutionStatus.Resolved;
}

public static class VerifiedVillageWorkforceRules
{
    public const string SupportedGameDataVersion =
        "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20";

    public const string ObjectiveVersion = "1";
    public const string RuleVersion = "1.0.0";
    public const string MappingVersion = "1";
    public const string CandidateUniverseVersion = "1";
    public const string FingerprintSchemaVersion = "1";

    public static WorkforceRuleResolution Resolve(
        WorkforceObjectiveIdentity objective,
        WorkforceSourceVersions sourceVersions,
        WorkforceTargetKind targetKind,
        LifeSkillDisciplineIdentity requiredDiscipline)
    {
        ArgumentNullException.ThrowIfNull(objective);
        ArgumentNullException.ThrowIfNull(sourceVersions);
        ArgumentNullException.ThrowIfNull(requiredDiscipline);

        if (objective.Kind
                != WorkforceObjectiveKind.ShopManagerBaseLifeSkillQualification
            || !string.Equals(
                objective.Version,
                ObjectiveVersion,
                StringComparison.Ordinal))
        {
            return Unsupported(
                WorkforceRuleResolutionStatus.UnsupportedObjectiveVersion,
                "WORKFORCE_OBJECTIVE_VERSION_UNSUPPORTED");
        }

        if (!Enum.IsDefined(targetKind)
            || targetKind != WorkforceTargetKind.ShopManagerSlot)
        {
            return Unsupported(
                WorkforceRuleResolutionStatus.UnsupportedTargetKind,
                "WORKFORCE_TARGET_KIND_UNSUPPORTED");
        }

        if (!string.Equals(
                sourceVersions.GameDataVersion,
                SupportedGameDataVersion,
                StringComparison.Ordinal))
        {
            return Unsupported(
                WorkforceRuleResolutionStatus.UnsupportedGameDataVersion,
                "WORKFORCE_GAME_DATA_VERSION_UNSUPPORTED");
        }

        if (!string.Equals(
                sourceVersions.MappingVersion,
                MappingVersion,
                StringComparison.Ordinal))
        {
            return Unsupported(
                WorkforceRuleResolutionStatus.UnsupportedMappingVersion,
                "WORKFORCE_MAPPING_VERSION_UNSUPPORTED");
        }

        if (!string.Equals(
                sourceVersions.CandidateUniverseVersion,
                CandidateUniverseVersion,
                StringComparison.Ordinal))
        {
            return Unsupported(
                WorkforceRuleResolutionStatus
                    .UnsupportedCandidateUniverseVersion,
                "WORKFORCE_CANDIDATE_UNIVERSE_VERSION_UNSUPPORTED");
        }

        if (!string.Equals(
                sourceVersions.FingerprintSchemaVersion,
                FingerprintSchemaVersion,
                StringComparison.Ordinal))
        {
            return Unsupported(
                WorkforceRuleResolutionStatus
                    .UnsupportedFingerprintSchemaVersion,
                "WORKFORCE_FINGERPRINT_SCHEMA_VERSION_UNSUPPORTED");
        }

        return new WorkforceRuleResolution(
            WorkforceRuleResolutionStatus.Resolved,
            Create(requiredDiscipline),
            "WORKFORCE_RULE_RESOLVED");
    }

    private static WorkforceRuleDefinition Create(
        LifeSkillDisciplineIdentity requiredDiscipline)
    {
        var candidateFact = new WorkforceFactIdentity(
            WorkforceFactKind.CandidateUniverseMembership);
        var qualificationFact = new WorkforceFactIdentity(
            WorkforceFactKind.BaseLifeSkillQualification,
            requiredDiscipline);
        return new WorkforceRuleDefinition(
            new WorkforceRuleIdentity(
                "SHOP_MANAGER_REQUIRED_BASE_LIFE_SKILL_QUALIFICATION"),
            new WorkforceRuleVersion(RuleVersion),
            new WorkforceObjectiveIdentity(
                WorkforceObjectiveKind.ShopManagerBaseLifeSkillQualification,
                ObjectiveVersion),
            new WorkforceSupportedSourceVersion(
                SupportedGameDataVersion,
                MappingVersion,
                CandidateUniverseVersion,
                FingerprintSchemaVersion),
            WorkforceTargetKind.ShopManagerSlot,
            [
                new WorkforceRequirementDefinition(
                    1,
                    WorkforceRequirementKind.SupportedSourceVersion,
                    WorkforceEvidenceRequirementKind.SourceVersions,
                    sourceFact: null),
                new WorkforceRequirementDefinition(
                    2,
                    WorkforceRequirementKind.SupportedShopTarget,
                    WorkforceEvidenceRequirementKind.SupportedTarget,
                    sourceFact: null),
                new WorkforceRequirementDefinition(
                    3,
                    WorkforceRequirementKind.AlternativeWorkCandidate,
                    WorkforceEvidenceRequirementKind.ConfirmedFact,
                    candidateFact),
                new WorkforceRequirementDefinition(
                    4,
                    WorkforceRequirementKind.CharacterProfileAvailable,
                    WorkforceEvidenceRequirementKind.ConfirmedFact,
                    qualificationFact),
                new WorkforceRequirementDefinition(
                    5,
                    WorkforceRequirementKind.QualificationProvenanceMatch,
                    WorkforceEvidenceRequirementKind.MatchingProvenance,
                    qualificationFact)
            ],
            [new WorkforceComponentDefinition(
                new WorkforceComponentIdentity(
                    WorkforceComponentKind.RequiredBaseLifeSkillQualification,
                    requiredDiscipline),
                qualificationFact,
                WorkforceNormalizationKind.Identity,
                WorkforceUnit.BaseQualificationPoint,
                WorkforceScoreDirection.HigherIsBetter,
                weight: 1m,
                explanationIdentity:
                    "REQUIRED_BASE_LIFE_SKILL_QUALIFICATION_EXACT_VALUE")],
            [
                new WorkforceRuleLimitation(
                    "SAVED_BASE_QUALIFICATION_ONLY"),
                new WorkforceRuleLimitation(
                    "NO_EFFICIENCY_OUTPUT_OR_REVENUE"),
                new WorkforceRuleLimitation(
                    "OCCUPIED_SHOP_REPLACEMENT_ONLY")
            ]);
    }

    private static WorkforceRuleResolution Unsupported(
        WorkforceRuleResolutionStatus status,
        string reasonIdentity) =>
        new(status, rule: null, reasonIdentity);
}
