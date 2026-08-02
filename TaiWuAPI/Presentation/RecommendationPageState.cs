using TaiWu.Application.Targets;

namespace TaiWuAPI.Presentation;

public enum RecommendationPageStatus
{
    Initial,
    Loading,
    TargetReady,
    Empty,
    AmbiguousTarget,
    Success,
    SuccessWithWarning,
    InvalidConfiguration,
    UnsupportedVersion,
    Failure
}

public sealed record RecommendationPageState(
    RecommendationPageStatus Status,
    string Title,
    string Message,
    string? Recovery,
    bool CanRetryRead)
{
    public bool IsLoading => Status == RecommendationPageStatus.Loading;

    public bool IsProblem =>
        Status is RecommendationPageStatus.InvalidConfiguration
            or RecommendationPageStatus.UnsupportedVersion
            or RecommendationPageStatus.Failure;

    public static RecommendationPageState Initial() =>
        new(
            RecommendationPageStatus.Initial,
            "Start with a target",
            "Search the configured save by character name, then select "
            + "the intended opponent.",
            null,
            CanRetryRead: false);

    public static RecommendationPageState Loading(string operation) =>
        new(
            RecommendationPageStatus.Loading,
            operation,
            "Reading a new snapshot from the configured save. No game data "
            + "is changed.",
            null,
            CanRetryRead: false);

    public static RecommendationPageState ForTargetLookup(
        TargetLookupStatus status,
        int matchCount) => status switch
        {
            TargetLookupStatus.NotFound => new(
                RecommendationPageStatus.Empty,
                "No matching target",
                "The configured save returned no target for this search.",
                "Check the in-game name and search again.",
                CanRetryRead: false),
            TargetLookupStatus.Ambiguous => new(
                RecommendationPageStatus.AmbiguousTarget,
                "Multiple targets matched",
                $"{matchCount} possible targets were found. Select one using "
                + "its name, age, and named location.",
                "If the intended opponent is still unclear, gather more "
                + "in-game evidence before requesting a recommendation.",
                CanRetryRead: false),
            TargetLookupStatus.Found => new(
                RecommendationPageStatus.TargetReady,
                "Target found",
                "Select the matching result, review the context, and request "
                + "the recommendation.",
                null,
                CanRetryRead: false),
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown target lookup status.")
        };

    public static RecommendationPageState TargetReady(string displayName) =>
        new(
            RecommendationPageStatus.TargetReady,
            "Target selected",
            $"{displayName} is selected for the next read-only analysis.",
            "Review the target context before requesting a recommendation.",
            CanRetryRead: false);

    public static RecommendationPageState ForRecommendation(
        CombatRecommendationViewModel recommendation)
    {
        ArgumentNullException.ThrowIfNull(recommendation);

        if (recommendation.Warnings.Any(warning =>
                warning.Code.Equals(
                    "TARGET_GAMEDATA_VERSION_UNSUPPORTED",
                    StringComparison.Ordinal)))
        {
            return new(
                RecommendationPageStatus.UnsupportedVersion,
                "Unsupported GameData version",
                "Verified mechanic rules do not cover this GameData "
                + "version, so the helper does not estimate the missing "
                + "recommendation.",
                "Use a save from a verified game version or update the "
                + "helper's evidence-backed rules, then retry the read.",
                CanRetryRead: true);
        }

        if (recommendation.Warnings.Count > 0)
        {
            return new(
                RecommendationPageStatus.SuccessWithWarning,
                "Recommendation ready with warnings",
                "A recommendation was produced, but unavailable or uncertain "
                + "information requires manual review.",
                "Read every warning before following the manual setup.",
                CanRetryRead: true);
        }

        return new(
            RecommendationPageStatus.Success,
            "Recommendation ready",
            "The recommendation satisfies every known constraint in this "
            + "snapshot.",
            null,
            CanRetryRead: true);
    }

    public static RecommendationPageState InvalidConfiguration() =>
        new(
            RecommendationPageStatus.InvalidConfiguration,
            "Save path is not configured",
            SaveGameOptionsMessage,
            "Set SaveGames:DefaultSaveFilePath to an absolute .sav path and "
            + "restart TaiWu Helper.",
            CanRetryRead: false);

    public static RecommendationPageState Failure(
        string message,
        bool canRetryRead = true) =>
        new(
            RecommendationPageStatus.Failure,
            "Could not complete the read",
            string.IsNullOrWhiteSpace(message)
                ? "An unexpected read or calculation failure occurred."
                : message,
            "Retry the read. TaiWu Helper did not change the save or game.",
            canRetryRead);

    private const string SaveGameOptionsMessage =
        "The helper needs a valid absolute .sav path before it can read a "
        + "snapshot.";
}
