namespace TaiWu.Domain.CompanionRoles;

public sealed class CompanionRoleDefinitionResolution
{
    internal CompanionRoleDefinitionResolution(
        CompanionRoleDefinitionResolutionState state,
        CompanionRoleDefinition? definition,
        string diagnosticIdentity)
    {
        State = state;
        Definition = definition;
        DiagnosticIdentity = diagnosticIdentity;
    }

    public CompanionRoleDefinitionResolutionState State { get; }

    public CompanionRoleDefinition? Definition { get; }

    public string DiagnosticIdentity { get; }
}
