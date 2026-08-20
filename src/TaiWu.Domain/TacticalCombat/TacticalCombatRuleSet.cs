using System.Collections.Immutable;
using System.Text;

namespace TaiWu.Domain.TacticalCombat;

public sealed class TacticalCombatRuleSet
{
    public TacticalCombatRuleSet(
        TacticalSemanticVersion semanticVersion,
        IEnumerable<string> supportedGameDataVersions,
        IEnumerable<string> supportedTargetGoalCodes,
        IEnumerable<TacticalTransitionRule> transitions,
        IEnumerable<TacticalSkillRoleRule> roles)
    {
        SemanticVersion = semanticVersion
            ?? throw new ArgumentNullException(nameof(semanticVersion));
        SupportedGameDataVersions = TacticalRuleCollections.Versions(
            supportedGameDataVersions,
            nameof(supportedGameDataVersions));
        SupportedTargetGoalCodes = TacticalRuleCollections.Goals(
            supportedTargetGoalCodes,
            nameof(supportedTargetGoalCodes));
        Transitions = TacticalCombatText.CopyUnique(
            transitions,
            item => item.StableKey,
            "transition rule",
            nameof(transitions));
        Roles = TacticalCombatText.CopyUnique(
            roles,
            item => item.StableKey,
            "role rule",
            nameof(roles));
        if (Transitions.IsEmpty || Roles.IsEmpty)
        {
            throw new ArgumentException(
                "A tactical rule set requires transition and role rules.");
        }

        ValidateReferencesAndVersions();
        Fingerprint = CreateFingerprint();
    }

    public TacticalSemanticVersion SemanticVersion { get; }

    public ImmutableArray<string> SupportedGameDataVersions { get; }

    public ImmutableArray<string> SupportedTargetGoalCodes { get; }

    public ImmutableArray<TacticalTransitionRule> Transitions { get; }

    public ImmutableArray<TacticalSkillRoleRule> Roles { get; }

    public string Fingerprint { get; }

    public TacticalCombatRuleResolution Resolve(
        string gameDataVersion,
        IEnumerable<string> exactTargetGoalCodes,
        IEnumerable<TacticalRuleEvidenceObservation> observations) =>
        TacticalCombatRuleResolver.Resolve(
            this,
            gameDataVersion,
            exactTargetGoalCodes,
            observations);

    private void ValidateReferencesAndVersions()
    {
        var versions = SupportedGameDataVersions.ToHashSet(
            StringComparer.Ordinal);
        var goals = SupportedTargetGoalCodes.ToHashSet(StringComparer.Ordinal);
        var transitions = Transitions.ToDictionary(
            item => item.StableKey,
            StringComparer.Ordinal);
        foreach (var transition in Transitions)
        {
            if (transition.SemanticVersion != SemanticVersion
                || !transition.SupportedGameDataVersions.SequenceEqual(
                    SupportedGameDataVersions)
                || transition.TargetGoalCodes.Any(goal => !goals.Contains(goal)))
            {
                throw new ArgumentException(
                    "Transition rules must use the rule-set semantic version, exact source versions, and known target goals.",
                    nameof(Transitions));
            }
        }

        foreach (var role in Roles)
        {
            if (role.SemanticVersion != SemanticVersion
                || !role.SupportedGameDataVersions.SequenceEqual(
                    SupportedGameDataVersions)
                || role.TargetGoalCodes.Any(goal => !goals.Contains(goal)))
            {
                throw new ArgumentException(
                    "Role rules must use the rule-set semantic version, exact source versions, and known target goals.",
                    nameof(Roles));
            }

            foreach (var transition in role.Transitions)
            {
                if (!transitions.ContainsKey(transition.StableKey))
                {
                    throw new ArgumentException(
                        $"A tactical role references an unknown transition: {transition.Code}.",
                        nameof(Roles));
                }

                if (!transitions[transition.StableKey].TargetGoalCodes.Any(
                    goal => role.TargetGoalCodes.Contains(
                        goal,
                        StringComparer.Ordinal)))
                {
                    throw new ArgumentException(
                        $"A tactical role references transition {transition.Code} without a shared exact-target goal.",
                        nameof(Roles));
                }
            }

            if (!versions.Contains(role.Evidence[0].GameDataVersion))
            {
                throw new ArgumentException(
                    "A tactical role contains inconsistent source versions.",
                    nameof(Roles));
            }
        }
    }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("TACTICAL_COMBAT_RULE_SET_V1\n")
            .Append(SemanticVersion.StableKey).Append('\n')
            .AppendJoin('|', SupportedGameDataVersions).Append('\n')
            .AppendJoin('|', SupportedTargetGoalCodes).Append('\n');
        foreach (var transition in Transitions)
        {
            canonical.Append("TRANSITION|")
                .Append(transition.ContentKey).Append('\n');
        }

        foreach (var role in Roles)
        {
            canonical.Append("ROLE|").Append(role.ContentKey).Append('\n');
        }

        return TacticalCombatText.Fingerprint(canonical.ToString());
    }
}
