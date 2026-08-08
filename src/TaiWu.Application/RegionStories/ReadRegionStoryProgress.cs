namespace TaiWu.Application.RegionStories;

public sealed class ReadRegionStoryProgress(
    IRegionStoryProgressReader reader)
{
    public Task<RegionStoryProgressSnapshot> ExecuteAsync(
        RegionStoryProgressReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SaveFilePath);
        if (!Enum.IsDefined(request.Language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.Language,
                "Unknown Taiwu language.");
        }

        return reader.ReadAsync(request, cancellationToken);
    }
}
