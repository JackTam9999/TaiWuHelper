using TaiWu.Application.GameData;
using TaiWu.Application.Localization;

namespace TaiWu.Application.RegionStories;

public enum RegionStoryProgressStatus
{
    Unavailable,
    NotCompleted,
    InProgress,
    CompletedEndingUnrecorded,
    ProsperousEnding,
    FailingEnding
}

public sealed record RegionStoryProgressEntry(
    int OrganizationId,
    RegionStoryProgressStatus Status,
    int? CompletionDate,
    int? ActiveTaskChainId,
    int? CurrentTaskId,
    string? CurrentTaskTitle,
    string? CurrentTaskDescription,
    bool MainStoryFunctionUnlocked = false,
    bool PostStoryFunctionUpgraded = false);

public sealed record RegionStoryProgressWarning(
    string Code,
    string Message,
    int? OrganizationId = null);

public sealed record RegionStoryProgressSnapshot(
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset SaveLastWriteTimeUtc,
    string SaveSha256,
    IReadOnlyList<RegionStoryProgressEntry> Stories,
    IReadOnlyList<RegionStoryProgressWarning> Warnings);

public sealed record RegionStoryProgressReadRequest(
    string SaveFilePath,
    TaiwuLanguage Language = TaiwuLanguage.English);

public interface IRegionStoryProgressReader : IReadOnlyGameDataSource
{
    Task<RegionStoryProgressSnapshot> ReadAsync(
        RegionStoryProgressReadRequest request,
        CancellationToken cancellationToken = default);
}
