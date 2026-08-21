namespace TaiWuAPI.Presentation;

internal sealed class TacticalCandidatePagingState
{
    private const int PageSize = 25;
    private readonly Dictionary<TacticalCandidatePresentationGroup, int>
        _visibleCounts = [];
    private string? _resultIdentity;
    private bool _initialized;

    internal void ResetFor(string? resultIdentity)
    {
        if (_initialized
            && string.Equals(
                _resultIdentity,
                resultIdentity,
                StringComparison.Ordinal))
        {
            return;
        }

        _visibleCounts.Clear();
        _resultIdentity = resultIdentity;
        _initialized = true;
    }

    internal IReadOnlyList<TacticalCandidateViewModel> Visible(
        TacticalCandidateGroupViewModel group)
    {
        ArgumentNullException.ThrowIfNull(group);
        var count = _visibleCounts.GetValueOrDefault(group.Group, PageSize);
        return group.Candidates.Take(count).ToArray();
    }

    internal void ShowMore(TacticalCandidateGroupViewModel group)
    {
        ArgumentNullException.ThrowIfNull(group);
        var current = _visibleCounts.GetValueOrDefault(group.Group, PageSize);
        _visibleCounts[group.Group] = Math.Min(
            checked(current + PageSize),
            group.Candidates.Count);
    }
}
