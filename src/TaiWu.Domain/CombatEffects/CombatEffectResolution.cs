using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatEffects;

public sealed record CombatEffectResolution
{
    internal CombatEffectResolution(
        string observedGameDataVersion,
        int skillId,
        PracticeDirection direction,
        int rawEffectId,
        CombatEffectResolutionStatus status,
        CombatEffectCatalogEntry? catalogEntry)
    {
        ObservedGameDataVersion = observedGameDataVersion;
        SkillId = skillId;
        Direction = direction;
        RawEffectId = rawEffectId;
        Status = status;
        CatalogEntry = catalogEntry;
    }

    public string ObservedGameDataVersion { get; }

    public int SkillId { get; }

    public PracticeDirection Direction { get; }

    public int RawEffectId { get; }

    public CombatEffectResolutionStatus Status { get; }

    public CombatEffectCatalogEntry? CatalogEntry { get; }

    public bool IsRecognized =>
        Status == CombatEffectResolutionStatus.Recognized;
}
