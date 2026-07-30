namespace TaiWu.Domain.CombatThreats;

public sealed record UnknownTargetMechanic
{
    public UnknownTargetMechanic(
        string description,
        string evidenceReference,
        int? sourceSkillId = null,
        int? rawEffectId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Unknown mechanic requires a description.",
                nameof(description));
        }

        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException(
                "Unknown mechanic requires an evidence reference.",
                nameof(evidenceReference));
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

        Description = description.Trim();
        EvidenceReference = evidenceReference.Trim();
        SourceSkillId = sourceSkillId;
        RawEffectId = rawEffectId;
    }

    public string Description { get; }

    public string EvidenceReference { get; }

    public int? SourceSkillId { get; }

    public int? RawEffectId { get; }
}
