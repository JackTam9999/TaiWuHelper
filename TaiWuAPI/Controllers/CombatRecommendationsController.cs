using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaiWu.Application.CombatRecommendations;
using TaiWuAPI.Configuration;
using TaiWuAPI.Contracts.CombatRecommendations;

namespace TaiWuAPI.Controllers;

[ApiController]
[Route("api/combat-recommendations")]
public sealed class CombatRecommendationsController(
    IRecommendCombatLoadout recommendCombatLoadout,
    IOptions<SaveGameOptions> options) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CombatRecommendationResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CombatRecommendationResponse>> Recommend(
        [FromBody] CombatRecommendationApiRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.TargetCharacterId is null or <= 0)
        {
            return Problem(
                detail: "targetCharacterId is required and must be greater "
                + "than zero.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!Enum.IsDefined(request.Objective))
        {
            return Problem(
                detail: "objective must be Safe, Balanced, or Aggressive.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var observation = request.CurrentScreenObservation?.ToDomain();
            var recommendation = await recommendCombatLoadout.ExecuteAsync(
                new RecommendCombatLoadoutRequest(
                    options.Value.DefaultSaveFilePath,
                    request.TargetCharacterId.Value,
                    request.Objective,
                    observation),
                cancellationToken);
            return Ok(CombatRecommendationResponseMapper.Map(recommendation));
        }
        catch (Exception exception)
            when (exception is ArgumentException
                or FileNotFoundException
                or InvalidDataException)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
