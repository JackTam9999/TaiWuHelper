using TaiWu.Application.Localization;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.LoadoutComparisons;
using TaiWuAPI.Localization;

namespace TaiWuAPI.Presentation;

internal static class LoadoutComparisonUiText
{
    public static string Column(
        LoadoutComparisonColumnViewModel column,
        TaiwuLanguage language) => column.Kind switch
        {
            LoadoutComparisonColumnKind.Current => Text(language, "Current loadout"),
            LoadoutComparisonColumnKind.Safe => Text(language, "Safe"),
            LoadoutComparisonColumnKind.Balanced => Text(language, "Balanced"),
            LoadoutComparisonColumnKind.Aggressive => Text(language, "Aggressive"),
            _ => Text(language, "Unavailable")
        };

    public static string Policy(
        RecommendationPolicy policy,
        TaiwuLanguage language) => policy switch
        {
            RecommendationPolicy.Safe => Text(language, "Safe"),
            RecommendationPolicy.Balanced => Text(language, "Balanced"),
            RecommendationPolicy.Aggressive => Text(language, "Aggressive"),
            _ => Text(language, "Unavailable")
        };

    public static string ColumnStatus(
        LoadoutComparisonColumnViewModel column,
        TaiwuLanguage language) => column.Status switch
        {
            LoadoutComparisonColumnStatus.Available
                when column.Kind == LoadoutComparisonColumnKind.Current =>
                Text(language, "Baseline used for this result"),
            LoadoutComparisonColumnStatus.Available =>
                column.ManualActionCount.HasValue
                    ? $"{column.ManualActionCount.Value} "
                        + Text(language, "manual action(s)")
                    : Text(language, "Feasible policy winner"),
            LoadoutComparisonColumnStatus.Infeasible =>
                Text(language, "No feasible proposal"),
            LoadoutComparisonColumnStatus.Unavailable =>
                Text(language, "Unavailable"),
            _ => Text(language, "Unavailable")
        };

    private static string Text(TaiwuLanguage language, string english) =>
        UiText.Get(language, english);
}
