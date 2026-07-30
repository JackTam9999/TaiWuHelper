using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace TaiWu.Domain.CombatThreats;

public sealed partial record TargetThreat
{
    public TargetThreat(
        string code,
        TargetThreatKind kind,
        TargetThreatSeverity severity,
        string title,
        string explanation,
        TargetThreatActivationTiming activationTiming,
        IEnumerable<TargetThreatEvidence> evidence)
    {
        if (string.IsNullOrWhiteSpace(code)
            || !ThreatCodePattern().IsMatch(code))
        {
            throw new ArgumentException(
                "Threat code must contain only uppercase letters, numbers, "
                + "and underscores.",
                nameof(code));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown target-threat kind.");
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(
                nameof(severity),
                severity,
                "Unknown target-threat severity.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Threat title cannot be blank.",
                nameof(title));
        }

        if (string.IsNullOrWhiteSpace(explanation))
        {
            throw new ArgumentException(
                "Threat explanation cannot be blank.",
                nameof(explanation));
        }

        if (!Enum.IsDefined(activationTiming))
        {
            throw new ArgumentOutOfRangeException(
                nameof(activationTiming),
                activationTiming,
                "Unknown threat activation timing.");
        }

        ArgumentNullException.ThrowIfNull(evidence);
        var evidenceValues = evidence.ToImmutableArray();
        if (evidenceValues.IsEmpty)
        {
            throw new ArgumentException(
                "Every target threat requires source evidence.",
                nameof(evidence));
        }

        if (evidenceValues.Any(value => value is null))
        {
            throw new ArgumentException(
                "Threat evidence cannot contain null entries.",
                nameof(evidence));
        }

        Code = code;
        Kind = kind;
        Severity = severity;
        Title = title.Trim();
        Explanation = explanation.Trim();
        ActivationTiming = activationTiming;
        Evidence = evidenceValues;
    }

    public string Code { get; }

    public TargetThreatKind Kind { get; }

    public TargetThreatSeverity Severity { get; }

    public string Title { get; }

    public string Explanation { get; }

    public TargetThreatActivationTiming ActivationTiming { get; }

    public ImmutableArray<TargetThreatEvidence> Evidence { get; }

    [GeneratedRegex("^[A-Z0-9]+(?:_[A-Z0-9]+)*$")]
    private static partial Regex ThreatCodePattern();
}
