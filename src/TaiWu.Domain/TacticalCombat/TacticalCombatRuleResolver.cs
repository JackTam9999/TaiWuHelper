using System.Collections.Immutable;

namespace TaiWu.Domain.TacticalCombat;

public sealed record TacticalTransitionRuleMatch
{
    internal TacticalTransitionRuleMatch(
        TacticalTransitionRule rule,
        TacticalRuleApplicability applicability,
        ImmutableArray<TacticalRuleEvidenceIdentity> unmetEvidence)
    {
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        Applicability = TacticalCombatText.Defined(
            applicability,
            nameof(applicability));
        UnmetEvidence = unmetEvidence;
    }

    public TacticalTransitionRule Rule { get; }

    public TacticalRuleApplicability Applicability { get; }

    public ImmutableArray<TacticalRuleEvidenceIdentity> UnmetEvidence { get; }
}

public sealed record TacticalSkillRoleRuleMatch
{
    internal TacticalSkillRoleRuleMatch(
        TacticalSkillRoleRule rule,
        TacticalRuleApplicability applicability,
        ImmutableArray<TacticalRuleEvidenceIdentity> unmetEvidence)
    {
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        Applicability = TacticalCombatText.Defined(
            applicability,
            nameof(applicability));
        UnmetEvidence = unmetEvidence;
    }

    public TacticalSkillRoleRule Rule { get; }

    public TacticalRuleApplicability Applicability { get; }

    public ImmutableArray<TacticalRuleEvidenceIdentity> UnmetEvidence { get; }
}

public sealed class TacticalCombatRuleResolution
{
    internal TacticalCombatRuleResolution(
        TacticalRuleSetResolutionStatus status,
        IEnumerable<TacticalTransitionRuleMatch> transitions,
        IEnumerable<TacticalSkillRoleRuleMatch> roles)
    {
        Status = TacticalCombatText.Defined(status, nameof(status));
        Transitions = [.. transitions.OrderBy(
            item => item.Rule.Identity.Code,
            StringComparer.Ordinal)];
        Roles = [.. roles.OrderBy(
            item => item.Rule.Identity.Code,
            StringComparer.Ordinal)];
        if (Status == TacticalRuleSetResolutionStatus.UnsupportedGameDataVersion
            && (!Transitions.IsEmpty || !Roles.IsEmpty))
        {
            throw new ArgumentException(
                "An unsupported tactical rule version cannot expose stale matches.");
        }
    }

    public TacticalRuleSetResolutionStatus Status { get; }

    public ImmutableArray<TacticalTransitionRuleMatch> Transitions { get; }

    public ImmutableArray<TacticalSkillRoleRuleMatch> Roles { get; }

    public bool IsResolved =>
        Status == TacticalRuleSetResolutionStatus.Resolved;
}

