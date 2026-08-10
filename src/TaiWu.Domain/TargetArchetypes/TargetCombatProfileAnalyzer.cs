using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetArchetypes;

public static class TargetCombatProfileAnalyzer
{
    public static TargetCombatProfileAnalysis Analyze(
        CombatSnapshot snapshot,
        TargetThreatRuleSet threatRules,
        TargetProfileExtractionRuleSet profileRules,
        IEnumerable<TargetArchetypeDefinition> archetypeDefinitions)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(threatRules);
        ArgumentNullException.ThrowIfNull(profileRules);
        ArgumentNullException.ThrowIfNull(archetypeDefinitions);

        var threats = TargetThreatAnalyzer.Analyze(snapshot, threatRules);
        var profile = TargetCombatProfileExtractor.Extract(
            snapshot,
            threats,
            profileRules);
        var matches = TargetArchetypeMatcher.Match(
            profile,
            archetypeDefinitions);
        return new TargetCombatProfileAnalysis(threats, profile, matches);
    }
}
