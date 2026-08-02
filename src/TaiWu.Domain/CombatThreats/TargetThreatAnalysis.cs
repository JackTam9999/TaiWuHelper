using System.Collections.Immutable;

namespace TaiWu.Domain.CombatThreats;

public sealed record TargetThreatAnalysis
{
    internal TargetThreatAnalysis(
        IEnumerable<AnalyzedTargetThreat> threats,
        IEnumerable<TargetThreatWarning> warnings)
    {
        Threats = [.. threats];
        Warnings = [.. warnings];
    }

    public ImmutableArray<AnalyzedTargetThreat> Threats { get; }

    public ImmutableArray<TargetThreatWarning> Warnings { get; }
}
