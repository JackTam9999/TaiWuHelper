namespace TaiWuAPI.Presentation;

public sealed record RecommendationWarningClassification(
    PresentationWarningKind Kind,
    bool IsCritical,
    string EffectOnRecommendation);

public static class RecommendationWarningPresentation
{
    public static RecommendationWarningClassification Classify(
        string source,
        string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (code.Contains("NOT_NEWER", StringComparison.Ordinal)
            || code.Contains("OBSERVATION", StringComparison.Ordinal))
        {
            return new(
                PresentationWarningKind.ObservationDifference,
                IsCritical: false,
                "The current-screen input was not used as the authoritative "
                + "value; verify the displayed loadout before following it.");
        }

        if (code.Contains("STALE", StringComparison.Ordinal)
            || code.Contains("TIMESTAMP", StringComparison.Ordinal))
        {
            return new(
                PresentationWarningKind.StaleData,
                IsCritical: false,
                "Snapshot freshness cannot be fully established; reread the "
                + "save before relying on time-sensitive details.");
        }

        if (code.Contains("UNSUPPORTED", StringComparison.Ordinal)
            || code.Contains("UNRECOGNIZED", StringComparison.Ordinal)
            || code.Contains("UNVERIFIED", StringComparison.Ordinal))
        {
            return new(
                PresentationWarningKind.UnverifiedMechanic,
                IsCritical: true,
                "The affected mechanic was excluded from verified scoring, "
                + "so threat coverage may be incomplete.");
        }

        if (code.Contains("UNAVAILABLE", StringComparison.Ordinal))
        {
            var critical = code.Contains("TARGET_", StringComparison.Ordinal)
                || code.Contains("GAMEDATA_", StringComparison.Ordinal);
            return new(
                PresentationWarningKind.UnavailableValue,
                critical,
                "The affected value remains unavailable and is not replaced "
                + "with an estimate; review the related caveat manually.");
        }

        if (source.Equals(
                "CandidateGeneration",
                StringComparison.Ordinal))
        {
            var critical = code.Equals(
                "NoEligibleOptions",
                StringComparison.Ordinal);
            return new(
                PresentationWarningKind.CandidateSearch,
                critical,
                critical
                    ? "No eligible option survived validation; this style "
                      + "cannot provide a feasible recommendation."
                    : "The affected option was excluded before scoring; "
                      + "returned candidates still satisfy known constraints.");
        }

        return new(
            PresentationWarningKind.General,
            IsCritical: false,
            "This warning is retained with the recommendation for manual "
            + "review and does not receive an inferred replacement value.");
    }
}
