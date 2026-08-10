using System.Collections.Immutable;
using System.Globalization;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybooks;

public sealed class TargetCounterPlaybookGoal
{
    public TargetCounterPlaybookGoal(
        string code,
        int sequence,
        TargetResponsePriority priority,
        CombatCounterActivationTiming responseTiming,
        IEnumerable<TargetProfileFacetIdentity> profileFacets,
        IEnumerable<TargetThreat> threats,
        IEnumerable<TargetCounterPlaybookOption> options,
        IEnumerable<string> conflictGroups,
        IEnumerable<string> evidenceReferences,
        IEnumerable<TargetCounterPlaybookGap> knownGaps)
    {
        Code = TargetProfileText.Code(code, nameof(code));
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                sequence,
                "Goal sequence cannot be negative.");
        }

        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(
                nameof(priority),
                priority,
                "Unknown response priority.");
        }

        if (!Enum.IsDefined(responseTiming))
        {
            throw new ArgumentOutOfRangeException(
                nameof(responseTiming),
                responseTiming,
                "Unknown response timing.");
        }

        ProfileFacets = CopyFacets(profileFacets);
        Threats = CopyThreats(threats);
        if (ProfileFacets.Length == 0 && Threats.Length == 0)
        {
            throw new ArgumentException(
                "A mechanical response goal must reference a typed profile "
                + "facet or target threat.",
                nameof(profileFacets));
        }

        Options = CopyOptions(options);
        EnsureOptionsAddressThreats(Options, Threats);
        ConflictGroups = CopyCodes(
            conflictGroups,
            nameof(conflictGroups),
            requireValue: false);
        EvidenceReferences = CopyCodes(
            evidenceReferences,
            nameof(evidenceReferences),
            requireValue: true);
        KnownGaps = CopyGaps(knownGaps);
        if (Options.Length == 0 && KnownGaps.Length == 0)
        {
            throw new ArgumentException(
                "A response goal without a verified option must retain an "
                + "explicit gap.",
                nameof(knownGaps));
        }

        var optionCodes = Options
            .Select(option => option.Code)
            .ToHashSet(StringComparer.Ordinal);
        var invalidGap = KnownGaps.FirstOrDefault(gap =>
            gap.Kind == TargetCounterPlaybookGapKind.InaccessibleVerifiedOption
            && !optionCodes.Contains(gap.RelatedCounterCode!));
        if (invalidGap is not null)
        {
            throw new ArgumentException(
                $"Gap {invalidGap.Code} references a counter that is not an "
                + "option for this goal.",
                nameof(knownGaps));
        }

        Sequence = sequence;
        Priority = priority;
        ResponseTiming = responseTiming;
    }

    public string Code { get; }

    public int Sequence { get; }

    public TargetResponsePriority Priority { get; }

    public CombatCounterActivationTiming ResponseTiming { get; }

    public ImmutableArray<TargetProfileFacetIdentity> ProfileFacets { get; }

    public ImmutableArray<TargetThreat> Threats { get; }

    public ImmutableArray<TargetCounterPlaybookOption> Options { get; }

    public ImmutableArray<string> ConflictGroups { get; }

    public ImmutableArray<string> EvidenceReferences { get; }

    public ImmutableArray<TargetCounterPlaybookGap> KnownGaps { get; }

    public string StableKey => Code;

    internal string ContentKey => TargetProfileText.Stable(
        Code,
        Sequence.ToString(CultureInfo.InvariantCulture),
        ((int)Priority).ToString(CultureInfo.InvariantCulture),
        ((int)ResponseTiming).ToString(CultureInfo.InvariantCulture),
        TargetProfileText.StableCollection(
            ProfileFacets.Select(facet => facet.StableKey)),
        TargetProfileText.StableCollection(
            Threats.Select(threat => threat.Code)),
        TargetProfileText.StableCollection(
            Options.Select(option => option.ContentKey)),
        TargetProfileText.StableCollection(ConflictGroups),
        TargetProfileText.StableCollection(EvidenceReferences),
        TargetProfileText.StableCollection(
            KnownGaps.Select(gap => gap.ContentKey)));

    private static ImmutableArray<TargetProfileFacetIdentity> CopyFacets(
        IEnumerable<TargetProfileFacetIdentity> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var facets = values.ToImmutableArray();
        if (facets.Any(facet => facet is null))
        {
            throw new ArgumentException(
                "Goal profile facets cannot contain null entries.",
                nameof(values));
        }

        if (facets.DistinctBy(facet => facet.StableKey, StringComparer.Ordinal)
            .Count() != facets.Length)
        {
            throw new ArgumentException(
                "Goal profile facets must be unique.",
                nameof(values));
        }

        return [.. facets.OrderBy(facet => facet.StableKey,
            StringComparer.Ordinal)];
    }

    private static ImmutableArray<TargetThreat> CopyThreats(
        IEnumerable<TargetThreat> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var threats = values.ToImmutableArray();
        if (threats.Any(threat => threat is null))
        {
            throw new ArgumentException(
                "Goal threats cannot contain null entries.",
                nameof(values));
        }

        if (threats.DistinctBy(threat => threat.Code, StringComparer.Ordinal)
            .Count() != threats.Length)
        {
            throw new ArgumentException(
                "Goal threats must be unique.",
                nameof(values));
        }

        return [.. threats.OrderBy(threat => threat.Code,
            StringComparer.Ordinal)];
    }

    private static ImmutableArray<TargetCounterPlaybookOption> CopyOptions(
        IEnumerable<TargetCounterPlaybookOption> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var options = values.ToImmutableArray();
        if (options.Any(option => option is null))
        {
            throw new ArgumentException(
                "Goal options cannot contain null entries.",
                nameof(values));
        }

        if (options.DistinctBy(option => option.Code, StringComparer.Ordinal)
            .Count() != options.Length)
        {
            throw new ArgumentException(
                "Goal counter options must be unique.",
                nameof(values));
        }

        return
        [
            .. options
                .OrderByDescending(option => option.Strength)
                .ThenBy(option => TimingOrder(option.ActivationTiming))
                .ThenBy(option => option.Code, StringComparer.Ordinal)
        ];
    }

    private static ImmutableArray<string> CopyCodes(
        IEnumerable<string> values,
        string parameterName,
        bool requireValue)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var codes = values
            .Select(value => TargetProfileText.Code(value, parameterName))
            .ToImmutableArray();
        if (requireValue && codes.Length == 0)
        {
            throw new ArgumentException(
                "A response goal requires evidence.",
                parameterName);
        }

        if (codes.Distinct(StringComparer.Ordinal).Count() != codes.Length)
        {
            throw new ArgumentException(
                "Stable code collections must contain unique values.",
                parameterName);
        }

        return [.. codes.Order(StringComparer.Ordinal)];
    }

    private static ImmutableArray<TargetCounterPlaybookGap> CopyGaps(
        IEnumerable<TargetCounterPlaybookGap> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var gaps = values.ToImmutableArray();
        if (gaps.Any(gap => gap is null))
        {
            throw new ArgumentException(
                "Goal gaps cannot contain null entries.",
                nameof(values));
        }

        if (gaps.DistinctBy(gap => gap.Code, StringComparer.Ordinal).Count()
            != gaps.Length)
        {
            throw new ArgumentException(
                "Goal gaps must be unique.",
                nameof(values));
        }

        return [.. gaps.OrderBy(gap => gap.Code, StringComparer.Ordinal)];
    }

    private static void EnsureOptionsAddressThreats(
        ImmutableArray<TargetCounterPlaybookOption> options,
        ImmutableArray<TargetThreat> threats)
    {
        if (options.Length == 0)
        {
            return;
        }

        if (threats.Length == 0)
        {
            throw new ArgumentException(
                "A verified counter option requires an existing typed threat "
                + "reference on its response goal.",
                nameof(threats));
        }

        var threatCodes = threats
            .Select(threat => threat.Code)
            .ToHashSet(StringComparer.Ordinal);
        var unrelated = options.FirstOrDefault(option =>
            !option.CounterRule.ThreatCodes.Any(threatCodes.Contains));
        if (unrelated is not null)
        {
            throw new ArgumentException(
                $"Counter {unrelated.Code} does not address a typed threat "
                + "on this goal.",
                nameof(options));
        }
    }

    private static int TimingOrder(CombatCounterActivationTiming timing) =>
        timing switch
        {
            CombatCounterActivationTiming.CombatStartPassive => 0,
            CombatCounterActivationTiming.EquippedPassive => 1,
            CombatCounterActivationTiming.ActiveDefense => 2,
            CombatCounterActivationTiming.ActiveAgility => 3,
            CombatCounterActivationTiming.ActiveAttack => 4,
            _ => throw new ArgumentOutOfRangeException(
                nameof(timing),
                timing,
                "Unknown counter activation timing.")
        };
}
