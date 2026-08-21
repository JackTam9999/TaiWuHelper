using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaiWu.Application.Localization;
using TaiWu.Application.Targets;
using TaiWuAPI.Configuration;
using TaiWuAPI.Contracts.Targets;

namespace TaiWuAPI.Controllers;

[ApiController]
[Route("api/targets")]
public sealed class TargetsController(
    IFindTargets findTargets,
    IOptions<SaveGameOptions> options) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TargetLookupResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TargetLookupResponse>> Find(
        [FromQuery] string? query,
        [FromQuery] int maxResults = 25,
        [FromQuery] TaiwuLanguage language = TaiwuLanguage.English,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Problem(
                detail: "query must contain a target name or character ID.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var result = await findTargets.ExecuteAsync(
                new FindTargetsRequest(
                    options.Value.DefaultSaveFilePath,
                    query,
                    maxResults,
                    language),
                cancellationToken);
            return Ok(Map(result));
        }
        catch (Exception exception)
            when (exception is ArgumentException
                or FileNotFoundException
                or InvalidDataException
                or OptionsValidationException)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static TargetLookupResponse Map(FindTargetsResult result)
    {
        return new TargetLookupResponse(
            result.Query,
            result.Status,
            result.TotalMatches,
            result.CapturedAtUtc,
            result.GameDataVersion,
            [.. result.Matches.Select(entry => new TargetLookupMatchResponse(
                    $"target:{entry.CharacterId}",
                    entry.CharacterId,
                    entry.DisplayName,
                    entry.Age,
                    entry.ConsummateLevel,
                    entry.Kind,
                    entry.TemplateId,
                    new TargetLocationResponse(
                        $"location:{entry.AreaId}:{entry.BlockId}",
                        entry.AreaId,
                        entry.BlockId,
                        entry.LocationDisplayName)))],
            [.. result.Warnings.Select((warning, index) =>
                    new TargetLookupWarningResponse(
                        $"warning:target-lookup:{warning.Code}:{index + 1}",
                        warning.Code,
                        warning.Message))]);
    }
}
