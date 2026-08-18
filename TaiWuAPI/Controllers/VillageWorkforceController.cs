using Microsoft.AspNetCore.Mvc;
using TaiWu.Application.VillageWorkforce;
using TaiWu.Domain.VillageWorkforce;
using TaiWuAPI.Contracts.VillageWorkforce;
using TaiWuAPI.Localization;

namespace TaiWuAPI.Controllers;

[ApiController]
[Route("api/village-workforce")]
public sealed class VillageWorkforceController(
    IVillageWorkforceSnapshotReader snapshotReader,
    IFindVillageWorkforce findVillageWorkforce) : ControllerBase
{
    [HttpGet("options")]
    [ProducesResponseType<VillageWorkforceDiscoveryResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<VillageWorkforceDiscoveryResponse>(
        StatusCodes.Status206PartialContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VillageWorkforceDiscoveryResponse>> Options(
        [FromQuery] string language = VillageWorkforceApiTokens.English,
        CancellationToken cancellationToken = default)
    {
        if (!VillageWorkforceResponseMapper.TryParseLanguage(
                language,
                out var parsedLanguage))
        {
            return InvalidRequest(
                VillageWorkforceApiLanguage.English,
                "VILLAGE_WORKFORCE_REQUEST_INVALID");
        }

        var read = await snapshotReader.ReadAsync(
                VillageWorkforceSnapshotReadRequest.Current,
                cancellationToken)
            .ConfigureAwait(false);
        var response = VillageWorkforceResponseMapper.MapDiscovery(
            read,
            parsedLanguage);
        return ToActionResult(response.Status, response, parsedLanguage);
    }

    [HttpGet("result")]
    [ProducesResponseType<VillageWorkforceResultResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<VillageWorkforceResultResponse>(
        StatusCodes.Status206PartialContent)]
    [ProducesResponseType<VillageWorkforceResultResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VillageWorkforceResultResponse>> Result(
        [FromQuery] VillageWorkforceApiQuery? query,
        CancellationToken cancellationToken = default)
    {
        if (!Valid(query, out var language, out var filter))
        {
            return InvalidRequest(
                language ?? VillageWorkforceApiLanguage.English,
                "VILLAGE_WORKFORCE_REQUEST_INVALID");
        }

        var request = new VillageWorkforceFinderRequest(
            new ShopManagerTargetIdentity(
                new ShopBuildingIdentity(
                    query!.AreaId,
                    query.BlockId,
                    query.BuildingBlockIndex),
                query.ManagerSlotIndex),
            new WorkforceObjectiveIdentity(
                WorkforceObjectiveKind.ShopManagerBaseLifeSkillQualification,
                query.ObjectiveVersion),
            filter!.Value,
            query.FirstComparisonCharacterId.HasValue
                ? new VillageWorkerIdentity(
                    query.FirstComparisonCharacterId.Value)
                : null,
            query.SecondComparisonCharacterId.HasValue
                ? new VillageWorkerIdentity(
                    query.SecondComparisonCharacterId.Value)
                : null,
            query.ProposedCharacterId.HasValue
                ? new VillageWorkerIdentity(query.ProposedCharacterId.Value)
                : null);
        var result = await findVillageWorkforce.ExecuteAsync(
                request,
                cancellationToken)
            .ConfigureAwait(false);
        var response = VillageWorkforceResponseMapper.Map(
            result,
            language!.Value);
        return ToActionResult(response.Status, response, language.Value);
    }

    private ActionResult<T> ToActionResult<T>(
        VillageWorkforceApiStatus status,
        T response,
        VillageWorkforceApiLanguage language) => status switch
        {
            VillageWorkforceApiStatus.Complete => Ok(response),
            VillageWorkforceApiStatus.Partial => StatusCode(
                StatusCodes.Status206PartialContent,
                response),
            VillageWorkforceApiStatus.InvalidComparison
                or VillageWorkforceApiStatus.InvalidProposal => StatusCode(
                    StatusCodes.Status400BadRequest,
                    response),
            VillageWorkforceApiStatus.InvalidRequest => ProblemResult(
                language,
                "VILLAGE_WORKFORCE_REQUEST_INVALID",
                StatusCodes.Status400BadRequest),
            VillageWorkforceApiStatus.SaveUnavailable
                or VillageWorkforceApiStatus.TargetNotFound => ProblemResult(
                    language,
                    FailureIdentity(response),
                    StatusCodes.Status404NotFound),
            VillageWorkforceApiStatus.ConflictingSources
                or VillageWorkforceApiStatus.ChangedRevision => ProblemResult(
                    language,
                    FailureIdentity(response),
                    StatusCodes.Status409Conflict),
            VillageWorkforceApiStatus.UnsupportedSourceVersion
                or VillageWorkforceApiStatus.UnsupportedRule => ProblemResult(
                    language,
                    FailureIdentity(response),
                    StatusCodes.Status422UnprocessableEntity),
            VillageWorkforceApiStatus.ReadFailed => ProblemResult(
                language,
                FailureIdentity(response),
                StatusCodes.Status500InternalServerError),
            _ => ProblemResult(
                language,
                "VILLAGE_WORKFORCE_FAILED",
                StatusCodes.Status500InternalServerError)
        };

    private ObjectResult InvalidRequest(
        VillageWorkforceApiLanguage language,
        string identity) => ProblemResult(
        language,
        identity,
        StatusCodes.Status400BadRequest);

    private ObjectResult ProblemResult(
        VillageWorkforceApiLanguage language,
        string identity,
        int statusCode)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Village workforce result",
            Detail = VillageWorkforceApiText.Failure(language, identity)
        };
        problem.Extensions["code"] = identity;
        return StatusCode(statusCode, problem);
    }

    private static string FailureIdentity<T>(T response) => response switch
    {
        VillageWorkforceDiscoveryResponse discovery =>
            discovery.Failure?.Identity ?? "VILLAGE_WORKFORCE_FAILED",
        VillageWorkforceResultResponse result =>
            result.Failure?.Identity ?? "VILLAGE_WORKFORCE_FAILED",
        _ => "VILLAGE_WORKFORCE_FAILED"
    };

    private static bool Valid(
        VillageWorkforceApiQuery? query,
        out VillageWorkforceApiLanguage? language,
        out WorkforceShortlistFilter? filter)
    {
        language = null;
        filter = null;
        if (query is null
            || !VillageWorkforceResponseMapper.TryParseLanguage(
                query.Language,
                out var parsedLanguage))
        {
            return false;
        }

        language = parsedLanguage;
        if (!VillageWorkforceResponseMapper.TryParseFilter(
                query.Filter,
                out var parsedFilter)
            || !string.Equals(
                query.Objective,
                VillageWorkforceApiTokens.Objective,
                StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(query.ObjectiveVersion)
            || query.ObjectiveVersion.Length > 20
            || query.AreaId < 0
            || query.BlockId < 0
            || query.BuildingBlockIndex < 0
            || query.ManagerSlotIndex is < 0 or > sbyte.MaxValue
            || query.FirstComparisonCharacterId <= 0
            || query.SecondComparisonCharacterId <= 0
            || query.ProposedCharacterId <= 0)
        {
            return false;
        }

        var hasFirst = query.FirstComparisonCharacterId.HasValue;
        var hasSecond = query.SecondComparisonCharacterId.HasValue;
        if (hasFirst != hasSecond
            || hasFirst
                && query.FirstComparisonCharacterId
                    == query.SecondComparisonCharacterId)
        {
            return false;
        }

        filter = parsedFilter;
        return true;
    }
}
