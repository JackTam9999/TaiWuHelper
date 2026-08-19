namespace TaiWu.Domain.CompanionCandidates;

public sealed record CandidateDisciplineIdentity
{
    public CandidateDisciplineIdentity(
        CandidateDisciplineDomain domain,
        short type)
    {
        if (!Enum.IsDefined(domain))
        {
            throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown discipline domain.");
        }

        if (type < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "A discipline type cannot be negative.");
        }

        Domain = domain;
        Type = type;
    }

    public CandidateDisciplineDomain Domain { get; }

    public short Type { get; }

    internal string StableKey => $"{CandidateProfileText.EnumKey(Domain)}:{Type.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
}

public sealed record CandidateProfileFieldIdentity
{
    public CandidateProfileFieldIdentity(
        CandidateProfileField field,
        CandidateDisciplineIdentity? discipline = null) :
        this(field, discipline, mainAttribute: null)
    {
    }

    public CandidateProfileFieldIdentity(
        CandidateProfileField field,
        CandidateMainAttribute mainAttribute) :
        this(field, discipline: null, mainAttribute)
    {
    }

    private CandidateProfileFieldIdentity(
        CandidateProfileField field,
        CandidateDisciplineIdentity? discipline,
        CandidateMainAttribute? mainAttribute)
    {
        if (!Enum.IsDefined(field))
        {
            throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown candidate-profile field.");
        }

        var expectedDomain = ExpectedDisciplineDomain(field);
        if (expectedDomain is null && discipline is not null)
        {
            throw new ArgumentException(
                "This candidate-profile field cannot have a discipline identity.",
                nameof(discipline));
        }

        if (expectedDomain is not null
            && (discipline is null || discipline.Domain != expectedDomain))
        {
            throw new ArgumentException(
                $"This candidate-profile field requires a {expectedDomain} discipline identity.",
                nameof(discipline));
        }

        var expectsMainAttribute = field
            == CandidateProfileField.BaseMainAttribute;
        if (expectsMainAttribute != mainAttribute.HasValue)
        {
            throw new ArgumentException(
                expectsMainAttribute
                    ? "This candidate-profile field requires a main-attribute identity."
                    : "This candidate-profile field cannot have a main-attribute identity.",
                nameof(mainAttribute));
        }

        if (mainAttribute.HasValue
            && !Enum.IsDefined(mainAttribute.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(mainAttribute),
                mainAttribute,
                "Unknown candidate main attribute.");
        }

        Field = field;
        Discipline = discipline;
        MainAttribute = mainAttribute;
    }

    public CandidateProfileField Field { get; }

    public CandidateDisciplineIdentity? Discipline { get; }

    public CandidateMainAttribute? MainAttribute { get; }

    public static CandidateProfileFieldIdentity ForRole(
        CandidateProfileField field,
        CandidateDisciplineIdentity discipline)
    {
        ArgumentNullException.ThrowIfNull(discipline);
        return ExpectedDisciplineDomain(field) is null
            ? new CandidateProfileFieldIdentity(field)
            : new CandidateProfileFieldIdentity(field, discipline);
    }

    internal string StableKey => Discipline is not null
        ? $"{CandidateProfileText.EnumKey(Field)}:{Discipline.StableKey}"
        : MainAttribute.HasValue
            ? $"{CandidateProfileText.EnumKey(Field)}:{CandidateProfileText.EnumKey(MainAttribute.Value)}"
            : CandidateProfileText.EnumKey(Field);

    internal CandidateFactValueKind ExpectedValueKind => Field switch
    {
        CandidateProfileField.RosterMembership
            or CandidateProfileField.DomainGroupMembership
            or CandidateProfileField.CharacterGroupMembership
            or CandidateProfileField.LivingState
            or CandidateProfileField.VillageWorkCandidateMembership =>
                CandidateFactValueKind.Boolean,
        CandidateProfileField.CurrentAge
            or CandidateProfileField.BaseMainAttribute
            or CandidateProfileField.CapabilityBreadthIndex
            or CandidateProfileField.BaseMartialQualification
            or CandidateProfileField.CurrentMartialQualification
            or CandidateProfileField.CurrentMartialAttainment
            or CandidateProfileField.BaseLifeSkillQualification
            or CandidateProfileField.CurrentLifeSkillQualification
            or CandidateProfileField.CurrentLifeSkillAttainment => CandidateFactValueKind.Int16,
        CandidateProfileField.CurrentLocationArea
            or CandidateProfileField.CurrentLocationBlock => CandidateFactValueKind.Int32,
        CandidateProfileField.FeatureIdentities
            or CandidateProfileField.LearnedMartialSkillIdentities
            or CandidateProfileField.EquippedMartialSkillIdentities
            or CandidateProfileField.LearnedLifeSkillIdentities => CandidateFactValueKind.Int32Set,
        _ => throw new InvalidOperationException("Unknown candidate-profile field.")
    };

    private static CandidateDisciplineDomain? ExpectedDisciplineDomain(
        CandidateProfileField field) =>
        field switch
        {
            CandidateProfileField.BaseMartialQualification
                or CandidateProfileField.CurrentMartialQualification
                or CandidateProfileField.CurrentMartialAttainment => CandidateDisciplineDomain.Martial,
            CandidateProfileField.BaseLifeSkillQualification
                or CandidateProfileField.CurrentLifeSkillQualification
                or CandidateProfileField.CurrentLifeSkillAttainment => CandidateDisciplineDomain.LifeSkill,
            CandidateProfileField.CapabilityBreadthIndex =>
                CandidateDisciplineDomain.Capability,
            _ => null
        };
}
