namespace TaiWu.Domain.CombatThreats;

public sealed record TargetThreatWarning
{
    internal TargetThreatWarning(
        string code,
        string message,
        UnknownTargetMechanic mechanic)
    {
        Code = code;
        Message = message;
        Mechanic = mechanic;
    }

    public string Code { get; }

    public string Message { get; }

    public UnknownTargetMechanic Mechanic { get; }
}
