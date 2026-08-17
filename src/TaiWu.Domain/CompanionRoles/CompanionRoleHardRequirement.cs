using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Domain.CompanionRoles;

public sealed record CompanionRoleHardRequirement
{
    internal CompanionRoleHardRequirement(
        int order,
        CompanionRoleRequirementKind kind,
        string identity,
        CandidateProfileField? field)
    {
        Order = order;
        Kind = kind;
        Identity = identity;
        Field = field;
    }

    public int Order { get; }

    public CompanionRoleRequirementKind Kind { get; }

    public string Identity { get; }

    public CandidateProfileField? Field { get; }

    internal string StableKey => string.Join('|',
        Order.ToString(System.Globalization.CultureInfo.InvariantCulture),
        CompanionRoleText.EnumKey(Kind),
        Identity,
        Field is null ? "NONE" : CompanionRoleText.EnumKey(Field.Value));
}
