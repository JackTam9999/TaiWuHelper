using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Domain.CompanionRoles;

public sealed record CompanionRoleScoreDimension
{
    public CompanionRoleScoreDimension(
        string identity,
        CandidateProfileField field,
        string unit,
        CompanionRoleScoreDirection direction,
        CompanionRoleNormalizationKind normalization,
        decimal normalizationMinimum,
        decimal normalizationMaximum,
        decimal weight,
        CompanionRoleMissingEvidenceBehavior missingEvidenceBehavior,
        string explanationIdentity)
    {
        if (!Enum.IsDefined(field))
        {
            throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown candidate-profile field.");
        }

        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown score direction.");
        }

        if (!Enum.IsDefined(normalization))
        {
            throw new ArgumentOutOfRangeException(nameof(normalization), normalization, "Unknown normalization rule.");
        }

        if (!Enum.IsDefined(missingEvidenceBehavior))
        {
            throw new ArgumentOutOfRangeException(
                nameof(missingEvidenceBehavior),
                missingEvidenceBehavior,
                "Unknown missing-evidence behavior.");
        }

        if (normalizationMinimum > normalizationMaximum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(normalizationMinimum),
                "The normalization minimum cannot exceed its maximum.");
        }

        if (weight <= 0m || weight > 1_000m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weight),
                weight,
                "A role score weight must be greater than zero and at most 1,000.");
        }

        Identity = CompanionRoleText.Stable(identity, nameof(identity));
        Field = field;
        Unit = CompanionRoleText.Stable(unit, nameof(unit));
        Direction = direction;
        Normalization = normalization;
        NormalizationMinimum = normalizationMinimum;
        NormalizationMaximum = normalizationMaximum;
        Weight = weight;
        MissingEvidenceBehavior = missingEvidenceBehavior;
        ExplanationIdentity = CompanionRoleText.Stable(
            explanationIdentity,
            nameof(explanationIdentity));
    }

    public string Identity { get; }

    public CandidateProfileField Field { get; }

    public string Unit { get; }

    public CompanionRoleScoreDirection Direction { get; }

    public CompanionRoleNormalizationKind Normalization { get; }

    public decimal NormalizationMinimum { get; }

    public decimal NormalizationMaximum { get; }

    public decimal Weight { get; }

    public CompanionRoleMissingEvidenceBehavior MissingEvidenceBehavior { get; }

    public string ExplanationIdentity { get; }

    internal string StableKey => string.Join('|',
        Identity,
        CompanionRoleText.EnumKey(Field),
        Unit,
        CompanionRoleText.EnumKey(Direction),
        CompanionRoleText.EnumKey(Normalization),
        NormalizationMinimum.ToString(System.Globalization.CultureInfo.InvariantCulture),
        NormalizationMaximum.ToString(System.Globalization.CultureInfo.InvariantCulture),
        Weight.ToString(System.Globalization.CultureInfo.InvariantCulture),
        CompanionRoleText.EnumKey(MissingEvidenceBehavior),
        ExplanationIdentity);
}
