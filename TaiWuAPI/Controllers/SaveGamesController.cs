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
            var report = await readSaveGame.ExecuteAsync(
                new SaveGameReadRequest(
                    options.Value.DefaultSaveFilePath,
                    targetCharacterId),
                cancellationToken);
            return Ok(report);
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
}
