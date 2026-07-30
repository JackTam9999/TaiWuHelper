using System.Collections.Immutable;
using System.Text.RegularExpressions;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatCounters;

public sealed partial record CombatCounterRule
{
    public CombatCounterRule(
        string code,
        IEnumerable<string> threatCodes,
        CombatCounterStrength strength,
        CombatCounterActivationTiming activationTiming,
        CombatEffectCatalogEntry effect,
        IEnumerable<CombatRequirement> requirements,
        string rationale)
    {
        Code = ValidateCode(code, nameof(code));
        ArgumentNullException.ThrowIfNull(threatCodes);
        var threatCodeValues = threatCodes
            .Select(value => ValidateCode(value, nameof(threatCodes)))
            .ToImmutableArray();
        if (threatCodeValues.IsEmpty)
        {
            throw new ArgumentException(
                "A counter rule must address at least one threat.",
                nameof(threatCodes));
        }

        if (threatCodeValues.Distinct(
                StringComparer.Ordinal).Count() != threatCodeValues.Length)
        {
            throw new ArgumentException(
                "Counter threat codes cannot be duplicated.",
                nameof(threatCodes));
        }

        if (!Enum.IsDefined(strength))
        {
            throw new ArgumentOutOfRangeException(
                nameof(strength),
                strength,
                "Unknown counter strength.");
        }

        if (!Enum.IsDefined(activationTiming))
        {
            throw new ArgumentOutOfRangeException(
                nameof(activationTiming),
                activationTiming,
                "Unknown counter activation timing.");
        }

        Effect = effect ?? throw new ArgumentNullException(nameof(effect));
        if (!Effect.HasTypedMechanics)
        {
            throw new ArgumentException(
                "A counter rule requires a typed, recognized effect.",
                nameof(effect));
        }

        ArgumentNullException.ThrowIfNull(requirements);
        Requirements = [.. requirements];
        if (Requirements.Any(requirement => requirement is null))
        {
            throw new ArgumentException(
                "Counter requirements cannot contain null entries.",
                nameof(requirements));
        }

        if (string.IsNullOrWhiteSpace(rationale))
        {
            throw new ArgumentException(
                "Counter rationale cannot be blank.",
                nameof(rationale));
        }

        ThreatCodes = threatCodeValues;
        Strength = strength;
        ActivationTiming = activationTiming;
        Rationale = rationale.Trim();
    }

    public string Code { get; }

    public ImmutableArray<string> ThreatCodes { get; }

    public CombatCounterStrength Strength { get; }

    public CombatCounterActivationTiming ActivationTiming { get; }

    public CombatEffectCatalogEntry Effect { get; }

    public PracticeDirection RequiredDirection => Effect.Direction;

    public ImmutableArray<CombatRequirement> Requirements { get; }

    public string Rationale { get; }

    private static string ValidateCode(string code, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(code)
            || !CounterCodePattern().IsMatch(code))
        {
            throw new ArgumentException(
                "Counter and threat codes must contain only uppercase "
                + "letters, numbers, and underscores.",
                parameterName);
        }

        return code;
    }

    [GeneratedRegex("^[A-Z0-9]+(?:_[A-Z0-9]+)*$")]
    private static partial Regex CounterCodePattern();
}
