using System.Collections.Immutable;
using System.Globalization;

namespace TaiWu.Domain.TacticalCombat;

public sealed record TacticalSearchBounds
{
    public TacticalSearchBounds(
        int maximumOptions,
        int maximumExploredCombinations,
        TimeSpan maximumElapsed,
        int maximumResults)
    {
        if (maximumOptions is <= 0 or > 24)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumOptions));
        }

        if (maximumExploredCombinations is <= 0 or > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumExploredCombinations));
        }

        if (maximumElapsed <= TimeSpan.Zero
            || maximumElapsed > TimeSpan.FromMinutes(10))
        {
            throw new ArgumentOutOfRangeException(nameof(maximumElapsed));
        }

        if (maximumResults is <= 0 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumResults));
        }

        MaximumOptions = maximumOptions;
        MaximumExploredCombinations = maximumExploredCombinations;
        MaximumElapsed = maximumElapsed;
        MaximumResults = maximumResults;
    }

    public int MaximumOptions { get; }

    public int MaximumExploredCombinations { get; }

    public TimeSpan MaximumElapsed { get; }

    public int MaximumResults { get; }

    internal string StableKey => string.Join(':',
        MaximumOptions.ToString(CultureInfo.InvariantCulture),
        MaximumExploredCombinations.ToString(CultureInfo.InvariantCulture),
        MaximumElapsed.Ticks.ToString(CultureInfo.InvariantCulture),
        MaximumResults.ToString(CultureInfo.InvariantCulture));
}

public sealed record TacticalCacheReuseDiagnostic
{
    public TacticalCacheReuseDiagnostic(
        string cacheIdentity,
        int hitCount,
        int missCount)
    {
        if (hitCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hitCount));
        }

        if (missCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(missCount));
        }

        CacheIdentity = TacticalCombatText.Code(
            cacheIdentity,
            nameof(cacheIdentity));
        HitCount = hitCount;
        MissCount = missCount;
    }

    public string CacheIdentity { get; }

    public int HitCount { get; }

    public int MissCount { get; }

    internal string StableKey => CacheIdentity;

    internal string DiagnosticKey => string.Join(':',
        CacheIdentity,
        HitCount.ToString(CultureInfo.InvariantCulture),
        MissCount.ToString(CultureInfo.InvariantCulture));
}

public sealed class TacticalSearchCoverage
{
    public TacticalSearchCoverage(
        TacticalSearchBounds bounds,
        int candidateUniverseCount,
        int roleSupportedCount,
        int admittedCount,
        int rejectedCount,
        int unsupportedCount,
        int irrelevantCount,
        int dominatedCount,
        int searchedOptionCount,
        int exploredCombinationCount,
        int feasibleResultCount,
        int retainedResultCount,
        TacticalSearchTerminator firstTerminator,
        TimeSpan elapsed,
        IEnumerable<TacticalCacheReuseDiagnostic>? caches = null)
    {
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        CandidateUniverseCount = NonNegative(
            candidateUniverseCount,
            nameof(candidateUniverseCount));
        RoleSupportedCount = NonNegative(
            roleSupportedCount,
            nameof(roleSupportedCount));
        AdmittedCount = NonNegative(admittedCount, nameof(admittedCount));
        RejectedCount = NonNegative(rejectedCount, nameof(rejectedCount));
        UnsupportedCount = NonNegative(
            unsupportedCount,
            nameof(unsupportedCount));
        IrrelevantCount = NonNegative(
            irrelevantCount,
            nameof(irrelevantCount));
        DominatedCount = NonNegative(
            dominatedCount,
            nameof(dominatedCount));
        SearchedOptionCount = NonNegative(
            searchedOptionCount,
            nameof(searchedOptionCount));
        ExploredCombinationCount = NonNegative(
            exploredCombinationCount,
            nameof(exploredCombinationCount));
        FeasibleResultCount = NonNegative(
            feasibleResultCount,
            nameof(feasibleResultCount));
        RetainedResultCount = NonNegative(
            retainedResultCount,
            nameof(retainedResultCount));
        FirstTerminator = TacticalCombatText.Defined(
            firstTerminator,
            nameof(firstTerminator));
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        Elapsed = elapsed;
        Caches = TacticalCombatText.CopyUnique(
            caches ?? [],
            item => item.StableKey,
            "cache diagnostic",
            nameof(caches));
        ValidateInvariant();
        Fingerprint = TacticalCombatText.Fingerprint(SemanticKey);
    }

