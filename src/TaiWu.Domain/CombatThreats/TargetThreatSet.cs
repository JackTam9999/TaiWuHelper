using System.Collections.Immutable;

namespace TaiWu.Domain.CombatThreats;

public sealed record TargetThreatSet
{
    internal TargetThreatSet(
        IEnumerable<TargetThreat> threats,
        IEnumerable<TargetThreatWarning> warnings)
    {
        Threats = [.. threats];
        Warnings = [.. warnings];
    }

    public ImmutableArray<TargetThreat> Threats { get; }

    public ImmutableArray<TargetThreatWarning> Warnings { get; }
}
