using TaiWu.Application.Localization;

namespace TaiWu.Application.Targets;

public sealed record TargetLookupReadRequest
{
    public TargetLookupReadRequest(
        string saveFilePath,
        TaiwuLanguage language = TaiwuLanguage.English)
    {
        if (string.IsNullOrWhiteSpace(saveFilePath))
        {
            throw new ArgumentException(
                "A save-file path is required.",
                nameof(saveFilePath));
        }

        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown Taiwu language.");
        }

        SaveFilePath = saveFilePath.Trim();
        Language = language;
    }

    public string SaveFilePath { get; }

    public TaiwuLanguage Language { get; }
}
