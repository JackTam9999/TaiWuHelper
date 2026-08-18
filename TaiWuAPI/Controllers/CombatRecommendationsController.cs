using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.TargetObservations;
using TaiWuAPI.Configuration;
using TaiWuAPI.Contracts.CombatRecommendations;

namespace TaiWuAPI.Controllers;

[ApiController]
[Route("api/combat-recommendations")]
public sealed class CombatRecommendationsController(
    IRecommendCombatLoadout recommendCombatLoadout,
    IOptions<SaveGameOptions> options,
    ITargetObservationRecommendationWorkflow? targetObservationWorkflow = null)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CombatRecommendationResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
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
            var recommendationRequest = new RecommendCombatLoadoutRequest(
                options.Value.DefaultSaveFilePath,
                request.TargetCharacterId.Value,
                request.Objective,
                observation,
                request.Language,
                request.TargetObservation?.ToApplication());
            var recommendation = request.TargetObservation is null
                ? await recommendCombatLoadout.ExecuteAsync(
                    recommendationRequest,
                    cancellationToken)
                : await RequiredTargetObservationWorkflow().ExecuteAsync(
                    recommendationRequest,
                    cancellationToken);
            return Ok(CombatRecommendationResponseMapper.Map(
                recommendation,
                request.Language));
        }
        catch (TargetObservationResolutionException exception)
        {
            return TargetObservationProblem(
                exception.Status.ToString(),
                exception.SelectionIndex,
                exception.Candidates.Select(
                    CombatRecommendationResponseMapper.MapCandidate));
        }
        catch (CombatSnapshotTargetNotFoundException)
        {
            return Problem(
                type: "urn:taiwu-helper:combat-recommendation:target-not-found",
                title: "Target character was not found.",
                detail: "The requested target does not exist in the configured save.",
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (ArgumentException)
            when (request.TargetObservation is not null)
        {
            return TargetObservationProblem(
                "InvalidObservation",
                selectionIndex: null,
                candidates: []);
        }
        catch (Exception exception)
            when (request.TargetObservation is not null
                && exception is FileNotFoundException
                    or InvalidDataException
                    or OptionsValidationException)
        {
            return TargetObservationProblem(
                "SnapshotUnavailable",
                selectionIndex: null,
                candidates: []);
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

    private ITargetObservationRecommendationWorkflow
        RequiredTargetObservationWorkflow() =>
        targetObservationWorkflow
        ?? throw new InvalidOperationException(
            "Target-observation workflow is unavailable.");

    private ActionResult<CombatRecommendationResponse>
        TargetObservationProblem(
            string code,
            int? selectionIndex,
            IEnumerable<TargetObservationProblemCandidateResponse> candidates)
    {
        var problem = new ProblemDetails
        {
            Type = "urn:taiwu-helper:target-observation:" + code.ToLowerInvariant(),
            Title = "Target observation could not be accepted.",
            Detail = "Review the selected visible skill and catalogue "
                + "confirmation, then try again.",
            Status = StatusCodes.Status400BadRequest
        };
        problem.Extensions["code"] = code;
        if (selectionIndex is not null)
        {
            problem.Extensions["selectionIndex"] = selectionIndex.Value;
        }

        problem.Extensions["candidates"] = candidates.ToArray();
        return BadRequest(problem);
    }
}
