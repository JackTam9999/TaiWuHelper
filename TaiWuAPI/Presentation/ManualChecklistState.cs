namespace TaiWuAPI.Presentation;

public sealed class ManualChecklistState
{
    private readonly HashSet<string> _completed =
        new(StringComparer.Ordinal);

    public bool IsCompleted(string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        return _completed.Contains(reference);
    }

    public void Toggle(string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        if (!_completed.Add(reference))
        {
            _completed.Remove(reference);
        }
    }

    public void Synchronize(
        IReadOnlyList<ManualChecklistItemViewModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var current = items
            .Select(item => item.Reference)
            .ToHashSet(StringComparer.Ordinal);
        _completed.RemoveWhere(reference => !current.Contains(reference));
    }
}
