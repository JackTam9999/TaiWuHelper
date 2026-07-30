using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<SaveGameReport>> ReadConfigured(
        [FromQuery] int? targetCharacterId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await Read(
                options.Value.DefaultSaveFilePath,
                targetCharacterId,
                cancellationToken);
        }
        catch (OptionsValidationException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
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
        catch (InvalidDataException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
