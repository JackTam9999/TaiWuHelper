using Microsoft.AspNetCore.Mvc;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.Localization;
using TaiWuAPI.Contracts.CompanionCandidates;
using TaiWuAPI.Localization;

namespace TaiWuAPI.Controllers;

[ApiController]
[Route("api/companion-candidates")]
public sealed class CompanionCandidatesController(
    IFindCompanionCandidates findCompanionCandidates) : ControllerBase
{
    [HttpGet("roles")]
    [ProducesResponseType<CompanionRoleDiscoveryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<CompanionRoleDiscoveryResponse> Roles(
        [FromQuery] TaiwuLanguage language = TaiwuLanguage.English)
    {
        if (!Enum.IsDefined(language))
        {
            return InvalidRequest(
                "LANGUAGE_INVALID",
                "The requested language is invalid.");
        }

        return Ok(CompanionFinderResponseMapper.MapRoles(language));
    }

    [HttpPost("find")]
    [ProducesResponseType<CompanionFinderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CompanionFinderResponse>(StatusCodes.Status206PartialContent)]
    [ProducesResponseType<CompanionFinderResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CompanionFinderResponse>> Find(
        [FromBody] CompanionFinderApiRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (!Valid(request))
        {
            return InvalidRequest(
                "COMPANION_FINDER_REQUEST_INVALID",
                request is not null && Enum.IsDefined(request.Language)
                    ? CompanionFinderApiText.Failure(
                        request.Language,
                        "COMPANION_FINDER_REQUEST_INVALID")
                    : "The companion-finder request is invalid.");
        }

        var result = await findCompanionCandidates.ExecuteAsync(
                request!.ToApplication(),
                cancellationToken)
            .ConfigureAwait(false);
        var response = CompanionFinderResponseMapper.Map(result, request.Language);
        return result.Status switch
        {
            CompanionFinderStatus.Complete or CompanionFinderStatus.Empty => Ok(response),
            CompanionFinderStatus.Partial => StatusCode(
                StatusCodes.Status206PartialContent,
                response),
            CompanionFinderStatus.InvalidComparison => StatusCode(
                StatusCodes.Status400BadRequest,
                response),
            CompanionFinderStatus.InvalidRequest
                or CompanionFinderStatus.UnknownRole
                or CompanionFinderStatus.UnsupportedRoleVersion => ProblemResult(
                    result,
                    request.Language,
                    StatusCodes.Status400BadRequest),
            CompanionFinderStatus.SaveUnavailable => ProblemResult(
                result,
                request.Language,
                StatusCodes.Status404NotFound),
            CompanionFinderStatus.ChangedRevision => ProblemResult(
                result,
                request.Language,
                StatusCodes.Status409Conflict),
            CompanionFinderStatus.UnsupportedSourceVersion => ProblemResult(
                result,
                request.Language,
                StatusCodes.Status422UnprocessableEntity),
            CompanionFinderStatus.ReadFailed or CompanionFinderStatus.Failed => ProblemResult(
                result,
                request.Language,
                StatusCodes.Status500InternalServerError),
            _ => ProblemResult(
                result,
                request.Language,
                StatusCodes.Status500InternalServerError)
        };
    }

    private static bool Valid(CompanionFinderApiRequest? request)
    {
        if (request is null
            || string.IsNullOrWhiteSpace(request.RoleIdentity)
            || request.RoleIdentity.Length > 160
            || string.IsNullOrWhiteSpace(request.RoleVersion)
            || request.RoleVersion.Length > 40
            || !Enum.IsDefined(request.DisciplineDomain)
            || request.DisciplineType < 0
            || !Enum.IsDefined(request.Filter)
            || !Enum.IsDefined(request.Language))
        {
            return false;
        }

        var hasFirst = request.FirstComparisonCharacterId.HasValue;
        var hasSecond = request.SecondComparisonCharacterId.HasValue;
        return hasFirst == hasSecond
            && (!hasFirst
                || request.FirstComparisonCharacterId > 0
                && request.SecondComparisonCharacterId > 0
                && request.FirstComparisonCharacterId
                    != request.SecondComparisonCharacterId);
    }

    private ObjectResult ProblemResult(
        CompanionFinderResult result,
        TaiwuLanguage language,
        int statusCode) => ProblemResult(
            result.FailureIdentity ?? "COMPANION_FINDER_FAILED",
            language,
            statusCode);

    private ObjectResult ProblemResult(
        string identity,
        TaiwuLanguage language,
        int statusCode)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Companion finder result",
            Detail = CompanionFinderApiText.Failure(language, identity)
        };
        problem.Extensions["code"] = identity;
        return StatusCode(statusCode, problem);
    }

    private ObjectResult InvalidRequest(string identity, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid companion-finder request",
            Detail = detail
        };
        problem.Extensions["code"] = identity;
        return BadRequest(problem);
    }
}
