namespace TaiWu.Domain.CombatThreats;

public sealed record TargetThreatEvidence
{
    public TargetThreatEvidence(
        string reference,
        string summary,
        TargetThreatEvidenceConfidence confidence,
        int? sourceSkillId = null,
        int? rawEffectId = null)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentException(
                "Threat evidence requires a reference.",
                nameof(reference));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException(
                "Threat evidence requires a summary.",
                nameof(summary));
        }

        if (!Enum.IsDefined(confidence))
        {
            throw new ArgumentOutOfRangeException(
                nameof(confidence),
                confidence,
                "Unknown evidence confidence.");
        }

        if (sourceSkillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceSkillId),
                sourceSkillId,
                "Source skill ID cannot be negative.");
        }

        if (rawEffectId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawEffectId),
                rawEffectId,
                "Raw effect ID cannot be negative.");
        }

        Reference = reference.Trim();
        Summary = summary.Trim();
        Confidence = confidence;
        SourceSkillId = sourceSkillId;
        RawEffectId = rawEffectId;
    }

    public string Reference { get; }

    public string Summary { get; }

    public TargetThreatEvidenceConfidence Confidence { get; }

    public int? SourceSkillId { get; }

    public int? RawEffectId { get; }
}