internal static class TacticalCombatRuleResolver
{
    internal static TacticalCombatRuleResolution Resolve(
        TacticalCombatRuleSet rules,
        string gameDataVersion,
        IEnumerable<string> exactTargetGoalCodes,
        IEnumerable<TacticalRuleEvidenceObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var version = TacticalCombatText.Stable(
            gameDataVersion,
            nameof(gameDataVersion));
        if (!rules.SupportedGameDataVersions.Contains(
                version,
                StringComparer.Ordinal))
        {
            return new TacticalCombatRuleResolution(
                TacticalRuleSetResolutionStatus.UnsupportedGameDataVersion,
                [],
                []);
        }

        var goals = TacticalRuleCollections.Goals(
            exactTargetGoalCodes,
            nameof(exactTargetGoalCodes));
        var supportedGoals = rules.SupportedTargetGoalCodes.ToHashSet(
            StringComparer.Ordinal);
        if (goals.Any(goal => !supportedGoals.Contains(goal)))
        {
            throw new ArgumentException(
                "Tactical rule resolution received an unknown target goal.",
                nameof(exactTargetGoalCodes));
        }

        var evidence = TacticalCombatText.CopyUnique(
            observations,
            item => item.StableKey,
            "rule evidence observation",
            nameof(observations));
        var expectedRuleVersion =
            $"TACTICAL_COMBAT_RULES@{rules.SemanticVersion}";
        if (evidence.Any(item => !string.Equals(
                    item.Evidence.GameDataVersion,
                    version,
                    StringComparison.Ordinal)
                || !string.Equals(
                    item.Evidence.RuleVersion,
                    expectedRuleVersion,
                    StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "Rule evidence observations must match the requested GameData and tactical rule versions.",
                nameof(observations));
        }

        var requestedGoals = goals.ToHashSet(StringComparer.Ordinal);
        var transitionMatches = rules.Transitions
            .Where(rule => rule.TargetGoalCodes.Any(requestedGoals.Contains))
            .Select(rule => Match(rule, evidence))
            .ToArray();
        var transitionStates = transitionMatches.ToDictionary(
            item => item.Rule.Identity.StableKey,
            StringComparer.Ordinal);
        var roleMatches = rules.Roles
            .Where(rule => rule.TargetGoalCodes.Any(requestedGoals.Contains))
            .Select(rule => Match(rule, evidence, transitionStates))
            .ToArray();

        return new TacticalCombatRuleResolution(
            TacticalRuleSetResolutionStatus.Resolved,
            transitionMatches,
            roleMatches);
    }

    private static TacticalTransitionRuleMatch Match(
        TacticalTransitionRule rule,
        ImmutableArray<TacticalRuleEvidenceObservation> observations)
    {
        var evaluation = Evaluate(rule.EvidenceRequirements, observations);
        return new TacticalTransitionRuleMatch(
            rule,
            evaluation.Applicability,
            evaluation.UnmetEvidence);
    }

    private static TacticalSkillRoleRuleMatch Match(
        TacticalSkillRoleRule rule,
        ImmutableArray<TacticalRuleEvidenceObservation> observations,
        IReadOnlyDictionary<string, TacticalTransitionRuleMatch> transitions)
    {
        var own = Evaluate(rule.EvidenceRequirements, observations);
        var dependencies = rule.Transitions
            .Select(item => transitions[item.StableKey])
            .ToArray();
        var applicability = Combine(
            own.Applicability,
            dependencies.Select(item => item.Applicability));
        var unmet = own.UnmetEvidence
            .Concat(dependencies.SelectMany(item => item.UnmetEvidence))
            .DistinctBy(item => item.StableKey, StringComparer.Ordinal)
            .OrderBy(item => item.StableKey, StringComparer.Ordinal)
            .ToImmutableArray();
        return new TacticalSkillRoleRuleMatch(rule, applicability, unmet);
    }

    private static EvidenceEvaluation Evaluate(
        ImmutableArray<TacticalRuleEvidenceRequirement> requirements,
        ImmutableArray<TacticalRuleEvidenceObservation> observations)
    {
        List<TacticalRuleEvidenceIdentity> unmet = [];
        var applicability = TacticalRuleApplicability.Applicable;
        foreach (var requirement in requirements)
        {
            var sameIdentity = observations
                .Where(item => item.Identity == requirement.Identity)
                .ToArray();
            var exactContrary = sameIdentity.Any(item =>
                item.Scope == TacticalRuleEvidenceScope.ExactTarget
                && item.Disposition
                    == TacticalRuleEvidenceDisposition.Contrary);
            if (exactContrary)
            {
                applicability = Combine(
                    applicability,
                    [TacticalRuleApplicability.Contrary]);
                unmet.Add(requirement.Identity);
                continue;
            }

            var exactConflict = sameIdentity.Any(item =>
                item.Scope == TacticalRuleEvidenceScope.ExactTarget
                && item.Disposition
                    == TacticalRuleEvidenceDisposition.Conflicting);
            if (exactConflict)
            {
                applicability = Combine(
                    applicability,
                    [TacticalRuleApplicability.Conflicting]);
                unmet.Add(requirement.Identity);
                continue;
            }

            var applicable = sameIdentity.Where(item =>
                item.Scope == requirement.Scope
                && item.Source == requirement.Source).ToArray();
            if (applicable.Any(item => item.Disposition
                == TacticalRuleEvidenceDisposition.Conflicting))
            {
                applicability = Combine(
                    applicability,
                    [TacticalRuleApplicability.Conflicting]);
                unmet.Add(requirement.Identity);
            }
            else if (!applicable.Any(item => item.Disposition
                == requirement.RequiredDisposition))
            {
                applicability = Combine(
                    applicability,
                    [TacticalRuleApplicability.Incomplete]);
                unmet.Add(requirement.Identity);
            }
        }

        return new EvidenceEvaluation(
            applicability,
            [.. unmet
                .DistinctBy(item => item.StableKey, StringComparer.Ordinal)
                .OrderBy(item => item.StableKey, StringComparer.Ordinal)]);
    }

    private static TacticalRuleApplicability Combine(
        TacticalRuleApplicability initial,
        IEnumerable<TacticalRuleApplicability> values)
    {
        return values.Append(initial).MinBy(Priority);
    }

    private static int Priority(TacticalRuleApplicability value) =>
        value switch
        {
            TacticalRuleApplicability.Contrary => 0,
            TacticalRuleApplicability.Conflicting => 1,
            TacticalRuleApplicability.Incomplete => 2,
            TacticalRuleApplicability.Applicable => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };

    private sealed record EvidenceEvaluation(
        TacticalRuleApplicability Applicability,
        ImmutableArray<TacticalRuleEvidenceIdentity> UnmetEvidence);
}
