namespace TaiWu.Domain.CompanionRoles;

public sealed record CompanionRoleIdentity
{
    public CompanionRoleIdentity(string value)
    {
        Value = CompanionRoleText.Stable(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}
