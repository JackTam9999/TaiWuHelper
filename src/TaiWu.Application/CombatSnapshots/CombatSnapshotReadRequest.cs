using TaiWu.Application.Localization;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Application.CombatSnapshots;

public sealed record CombatSnapshotReadRequest
{
    public CombatSnapshotReadRequest(
        string saveFilePath,
        int targetCharacterId,
        PlayerLoadoutObservation? currentLoadoutObservation = null,
        TaiwuLanguage language = TaiwuLanguage.English)
    {
        if (string.IsNullOrWhiteSpace(saveFilePath))
        {
            throw new ArgumentException(
                "A save-file path is required.",
                nameof(saveFilePath));
        }

        if (targetCharacterId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetCharacterId),
                targetCharacterId,
                "Target character ID must be greater than zero.");
        }

        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown Taiwu language.");
        }

        SaveFilePath = saveFilePath;
        TargetCharacterId = targetCharacterId;
        CurrentLoadoutObservation = currentLoadoutObservation;
        Language = language;
    }

    public string SaveFilePath { get; }

    public int TargetCharacterId { get; }

    public PlayerLoadoutObservation? CurrentLoadoutObservation { get; }

    public TaiwuLanguage Language { get; }
}
