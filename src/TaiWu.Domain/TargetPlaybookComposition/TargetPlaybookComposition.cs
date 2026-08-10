using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public sealed class TargetPlaybookComposition
{
    internal TargetPlaybookComposition(
        string profileFingerprint,
        string matchSetKey,
        IEnumerable<TargetCounterPlaybook> sourcePlaybooks,
        IEnumerable<ComposedTargetResponseGoal> goals,
        IEnumerable<TargetPlaybookCompositionConflict> conflicts,
        IEnumerable<TargetPlaybookCompositionDiagnostic> diagnostics)
    {
        ProfileFingerprint = TargetProfileText.Fingerprint(
            profileFingerprint,
            nameof(profileFingerprint));
        MatchSetKey = TargetProfileText.Fingerprint(
            matchSetKey,
            nameof(matchSetKey));
        SourcePlaybooks =
        [
            .. sourcePlaybooks
                .DistinctBy(
                    playbook => playbook.StableKey,
                    StringComparer.Ordinal)
                .OrderBy(
                    playbook => playbook.StableKey,
                    StringComparer.Ordinal)
        ];
        Goals =
        [
            .. goals
                .OrderBy(goal => goal.Sequence)
                .ThenBy(goal => goal.Priority)
                .ThenBy(goal => goal.Code, StringComparer.Ordinal)
        ];
        Options =
        [
            .. Goals
                .SelectMany(goal => goal.Options)
                .GroupBy(option => option.StableKey, StringComparer.Ordinal)
                .Select(group => new ComposedTargetCounterOption(
                    group.First().CounterRule,
                    group.SelectMany(option => option.SourcePlaybookKeys),
                    group.SelectMany(option => option.SourceGoalCodes),
                    group.SelectMany(option => option.ConflictGroups)))
                .OrderByDescending(option => option.Strength)
                .ThenBy(option => TargetPlaybookComposer.TimingOrder(
                    option.ActivationTiming))
                .ThenBy(option => option.StableKey, StringComparer.Ordinal)
        ];
        Threats =
        [
            .. Goals
                .SelectMany(goal => goal.Threats)
                .DistinctBy(threat => threat.Code, StringComparer.Ordinal)
                .OrderBy(threat => threat.Code, StringComparer.Ordinal)
        ];
        KnownGaps =
        [
            .. Goals
                .SelectMany(goal => goal.KnownGaps)
                .DistinctBy(gap => gap.StableKey, StringComparer.Ordinal)
                .OrderBy(gap => gap.StableKey, StringComparer.Ordinal)
        ];
        Conflicts =
        [
            .. conflicts
                .DistinctBy(
                    conflict => conflict.StableKey,
                    StringComparer.Ordinal)
                .OrderBy(
                    conflict => conflict.StableKey,
                    StringComparer.Ordinal)
        ];
        Diagnostics =
        [
            .. diagnostics
                .DistinctBy(
                    diagnostic => diagnostic.StableKey,
                    StringComparer.Ordinal)
                .OrderBy(
                    diagnostic => diagnostic.StableKey,
                    StringComparer.Ordinal)
        ];
        StableKey = CreateStableKey();
    }

    public string ProfileFingerprint { get; }

    public string MatchSetKey { get; }

    public ImmutableArray<TargetCounterPlaybook> SourcePlaybooks { get; }

    public ImmutableArray<ComposedTargetResponseGoal> Goals { get; }

    public ImmutableArray<ComposedTargetCounterOption> Options { get; }

    public ImmutableArray<TargetThreat> Threats { get; }

    public ImmutableArray<TargetCounterPlaybookGap> KnownGaps { get; }

    public ImmutableArray<TargetPlaybookCompositionConflict> Conflicts { get; }

    public ImmutableArray<TargetPlaybookCompositionDiagnostic> Diagnostics
    { get; }

    public string StableKey { get; }

    private string CreateStableKey()
    {
        var canonical = TargetProfileText.Stable(
            "TARGET_PLAYBOOK_COMPOSITION_V1",
            ProfileFingerprint,
            MatchSetKey,
            TargetProfileText.StableCollection(
                SourcePlaybooks.Select(playbook => playbook.StableKey)),
            TargetProfileText.StableCollection(
                Goals.Select(goal => goal.ContentKey)),
            TargetProfileText.StableCollection(
                Options.Select(option => option.ContentKey)),
            TargetProfileText.StableCollection(
                Threats.Select(threat => threat.Code)),
            TargetProfileText.StableCollection(
                KnownGaps.Select(gap => gap.StableKey)),
            TargetProfileText.StableCollection(
                Conflicts.Select(conflict => conflict.StableKey)),
            TargetProfileText.StableCollection(
                Diagnostics.Select(diagnostic => diagnostic.StableKey)));
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