    public TacticalSearchBounds Bounds { get; }

    public int CandidateUniverseCount { get; }

    public int RoleSupportedCount { get; }

    public int AdmittedCount { get; }

    public int RejectedCount { get; }

    public int UnsupportedCount { get; }

    public int IrrelevantCount { get; }

    public int DominatedCount { get; }

    public int SearchedOptionCount { get; }

    public int ExploredCombinationCount { get; }

    public int FeasibleResultCount { get; }

    public int RetainedResultCount { get; }

    public TacticalSearchTerminator FirstTerminator { get; }

    public TimeSpan Elapsed { get; }

    public ImmutableArray<TacticalCacheReuseDiagnostic> Caches { get; }

    public bool IsComplete =>
        FirstTerminator == TacticalSearchTerminator.None;

    public string Fingerprint { get; }

    internal string SemanticKey => string.Join('|',
        "TACTICAL_SEARCH_COVERAGE_V1",
        Bounds.StableKey,
        CandidateUniverseCount.ToString(CultureInfo.InvariantCulture),
        RoleSupportedCount.ToString(CultureInfo.InvariantCulture),
        AdmittedCount.ToString(CultureInfo.InvariantCulture),
        RejectedCount.ToString(CultureInfo.InvariantCulture),
        UnsupportedCount.ToString(CultureInfo.InvariantCulture),
        IrrelevantCount.ToString(CultureInfo.InvariantCulture),
        DominatedCount.ToString(CultureInfo.InvariantCulture),
        SearchedOptionCount.ToString(CultureInfo.InvariantCulture),
        ExploredCombinationCount.ToString(CultureInfo.InvariantCulture),
        FeasibleResultCount.ToString(CultureInfo.InvariantCulture),
        RetainedResultCount.ToString(CultureInfo.InvariantCulture),
        TacticalCombatText.EnumKey(FirstTerminator));

    private void ValidateInvariant()
    {
        if (CandidateUniverseCount
            != AdmittedCount
                + RejectedCount
                + UnsupportedCount
                + IrrelevantCount
                + DominatedCount)
        {
            throw new ArgumentException(
                "Candidate terminal counts must account for the universe exactly once.");
        }

        if (RoleSupportedCount > CandidateUniverseCount
            || AdmittedCount > RoleSupportedCount
            || SearchedOptionCount > AdmittedCount
            || SearchedOptionCount > Bounds.MaximumOptions
            || ExploredCombinationCount
                > Bounds.MaximumExploredCombinations
            || RetainedResultCount > FeasibleResultCount
            || RetainedResultCount > Bounds.MaximumResults)
        {
            throw new ArgumentException(
                "Tactical search counts exceed their source counts or bounds.");
        }

        switch (FirstTerminator)
        {
            case TacticalSearchTerminator.None:
                if (SearchedOptionCount != AdmittedCount
                    || RetainedResultCount != FeasibleResultCount)
                {
                    throw new ArgumentException(
                        "A complete search must examine every admitted option and retain every feasible result.");
                }

                break;
            case TacticalSearchTerminator.OptionLimit:
                if (AdmittedCount <= SearchedOptionCount
                    || SearchedOptionCount != Bounds.MaximumOptions)
                {
                    throw new ArgumentException(
                        "An option-limited search must fill its option bound while eligible options remain.");
                }

                break;
            case TacticalSearchTerminator.ExplorationLimit:
                if (ExploredCombinationCount
                    != Bounds.MaximumExploredCombinations)
                {
                    throw new ArgumentException(
                        "An exploration-limited search must reach its exploration bound.");
                }

                break;
            case TacticalSearchTerminator.TimeLimit:
                if (Elapsed < Bounds.MaximumElapsed)
                {
                    throw new ArgumentException(
                        "A time-limited search must observe its elapsed bound.");
                }

                break;
            case TacticalSearchTerminator.ResultLimit:
                if (FeasibleResultCount <= RetainedResultCount
                    || RetainedResultCount != Bounds.MaximumResults)
                {
                    throw new ArgumentException(
                        "A result-limited search must fill its result bound while feasible results remain.");
                }

                break;
            case TacticalSearchTerminator.Cancelled:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(FirstTerminator));
        }
    }

    private static int NonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, null);
        }

        return value;
    }
}
