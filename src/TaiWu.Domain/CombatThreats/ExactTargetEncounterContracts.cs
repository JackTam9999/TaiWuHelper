using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatThreats;

public enum TargetEncounterBindingStatus
{
    Complete,
    Partial,
    Conflicting,
    WrongPhase,
    UnsupportedVersion
}

public enum TargetEncounterEvidenceSource
{
    InstalledConfiguration,
    RuntimeBehavior,
    SavedStoryTemplate,
    SavedEquippedLoadout,
    CurrentScreenObservation,
    VerifiedGlobalRule,
    SyntheticFixture
}

public enum TargetEncounterFactState
{
    Confirmed,
    NotPresent,
    ManualObservationRequired
}

public enum TargetEncounterFactKind
{
    EncounterPhase,
    DirectPracticeCoverage,
    MagicSoundCastSet,
    MindDamagePressure,
    DistractionMarkAccumulation,
    MindRhythmCountdown,
    MindUpheavalCascade,
    DefeatMarkReset,
    ReverseSuppressionApplicability,
    InnerPowerSkillSet,
    ActiveInnerPowerState,
    AgilitySkillSet,
    FootworkSustain,
    MovementPressure,
    RangePressure,
    SpeedPressure,
    CloseRangePressure,
    LiveMarkCount,
    LiveRhythmCount,
    LiveTemporaryLayers,
    CurrentDistance,
    CurrentResourceState,
    ActiveAgility
}

public enum TargetEncounterTransitionState
{
    Verified,
    NotApplicable,
    ManualObservationRequired
}

public enum TargetEncounterTransitionTiming
{
    BeforeCombat,
    DuringCast,
    OnHit,
    OnMarkApplied,
    OnCountdownZero,
    OnDefeatThreshold,
    WhileAgilityActive,
    OnDistanceChanged,
    OnManualObservation
}

public sealed record TargetEncounterEvidence
{
    public TargetEncounterEvidence(
        TargetEncounterEvidenceSource source,
        string reference,
        string gameDataVersion)
    {
        if (!Enum.IsDefined(source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unknown exact-target evidence source.");
        }

        Source = source;
        Reference = TargetEncounterText.Stable(reference, nameof(reference));
        GameDataVersion = TargetEncounterText.Stable(
            gameDataVersion,
            nameof(gameDataVersion));
    }

    public TargetEncounterEvidenceSource Source { get; }

    public string Reference { get; }

    public string GameDataVersion { get; }

    internal string StableKey => string.Join(':',
        (int)Source,
        Reference,
        GameDataVersion);
}

public sealed record TargetEncounterPhaseEvidence
{
    public TargetEncounterPhaseEvidence(
        int targetTemplateId,
        TargetEncounterEvidence evidence)
    {
        if (targetTemplateId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetTemplateId),
                targetTemplateId,
                "An exact target template ID must be positive.");
        }

        TargetTemplateId = targetTemplateId;
        Evidence = evidence
            ?? throw new ArgumentNullException(nameof(evidence));
    }

    public int TargetTemplateId { get; }

    public TargetEncounterEvidence Evidence { get; }

    internal string StableKey =>
        $"{TargetTemplateId}:{Evidence.StableKey}";
}

public sealed class TargetEncounterPhaseObservation
{
    public TargetEncounterPhaseObservation(
        string detectedGameDataVersion,
        IEnumerable<TargetEncounterPhaseEvidence> phaseEvidence,
        TargetLoadoutCoverageKind? loadoutCoverage = null,
        IEnumerable<TargetThreatSkillSignature>? equippedSkillSignatures = null,
        TargetEncounterEvidence? loadoutEvidence = null)
    {
        DetectedGameDataVersion = TargetEncounterText.Stable(
            detectedGameDataVersion,
            nameof(detectedGameDataVersion));
        PhaseEvidence = TargetEncounterText.CopyUnique(
            phaseEvidence,
            item => item.StableKey,
            nameof(phaseEvidence));
        if (loadoutCoverage.HasValue
            && !Enum.IsDefined(loadoutCoverage.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(loadoutCoverage),
                loadoutCoverage,
                "Unknown target-loadout coverage.");
        }

        var signatures = (equippedSkillSignatures ?? [])
            .OrderBy(item => item.SkillId)
            .ToImmutableArray();
        if (signatures.Any(item => item is null)
            || signatures.GroupBy(item => item.SkillId).Any(group =>
                group.Count() > 1))
        {
            throw new ArgumentException(
                "Observed target skill signatures must be non-null and unique.",
                nameof(equippedSkillSignatures));
        }

        if ((loadoutCoverage.HasValue || signatures.Length > 0)
            && loadoutEvidence is null)
        {
            throw new ArgumentException(
                "Observed target loadout data requires source evidence.",
                nameof(loadoutEvidence));
        }

        if (!loadoutCoverage.HasValue && signatures.Length > 0)
        {
            throw new ArgumentException(
                "Observed target skills require an explicit coverage kind.",
                nameof(loadoutCoverage));
        }

        LoadoutCoverage = loadoutCoverage;
        EquippedSkillSignatures = signatures;
        LoadoutEvidence = loadoutEvidence;
    }

    public string DetectedGameDataVersion { get; }

    public ImmutableArray<TargetEncounterPhaseEvidence> PhaseEvidence { get; }

    public TargetLoadoutCoverageKind? LoadoutCoverage { get; }

    public ImmutableArray<TargetThreatSkillSignature>
        EquippedSkillSignatures
    { get; }

    public TargetEncounterEvidence? LoadoutEvidence { get; }
}

