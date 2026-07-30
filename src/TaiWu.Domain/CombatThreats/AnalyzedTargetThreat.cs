using System.Collections.Immutable;

namespace TaiWu.Domain.CombatThreats;

public sealed record AnalyzedTargetThreat
{
    internal AnalyzedTargetThreat(
        TargetThreat threat,
        IEnumerable<TargetThreatSource> sources)
    {
        Threat = threat;
        Sources = [.. sources];
    }

    public TargetThreat Threat { get; }

    public ImmutableArray<TargetThreatSource> Sources { get; }
}
