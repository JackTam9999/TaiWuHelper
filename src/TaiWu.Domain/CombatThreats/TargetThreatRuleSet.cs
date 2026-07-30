using System.Collections.Immutable;

namespace TaiWu.Domain.CombatThreats;

public sealed class TargetThreatRuleSet
{
    public TargetThreatRuleSet(
        string gameDataVersion,
        IEnumerable<int> relevantSkillIds,
        IEnumerable<TargetThreatRule> rules,
        IEnumerable<UnknownTargetMechanic>? unresolvedMechanics = null)
    {
        if (string.IsNullOrWhiteSpace(gameDataVersion))
        {
            throw new ArgumentException(
                "GameData version cannot be blank.",
                nameof(gameDataVersion));
        }

        ArgumentNullException.ThrowIfNull(relevantSkillIds);
        ArgumentNullException.ThrowIfNull(rules);
        var relevantIds = relevantSkillIds.ToArray();
        if (relevantIds.Any(skillId => skillId < 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(relevantSkillIds),
                "Relevant skill IDs cannot be negative.");
        }

        if (relevantIds.Distinct().Count() != relevantIds.Length)
        {
            throw new ArgumentException(
                "Relevant skill IDs cannot be duplicated.",
                nameof(relevantSkillIds));
        }

        Rules = [.. rules];
        if (Rules.Any(rule => rule is null))
        {
            throw new ArgumentException(
                "Target-threat rules cannot contain null entries.",
                nameof(rules));
        }

        var duplicateThreat = Rules
            .GroupBy(rule => rule.Threat.Code, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateThreat is not null)
        {
            throw new ArgumentException(
                $"Duplicate target-threat rule "
                + $"'{duplicateThreat.Key}'.",
                nameof(rules));
        }

        RelevantSkillIds = relevantIds.ToImmutableHashSet();
        if (Rules
            .SelectMany(rule => rule.Signatures)
            .Any(signature =>
                !RelevantSkillIds.Contains(signature.SkillId)))
        {
            throw new ArgumentException(
                "Every rule signature must reference a relevant skill.",
                nameof(rules));
        }

        var unresolved = unresolvedMechanics?.ToImmutableArray()
            ?? [];
        if (unresolved.Any(mechanic => mechanic is null))
        {
            throw new ArgumentException(
                "Unresolved mechanics cannot contain null entries.",
                nameof(unresolvedMechanics));
        }

        GameDataVersion = gameDataVersion.Trim();
        UnresolvedMechanics = unresolved;
    }

    public string GameDataVersion { get; }

    public ImmutableHashSet<int> RelevantSkillIds { get; }

    public ImmutableArray<TargetThreatRule> Rules { get; }

    public ImmutableArray<UnknownTargetMechanic> UnresolvedMechanics { get; }
}
