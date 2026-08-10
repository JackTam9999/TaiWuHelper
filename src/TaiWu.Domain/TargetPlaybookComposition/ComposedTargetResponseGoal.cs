using System.Collections.Immutable;
using System.Globalization;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public sealed class ComposedTargetResponseGoal
{
    internal ComposedTargetResponseGoal(
        string code,
        int sequence,
        TargetResponsePriority priority,
        CombatCounterActivationTiming responseTiming,
        IEnumerable<string> sourcePlaybookKeys,
        IEnumerable<TargetProfileFacetIdentity> profileFacets,
        IEnumerable<TargetThreat> threats,
        IEnumerable<ComposedTargetCounterOption> options,
        IEnumerable<string> conflictGroups,
        IEnumerable<string> evidenceReferences,
        IEnumerable<TargetCounterPlaybookGap> knownGaps)
    {
        Code = TargetProfileText.Code(code, nameof(code));
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        if (!Enum.IsDefined(responseTiming))
        {
            throw new ArgumentOutOfRangeException(nameof(responseTiming));
        }

        SourcePlaybookKeys = CopyReferences(
            sourcePlaybookKeys,
            nameof(sourcePlaybookKeys));
        ProfileFacets =
        [
            .. profileFacets
                .DistinctBy(facet => facet.StableKey, StringComparer.Ordinal)
                .OrderBy(facet => facet.StableKey, StringComparer.Ordinal)
        ];
        Threats =
        [
            .. threats
                .DistinctBy(threat => threat.Code, StringComparer.Ordinal)
                .OrderBy(threat => threat.Code, StringComparer.Ordinal)
        ];
        Options =
        [
            .. options
                .DistinctBy(option => option.StableKey, StringComparer.Ordinal)
                .OrderByDescending(option => option.Strength)
                .ThenBy(option => TargetPlaybookComposer.TimingOrder(
                    option.ActivationTiming))
                .ThenBy(option => option.StableKey, StringComparer.Ordinal)
        ];
        ConflictGroups = CopyCodes(
            conflictGroups,
            nameof(conflictGroups),
            requireValue: false);
        EvidenceReferences = CopyCodes(
            evidenceReferences,
            nameof(evidenceReferences),
            requireValue: true);
        KnownGaps =
        [
            .. knownGaps
                .DistinctBy(gap => gap.StableKey, StringComparer.Ordinal)
                .OrderBy(gap => gap.StableKey, StringComparer.Ordinal)
        ];
        Sequence = sequence;
        Priority = priority;
        ResponseTiming = responseTiming;
    }

    public string Code { get; }

    public string StableKey => Code;

    public int Sequence { get; }

    public TargetResponsePriority Priority { get; }

    public CombatCounterActivationTiming ResponseTiming { get; }

    public ImmutableArray<string> SourcePlaybookKeys { get; }

    public ImmutableArray<TargetProfileFacetIdentity> ProfileFacets { get; }

    public ImmutableArray<TargetThreat> Threats { get; }

    public ImmutableArray<ComposedTargetCounterOption> Options { get; }

    public ImmutableArray<string> ConflictGroups { get; }

    public ImmutableArray<string> EvidenceReferences { get; }

    public ImmutableArray<TargetCounterPlaybookGap> KnownGaps { get; }

    internal string ContentKey => TargetProfileText.Stable(
        StableKey,
        Sequence.ToString(CultureInfo.InvariantCulture),
        ((int)Priority).ToString(CultureInfo.InvariantCulture),
        ((int)ResponseTiming).ToString(CultureInfo.InvariantCulture),
        TargetProfileText.StableCollection(SourcePlaybookKeys),
        TargetProfileText.StableCollection(
            ProfileFacets.Select(facet => facet.StableKey)),
        TargetProfileText.StableCollection(
            Threats.Select(threat => threat.Code)),
        TargetProfileText.StableCollection(
            Options.Select(option => option.ContentKey)),
        TargetProfileText.StableCollection(ConflictGroups),
        TargetProfileText.StableCollection(EvidenceReferences),
        TargetProfileText.StableCollection(
            KnownGaps.Select(gap => gap.StableKey)));

    private static ImmutableArray<string> CopyCodes(
        IEnumerable<string> values,
        string parameterName,
        bool requireValue)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var codes = values
            .Select(value => TargetProfileText.Code(value, parameterName))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (requireValue && codes.Length == 0)
        {
            throw new ArgumentException(
                "A composed goal requires source evidence.",
                parameterName);
        }

        return codes;
    }

    private static ImmutableArray<string> CopyReferences(
        IEnumerable<string> values,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var references = values
            .Select(value =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(
                    value,
                    parameterName);
                return value.Trim();
            })
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (references.Length == 0)
        {
            throw new ArgumentException(
                "A composed goal requires a source playbook.",
                parameterName);
        }

        return references;
    }
}
