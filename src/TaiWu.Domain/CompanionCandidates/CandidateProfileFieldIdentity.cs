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
        CandidateDisciplineIdentity? discipline = null)
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

        Field = field;
        Discipline = discipline;
    }

    public CandidateProfileField Field { get; }

    public CandidateDisciplineIdentity? Discipline { get; }

    internal string StableKey => Discipline is null
        ? CandidateProfileText.EnumKey(Field)
        : $"{CandidateProfileText.EnumKey(Field)}:{Discipline.StableKey}";

    internal CandidateFactValueKind ExpectedValueKind => Field switch
    {
        CandidateProfileField.RosterMembership
            or CandidateProfileField.DomainGroupMembership
            or CandidateProfileField.CharacterGroupMembership
            or CandidateProfileField.LivingState => CandidateFactValueKind.Boolean,
        CandidateProfileField.CurrentAge
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
            _ => null
        };
}
