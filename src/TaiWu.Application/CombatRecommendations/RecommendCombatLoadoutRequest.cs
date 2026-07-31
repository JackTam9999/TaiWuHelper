using TaiWu.Application.Localization;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Application.CombatRecommendations;

public sealed record RecommendCombatLoadoutRequest
{
    public RecommendCombatLoadoutRequest(
        string saveFilePath,
        int targetCharacterId,
        RecommendationPolicy policy,
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

        if (!Enum.IsDefined(policy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                policy,
                "Unknown recommendation policy.");
        }

        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown Taiwu language.");
        }

        SaveFilePath = saveFilePath.Trim();
        TargetCharacterId = targetCharacterId;
        Policy = policy;
        CurrentLoadoutObservation = currentLoadoutObservation;
        Language = language;
    }

    public string SaveFilePath { get; }

    public int TargetCharacterId { get; }

    public RecommendationPolicy Policy { get; }

    public PlayerLoadoutObservation? CurrentLoadoutObservation { get; }

    public TaiwuLanguage Language { get; }
}
