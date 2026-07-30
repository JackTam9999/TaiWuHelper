namespace TaiWu.Application.Targets;

public sealed class FindTargets(ITargetLookupReader reader) : IFindTargets
{
    public async Task<FindTargetsResult> ExecuteAsync(
        FindTargetsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var snapshot = await reader.ReadAsync(
            new TargetLookupReadRequest(request.SaveFilePath),
            cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var matches = Match(snapshot.Entries, request.Query);
        var status = matches.Length switch
        {
            0 => TargetLookupStatus.NotFound,
            1 => TargetLookupStatus.Found,
            _ => TargetLookupStatus.Ambiguous
        };
        return new FindTargetsResult(
            request.Query,
            status,
            matches.Length,
            matches.Take(request.MaxResults),
            snapshot);
    }

    private static TargetLookupEntry[] Match(
        IEnumerable<TargetLookupEntry> entries,
        string query)
    {
        if (int.TryParse(query, out var characterId)
            && characterId > 0)
        {
            return
            [
                .. entries.Where(entry =>
                    entry.CharacterId == characterId)
            ];
        }

        return
        [
            .. entries
                .Where(entry => entry.DisplayName.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(entry => string.Equals(
                    entry.DisplayName,
                    query,
                    StringComparison.OrdinalIgnoreCase))
                .ThenBy(
                    entry => entry.DisplayName,
                    StringComparer.Ordinal)
                .ThenBy(entry => entry.AreaId)
                .ThenBy(entry => entry.BlockId)
                .ThenBy(entry => entry.CharacterId)
        ];
    }
}
