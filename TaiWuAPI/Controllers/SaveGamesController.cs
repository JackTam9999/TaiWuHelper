using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using TaiWu.Application.SaveGames;
using TaiWu.Domain.SaveGames;
using TaiWuAPI.Configuration;

namespace TaiWuAPI.Controllers;

[ApiController]
[Route("api/save-games")]
public sealed class SaveGamesController(
    ReadSaveGame readSaveGame,
    IOptions<SaveGameOptions> options) : ControllerBase
{
    [HttpGet("read")]
    [ProducesResponseType<SaveGameReport>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<SaveGameReport>> ReadConfigured(
        [FromQuery] int? targetCharacterId,
        CancellationToken cancellationToken) =>
        Read(
            options.Value.DefaultSaveFilePath,
            targetCharacterId,
            cancellationToken);

    [HttpPost("read")]
    [ProducesResponseType<SaveGameReport>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<SaveGameReport>> ReadOverride(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
        ReadSaveGameBody? body,
        CancellationToken cancellationToken)
    {
        var saveFilePath = string.IsNullOrWhiteSpace(body?.SaveFilePath)
            ? options.Value.DefaultSaveFilePath
            : body.SaveFilePath;

        return Read(
            saveFilePath,
            body?.TargetCharacterId,
            cancellationToken);
    }

    private async Task<ActionResult<SaveGameReport>> Read(
        string saveFilePath,
        int? targetCharacterId,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await readSaveGame.ExecuteAsync(
                new SaveGameReadRequest(saveFilePath, targetCharacterId),
                cancellationToken);
            return Ok(report);
        }
        catch (ArgumentException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (FileNotFoundException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}

public sealed record ReadSaveGameBody(
    string? SaveFilePath = null,
    int? TargetCharacterId = null);
