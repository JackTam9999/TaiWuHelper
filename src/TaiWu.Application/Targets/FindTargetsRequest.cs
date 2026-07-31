using TaiWu.Application.Localization;

namespace TaiWu.Application.Targets;

public sealed record FindTargetsRequest
{
    public const int MaximumResults = 100;

    public FindTargetsRequest(
        string saveFilePath,
        string query,
        int maxResults = 25,
        TaiwuLanguage language = TaiwuLanguage.English)
    {
        if (string.IsNullOrWhiteSpace(saveFilePath))
        {
            throw new ArgumentException(
                "A save-file path is required.",
                nameof(saveFilePath));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "A target name or character ID is required.",
                nameof(query));
        }

        if (maxResults is < 1 or > MaximumResults)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxResults),
                maxResults,
                $"Maximum results must be between 1 and {MaximumResults}.");
        }

        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown Taiwu language.");
        }

        SaveFilePath = saveFilePath.Trim();
        Query = query.Trim();
        MaxResults = maxResults;
        Language = language;
    }

    public string SaveFilePath { get; }

    public string Query { get; }

    public int MaxResults { get; }

    public TaiwuLanguage Language { get; }
}
