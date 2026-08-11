using System.Collections.Immutable;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record CombatLoadoutOption
{
    public CombatLoadoutOption(
        CombatSkillCandidate candidate,
        IEnumerable<CombatRequirement> requirements,
        IEnumerable<string> threatCodes,
        bool isCurrentlyEquipped,
        string evidenceReference,
        CombatCounterStrength? counterStrength = null,
        CombatCounterActivationTiming? activationTiming = null,
        int? expectedEffectId = null)
    {
        Candidate = candidate
            ?? throw new ArgumentNullException(nameof(candidate));
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(threatCodes);
        Requirements = [.. requirements];
        ThreatCodes = [.. threatCodes];
        if (Requirements.Any(requirement => requirement is null))
        {
            throw new ArgumentException(
                "Loadout-option requirements cannot contain nulls.",
                nameof(requirements));
        }

        if (ThreatCodes.Any(string.IsNullOrWhiteSpace)
            || ThreatCodes.Distinct(
                StringComparer.Ordinal).Count() != ThreatCodes.Length)
        {
            throw new ArgumentException(
                "Threat codes must be non-blank and unique.",
                nameof(threatCodes));
        }

        if (counterStrength.HasValue
            && !Enum.IsDefined(counterStrength.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(counterStrength),
                counterStrength,
                "Unknown counter strength.");
        }

        if (activationTiming.HasValue
            && !Enum.IsDefined(activationTiming.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(activationTiming),
                activationTiming,
                "Unknown counter activation timing.");
        }

        if (counterStrength.HasValue != activationTiming.HasValue)
        {
            throw new ArgumentException(
                "Counter strength and activation timing must be supplied "
                + "together.");
        }

        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException(
                "A loadout option requires evidence.",
                nameof(evidenceReference));
        }

        if (expectedEffectId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedEffectId),
                expectedEffectId,
                "Expected effect ID cannot be negative.");
        }

        if (expectedEffectId.HasValue
            && !Candidate.RequiredDirection.HasValue)
        {
            throw new ArgumentException(
                "An expected effect ID requires a direction-specific "
                + "candidate.",
                nameof(expectedEffectId));
        }

        IsCurrentlyEquipped = isCurrentlyEquipped;
        EvidenceReference = evidenceReference.Trim();
        CounterStrength = counterStrength;
        ActivationTiming = activationTiming;
        ExpectedEffectId = expectedEffectId;
    }

    public CombatSkillCandidate Candidate { get; }

    public ImmutableArray<CombatRequirement> Requirements { get; }

    public ImmutableArray<string> ThreatCodes { get; }

    public bool IsCurrentlyEquipped { get; }

    public string EvidenceReference { get; }

    public CombatCounterStrength? CounterStrength { get; }

    public CombatCounterActivationTiming? ActivationTiming { get; }

    public int? ExpectedEffectId { get; }

    public bool IsCombatStartCounter =>
        ActivationTiming
        == CombatCounterActivationTiming.CombatStartPassive;

    public bool IsHardCounter =>
        CounterStrength == CombatCounters.CombatCounterStrength.HardCounter;

    public static CombatLoadoutOption FromCounterRule(
        CombatCounterRule rule,
        bool isCurrentlyEquipped,
        bool allowDirectionChange = false,
        bool allowBreakthrough = false,
        IEnumerable<string>? applicableThreatCodes = null)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var threatCodes = applicableThreatCodes?.ToArray()
            ?? rule.ThreatCodes.ToArray();
        if (threatCodes.Except(
                rule.ThreatCodes,
                StringComparer.Ordinal).Any())
        {
            throw new ArgumentException(
                "Applicable threats must belong to the counter rule.",
                nameof(applicableThreatCodes));
        }

        if (threatCodes.Length == 0)
        {
            throw new ArgumentException(
                "A verified counter must address a target threat.",
                nameof(applicableThreatCodes));
        }

        return new CombatLoadoutOption(
            new CombatSkillCandidate(
                rule.Effect.SkillId,
                requiredDirection: rule.RequiredDirection,
                allowDirectionChange: allowDirectionChange,
                allowBreakthrough: allowBreakthrough),
            rule.Requirements,
            threatCodes,
            isCurrentlyEquipped,
            rule.Effect.SourceReference,
            rule.Strength,
            rule.ActivationTiming,
            rule.Effect.RawEffectId);
    }

    public static CombatLoadoutOption RetainCurrentSkill(
        int skillId,
        string evidenceReference)
    {
        return new CombatLoadoutOption(
            new CombatSkillCandidate(skillId),
            requirements: [],
            threatCodes: [],
            isCurrentlyEquipped: true,
            evidenceReference);
    }

    public static CombatLoadoutOption SelectCapacityProvider(
        int skillId,
        string evidenceReference)
    {
        return new CombatLoadoutOption(
            new CombatSkillCandidate(skillId),
            requirements: [],
            threatCodes: [],
            isCurrentlyEquipped: false,
            evidenceReference);
    }
}
