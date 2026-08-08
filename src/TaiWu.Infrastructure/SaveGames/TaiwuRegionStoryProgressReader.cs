using GameData.Domains;
using GameData.Domains.Organization;
using GameData.Domains.TaiwuEvent;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using TaiWu.Application.RegionStories;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed partial class TaiwuRegionStoryProgressReader(
    TaiwuArchiveReadSession readSession,
    TaiwuGameTextResolver textResolver,
    TimeProvider? timeProvider = null) : IRegionStoryProgressReader
{
    private static readonly MethodInfo EventArgumentIntGetter =
        typeof(EventArgBox).GetMethod(
            "Get",
            [typeof(string), typeof(int).MakeByRefType()])
        ?? throw new InvalidDataException(
            "The installed GameData integer event-argument reader is "
            + "unavailable.");

    private static readonly MethodInfo EventArgumentBoolGetter =
        typeof(EventArgBox).GetMethod(
            "Get",
            [typeof(string), typeof(bool).MakeByRefType()])
        ?? throw new InvalidDataException(
            "The installed GameData Boolean event-argument reader is "
            + "unavailable.");

    private const sbyte ShixiangOrganizationId = 6;
    private const string ShixiangEndingChoiceKey =
        "ConchShip_PresetKey_SelectGoodEnd";

    private readonly TimeProvider _timeProvider = timeProvider
        ?? TimeProvider.System;

    public Task<RegionStoryProgressSnapshot> ReadAsync(
        RegionStoryProgressReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return readSession.ReadAsync(
            request.SaveFilePath,
            (context, token) => Project(
                context,
                textResolver.CreateContext(
                    request.SaveFilePath,
                    request.Language),
                token),
            cancellationToken);
    }

    private RegionStoryProgressSnapshot Project(
        TaiwuArchiveReadContext readContext,
        TaiwuGameTextContext text,
        CancellationToken cancellationToken)
    {
        List<RegionStoryProgressEntry> stories = [];
        List<RegionStoryProgressWarning> warnings = [];

        for (sbyte organizationId = 1;
             organizationId <= 15;
             organizationId++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                stories.Add(MapStory(organizationId, text));
            }
            catch (Exception exception)
                when (exception is ArgumentException
                    or InvalidOperationException
                    or IndexOutOfRangeException
                    or NullReferenceException)
            {
                stories.Add(new RegionStoryProgressEntry(
                    organizationId,
                    RegionStoryProgressStatus.Unavailable,
                    CompletionDate: null,
                    ActiveTaskChainId: null,
                    CurrentTaskId: null,
                    CurrentTaskTitle: null,
                    CurrentTaskDescription: null));
                warnings.Add(new RegionStoryProgressWarning(
                    "REGION_STORY_UNAVAILABLE",
                    $"Organization {organizationId} could not be read: "
                    + exception.Message,
                    organizationId));
            }
        }

        return new RegionStoryProgressSnapshot(
            _timeProvider.GetUtcNow(),
            readContext.SourceFingerprint.LastWriteTimeUtc,
            readContext.SourceFingerprint.Sha256,
            stories,
            warnings);
    }

    private static RegionStoryProgressEntry MapStory(
        sbyte organizationId,
        TaiwuGameTextContext text)
    {
        var story = Config.Organization.Instance[organizationId]
            .SectMainStory;
        var arguments = DomainManager.Extra
            .GetSectMainStoryEventArgBox(organizationId);
        var hasProsperousEnding = TryGetInt(
            arguments,
            story.GoodEndDateKey,
            out int prosperousDate);
        var hasFailingEnding = TryGetInt(
            arguments,
            story.BadEndDateKey,
            out int failingDate);
        // Valid saves can lose the ending-date argument while retaining the
        // sect function granted by story completion. The function status is
        // therefore the stronger persistent fallback completion evidence.
        var mainStoryFunctionUnlocked = DomainManager.Organization
            .GetSectFunctionStatus(
                organizationId,
                SectFunctionStatuses.SectFunctionStatusType
                    .SpecialInteractionUnlocked);
        var postStoryFunctionUpgraded = DomainManager.Organization
            .GetSectFunctionStatus(
                organizationId,
                SectFunctionStatuses.SectFunctionStatusType
                    .UpgradedInteractionUnlocked);
        // Shixiang retains its selected route even when its ending date is
        // absent, allowing the recovered completion to keep the ending kind.
        bool? inferredProsperousEnding = organizationId
                == ShixiangOrganizationId
            && TryGetBool(
                arguments,
                ShixiangEndingChoiceKey,
                out var selectedProsperousEnding)
                ? selectedProsperousEnding
                : null;

        int? activeTaskChainId = null;
        int? currentTaskId = null;
        foreach (var taskChainId in story.TaskChains ?? [])
        {
            if (!DomainManager.World.IsExtraTaskChainInProgress(taskChainId))
            {
                continue;
            }

            activeTaskChainId = taskChainId;
            var taskId = DomainManager.World
                .GetExtraTaskChainCurrentTask(taskChainId);
            if (taskId >= 0)
            {
                currentTaskId = taskId;
            }

            break;
        }

        var status = Classify(
            hasProsperousEnding,
            hasFailingEnding,
            mainStoryFunctionUnlocked,
            inferredProsperousEnding,
            activeTaskChainId.HasValue);
        int? completionDate = status switch
        {
            RegionStoryProgressStatus.ProsperousEnding
                when hasProsperousEnding => prosperousDate,
            RegionStoryProgressStatus.FailingEnding
                when hasFailingEnding => failingDate,
            _ => null
        };

        return new RegionStoryProgressEntry(
            organizationId,
            status,
            completionDate,
            activeTaskChainId,
            currentTaskId,
            currentTaskId.HasValue
                ? ResolveTaskText(text, "TaskTitle", currentTaskId.Value)
                : null,
            currentTaskId.HasValue
                ? ResolveTaskDescription(text, currentTaskId.Value)
                : null,
            mainStoryFunctionUnlocked,
            postStoryFunctionUpgraded);
    }

    private static bool TryGetInt(
        EventArgBox arguments,
        string key,
        out int value)
    {
        object?[] invocationArguments = [key, 0];
        var found = EventArgumentIntGetter.Invoke(
            arguments,
            invocationArguments) as bool? == true;
        value = invocationArguments[1] is int parsed ? parsed : 0;
        return found;
    }

    private static bool TryGetBool(
        EventArgBox arguments,
        string key,
        out bool value)
    {
        object?[] invocationArguments = [key, false];
        var found = EventArgumentBoolGetter.Invoke(
            arguments,
            invocationArguments) as bool? == true;
        value = invocationArguments[1] is bool parsed && parsed;
        return found;
    }

    internal static RegionStoryProgressStatus Classify(
        bool hasProsperousEnding,
        bool hasFailingEnding,
        bool mainStoryFunctionUnlocked,
        bool? inferredProsperousEnding,
        bool hasActiveTask)
    {
        if (hasProsperousEnding)
        {
            return RegionStoryProgressStatus.ProsperousEnding;
        }

        if (hasFailingEnding)
        {
            return RegionStoryProgressStatus.FailingEnding;
        }

        if (mainStoryFunctionUnlocked)
        {
            return inferredProsperousEnding switch
            {
                true => RegionStoryProgressStatus.ProsperousEnding,
                false => RegionStoryProgressStatus.FailingEnding,
                null => RegionStoryProgressStatus
                    .CompletedEndingUnrecorded
            };
        }

        return hasActiveTask
            ? RegionStoryProgressStatus.InProgress
            : RegionStoryProgressStatus.NotCompleted;
    }

    private static string ResolveTaskDescription(
        TaiwuGameTextContext text,
        int taskId)
    {
        var value = ResolveTaskText(text, "TaskDescription", taskId);
        var arguments = FindTaskArguments(taskId);
        for (var index = 0; index < arguments.Count; index++)
        {
            value = value.Replace(
                $"{{{index}}}",
                arguments[index],
                StringComparison.Ordinal);
        }

        return UnresolvedPlaceholderPattern()
            .Replace(
                ColorTagPattern().Replace(value, string.Empty),
                "…")
            .Replace("\\n", Environment.NewLine, StringComparison.Ordinal);
    }

    private static string ResolveTaskText(
        TaiwuGameTextContext text,
        string field,
        int taskId)
    {
        var key = string.Create(
            CultureInfo.InvariantCulture,
            $"{field}_{taskId}");
        var value = text.Resolve("TaskInfo", key);
        return string.Equals(value, key, StringComparison.Ordinal)
            ? string.Empty
            : value;
    }

    private static IReadOnlyList<string> FindTaskArguments(int taskId)
    {
        try
        {
            foreach (var task in DomainManager.World.GetSortedTaskList())
            {
                if (task.InnerTaskData.TaskInfoId == taskId)
                {
                    return task.StringArray ?? [];
                }
            }
        }
        catch (Exception exception)
            when (exception is InvalidOperationException
                or NullReferenceException)
        {
        }

        return [];
    }

    [GeneratedRegex("<color=#[^>]+>|</color>", RegexOptions.IgnoreCase)]
    private static partial Regex ColorTagPattern();

    [GeneratedRegex("\\{\\d+\\}")]
    private static partial Regex UnresolvedPlaceholderPattern();
}
