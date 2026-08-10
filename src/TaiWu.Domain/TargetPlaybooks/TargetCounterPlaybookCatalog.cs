using System.Collections.Immutable;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybooks;

public sealed class TargetCounterPlaybookCatalog
{
    private readonly ImmutableDictionary<string, TargetCounterPlaybook>
        _playbooksByArchetype;

    public TargetCounterPlaybookCatalog(
        TargetProfileVersion gameDataVersion,
        IEnumerable<TargetArchetypeDefinition> archetypes,
        IEnumerable<CombatCounterRuleSet> verifiedCounterRuleSets,
        IEnumerable<TargetCounterPlaybook> playbooks)
    {
        GameDataVersion = gameDataVersion
            ?? throw new ArgumentNullException(nameof(gameDataVersion));
        Archetypes = CopyArchetypes(archetypes);
        VerifiedCounterRules = CopyVerifiedRules(
            verifiedCounterRuleSets,
            GameDataVersion);
        Playbooks = CopyPlaybooks(playbooks);

        var archetypesByKey = Archetypes.ToDictionary(
            archetype => archetype.StableKey,
            StringComparer.Ordinal);
        foreach (var playbook in Playbooks)
        {
            if (!archetypesByKey.ContainsKey(
                    playbook.Identity.Archetype.StableKey))
            {
                throw new ArgumentException(
                    $"Playbook {playbook.StableKey} does not reference an "
                    + "archetype in this catalogue.",
                    nameof(playbooks));
            }
        }

        var playbookArchetypes = Playbooks
            .Select(playbook => playbook.Identity.Archetype.StableKey)
            .ToHashSet(StringComparer.Ordinal);
        var missingPlaybook = Archetypes.FirstOrDefault(archetype =>
            !playbookArchetypes.Contains(archetype.StableKey));
        if (missingPlaybook is not null)
        {
            throw new ArgumentException(
                $"Archetype {missingPlaybook.StableKey} has no playbook.",
                nameof(playbooks));
        }

        var verifiedRulesByCode = VerifiedCounterRules.ToDictionary(
            rule => rule.Code,
            StringComparer.Ordinal);
        var unverifiedOption = Playbooks
            .SelectMany(playbook => playbook.Goals)
            .SelectMany(goal => goal.Options)
            .FirstOrDefault(option =>
                !verifiedRulesByCode.TryGetValue(
                    option.Code,
                    out var verifiedRule)
                || !ReferenceEquals(verifiedRule, option.CounterRule));
        if (unverifiedOption is not null)
        {
            throw new ArgumentException(
                $"Playbook option {unverifiedOption.Code} is not the exact "
                + "typed rule registered by this catalogue.",
                nameof(playbooks));
        }

        _playbooksByArchetype = Playbooks.ToImmutableDictionary(
            playbook => playbook.Identity.Archetype.StableKey,
            StringComparer.Ordinal);
    }

    public TargetProfileVersion GameDataVersion { get; }

    public ImmutableArray<TargetArchetypeDefinition> Archetypes { get; }

    public ImmutableArray<CombatCounterRule> VerifiedCounterRules { get; }

    public ImmutableArray<TargetCounterPlaybook> Playbooks { get; }

    public TargetCounterPlaybookResolution Resolve(
        string observedGameDataVersion,
        TargetArchetypeIdentity archetype)
    {
        var observedVersion = new TargetProfileVersion(
            observedGameDataVersion);
        ArgumentNullException.ThrowIfNull(archetype);
        if (!string.Equals(
                GameDataVersion.Value,
                observedVersion.Value,
                StringComparison.Ordinal))
        {
            return new TargetCounterPlaybookResolution(
                observedVersion,
                archetype,
                TargetCounterPlaybookResolutionStatus
                    .UnsupportedGameDataVersion,
                playbook: null);
        }

        if (!_playbooksByArchetype.TryGetValue(
                archetype.StableKey,
                out var playbook))
        {
            return new TargetCounterPlaybookResolution(
                observedVersion,
                archetype,
                TargetCounterPlaybookResolutionStatus.ArchetypeNotFound,
                playbook: null);
        }

        return new TargetCounterPlaybookResolution(
            observedVersion,
            archetype,
            TargetCounterPlaybookResolutionStatus.Resolved,
            playbook);
    }

    private static ImmutableArray<TargetArchetypeDefinition> CopyArchetypes(
        IEnumerable<TargetArchetypeDefinition> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var archetypes = values.ToImmutableArray();
        if (archetypes.Length == 0)
        {
            throw new ArgumentException(
                "A playbook catalogue requires archetypes.",
                nameof(values));
        }

        if (archetypes.Any(archetype => archetype is null))
        {
            throw new ArgumentException(
                "Catalogue archetypes cannot contain null entries.",
                nameof(values));
        }

        if (archetypes.DistinctBy(
                archetype => archetype.StableKey,
                StringComparer.Ordinal).Count() != archetypes.Length)
        {
            throw new ArgumentException(
                "Catalogue archetypes must be unique.",
                nameof(values));
        }

        return [.. archetypes.OrderBy(
            archetype => archetype.StableKey,
            StringComparer.Ordinal)];
    }

    private static ImmutableArray<CombatCounterRule> CopyVerifiedRules(
        IEnumerable<CombatCounterRuleSet> values,
        TargetProfileVersion gameDataVersion)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ruleSets = values.ToImmutableArray();
        if (ruleSets.Any(ruleSet => ruleSet is null))
        {
            throw new ArgumentException(
                "Verified counter-rule sets cannot contain null entries.",
                nameof(values));
        }

        var mismatched = ruleSets.FirstOrDefault(ruleSet =>
            !string.Equals(
                ruleSet.GameDataVersion,
                gameDataVersion.Value,
                StringComparison.Ordinal));
        if (mismatched is not null)
        {
            throw new ArgumentException(
                "Every verified counter-rule set must use the catalogue's "
                + "exact GameData version.",
                nameof(values));
        }

        var rules = ruleSets
            .SelectMany(ruleSet => ruleSet.Rules)
            .ToImmutableArray();
        if (rules.DistinctBy(rule => rule.Code, StringComparer.Ordinal).Count()
            != rules.Length)
        {
            throw new ArgumentException(
                "Verified counter-rule codes must be unique across sets.",
                nameof(values));
        }

        return [.. rules.OrderBy(rule => rule.Code, StringComparer.Ordinal)];
    }

    private static ImmutableArray<TargetCounterPlaybook> CopyPlaybooks(
        IEnumerable<TargetCounterPlaybook> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var playbooks = values.ToImmutableArray();
        if (playbooks.Length == 0)
        {
            throw new ArgumentException(
                "A playbook catalogue requires playbooks.",
                nameof(values));
        }

        if (playbooks.Any(playbook => playbook is null))
        {
            throw new ArgumentException(
                "Catalogue playbooks cannot contain null entries.",
                nameof(values));
        }

        if (playbooks.DistinctBy(
                playbook => playbook.Identity.Archetype.StableKey,
                StringComparer.Ordinal).Count() != playbooks.Length)
        {
            throw new ArgumentException(
                "An archetype can have only one playbook in a catalogue.",
                nameof(values));
        }

        return [.. playbooks.OrderBy(
            playbook => playbook.StableKey,
            StringComparer.Ordinal)];
    }

}