public sealed record TargetEncounterFact
{
    public TargetEncounterFact(
        string code,
        TargetEncounterFactKind kind,
        TargetEncounterFactState state,
        IEnumerable<int> sourceSkillIds,
        string limitationCode,
        IEnumerable<TargetEncounterEvidence> evidence)
    {
        Code = TargetEncounterText.Code(code, nameof(code));
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        ArgumentNullException.ThrowIfNull(sourceSkillIds);
        var skills = sourceSkillIds.Order().ToImmutableArray();
        if (skills.Any(id => id < 0) || skills.Distinct().Count() != skills.Length)
        {
            throw new ArgumentException(
                "Exact-target fact skill IDs must be non-negative and unique.",
                nameof(sourceSkillIds));
        }

        Kind = kind;
        State = state;
        SourceSkillIds = skills;
        LimitationCode = TargetEncounterText.Code(
            limitationCode,
            nameof(limitationCode));
        Evidence = TargetEncounterText.CopyUnique(
            evidence,
            item => item.StableKey,
            nameof(evidence));
        if (Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "Every exact-target fact requires evidence.",
                nameof(evidence));
        }
    }

    public string Code { get; }

    public TargetEncounterFactKind Kind { get; }

    public TargetEncounterFactState State { get; }

    public ImmutableArray<int> SourceSkillIds { get; }

    public string LimitationCode { get; }

    public ImmutableArray<TargetEncounterEvidence> Evidence { get; }

    internal string StableKey => TargetEncounterText.Join(
        Code,
        ((int)Kind).ToString(System.Globalization.CultureInfo.InvariantCulture),
        ((int)State).ToString(System.Globalization.CultureInfo.InvariantCulture),
        string.Join(',', SourceSkillIds),
        LimitationCode,
        string.Join(',', Evidence.Select(item => item.StableKey)));
}

public sealed record TargetEncounterTransition
{
    public TargetEncounterTransition(
        string code,
        TargetEncounterTransitionState state,
        TargetEncounterTransitionTiming timing,
        IEnumerable<string> triggerFactCodes,
        IEnumerable<string> resultFactCodes,
        string limitationCode,
        IEnumerable<TargetEncounterEvidence> evidence)
    {
        Code = TargetEncounterText.Code(code, nameof(code));
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        if (!Enum.IsDefined(timing))
        {
            throw new ArgumentOutOfRangeException(nameof(timing), timing, null);
        }

        State = state;
        Timing = timing;
        TriggerFactCodes = TargetEncounterText.Codes(
            triggerFactCodes,
            nameof(triggerFactCodes));
        ResultFactCodes = TargetEncounterText.Codes(
            resultFactCodes,
            nameof(resultFactCodes));
        if (TriggerFactCodes.IsEmpty || ResultFactCodes.IsEmpty)
        {
            throw new ArgumentException(
                "An exact-target transition requires trigger and result facts.");
        }

        LimitationCode = TargetEncounterText.Code(
            limitationCode,
            nameof(limitationCode));
        Evidence = TargetEncounterText.CopyUnique(
            evidence,
            item => item.StableKey,
            nameof(evidence));
        if (Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "Every exact-target transition requires evidence.",
                nameof(evidence));
        }
    }

    public string Code { get; }

    public TargetEncounterTransitionState State { get; }

    public TargetEncounterTransitionTiming Timing { get; }

    public ImmutableArray<string> TriggerFactCodes { get; }

    public ImmutableArray<string> ResultFactCodes { get; }

    public string LimitationCode { get; }

    public ImmutableArray<TargetEncounterEvidence> Evidence { get; }

    internal string StableKey => TargetEncounterText.Join(
        Code,
        ((int)State).ToString(System.Globalization.CultureInfo.InvariantCulture),
        ((int)Timing).ToString(System.Globalization.CultureInfo.InvariantCulture),
        string.Join(',', TriggerFactCodes),
        string.Join(',', ResultFactCodes),
        LimitationCode,
        string.Join(',', Evidence.Select(item => item.StableKey)));
}

internal static class TargetEncounterText
{
    internal static string Stable(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "An exact-target stable value cannot be blank.",
                parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Contains('\r') || trimmed.Contains('\n')
            || trimmed.Contains('|'))
        {
            throw new ArgumentException(
                "An exact-target stable value cannot contain separators.",
                parameterName);
        }

        return trimmed;
    }

    internal static string Code(string? value, string parameterName)
    {
        var code = Stable(value, parameterName);
        if (code.Length > 160 || code.Any(character =>
                !char.IsAsciiLetterUpper(character)
                && !char.IsAsciiDigit(character)
                && character is not '_' and not '-' and not '.'))
        {
            throw new ArgumentException(
                "An exact-target code uses uppercase ASCII code characters.",
                parameterName);
        }

        return code;
    }

    internal static ImmutableArray<string> Codes(
        IEnumerable<string> values,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copied = values.Select(value => Code(value, parameterName))
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (copied.Distinct(StringComparer.Ordinal).Count() != copied.Length)
        {
            throw new ArgumentException(
                "Exact-target codes must be unique.",
                parameterName);
        }

        return copied;
    }

    internal static ImmutableArray<T> CopyUnique<T>(
        IEnumerable<T> values,
        Func<T, string> key,
        string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        var copied = values.ToImmutableArray();
        if (copied.Any(item => item is null)
            || copied.GroupBy(key, StringComparer.Ordinal).Any(group =>
                group.Count() > 1))
        {
            throw new ArgumentException(
                "Exact-target values must be non-null and unique.",
                parameterName);
        }

        return [.. copied.OrderBy(key, StringComparer.Ordinal)];
    }

    internal static string Join(params string[] values) =>
        string.Join('|', values);

    internal static string Fingerprint(string canonical) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
}
