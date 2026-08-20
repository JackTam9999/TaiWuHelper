using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.TargetObservations;
using TaiWu.Application.TacticalCombat;
using TaiWuAPI.Configuration;
using TaiWuAPI.Contracts.CombatRecommendations;

namespace TaiWuAPI.Controllers;

[ApiController]
[Route("api/combat-recommendations")]
public sealed class CombatRecommendationsController(
    IRecommendCombatLoadout recommendCombatLoadout,
    IOptions<SaveGameOptions> options,
    ITargetObservationRecommendationWorkflow? targetObservationWorkflow = null,
    IRecommendTacticalCombat? recommendTacticalCombat = null)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CombatRecommendationResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<CombatRecommendationResponse>(
        StatusCodes.Status206PartialContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
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

        if (request.TacticalPlanning is not null
            && request.TargetObservation is not null)
        {
            return TacticalProblem(
                "INCOMPATIBLE_OBSERVATION_MODES",
                StatusCodes.Status400BadRequest);
        }

        try
        {
            var observation = request.CurrentScreenObservation?.ToDomain();
            if (request.TacticalPlanning is not null)
            {
                var tacticalRequest = request.TacticalPlanning.ToApplication(
                    options.Value.DefaultSaveFilePath,
                    request.TargetCharacterId.Value,
                    request.Objective,
                    observation,
                    request.Language);
                var tacticalResult = await RequiredTacticalWorkflow()
                    .ExecuteAsync(tacticalRequest, cancellationToken);
                if (TacticalFailure(tacticalResult.Status))
                {
                    return TacticalProblem(
                        tacticalResult.ReasonIdentity,
                        TacticalFailureStatus(tacticalResult.Status));
                }

                var response = CombatRecommendationResponseMapper.Map(
                    tacticalResult,
                    request.Language);
                return tacticalResult.Status is
                        TacticalCombatRecommendationStatus.PartialEvidence
                        or TacticalCombatRecommendationStatus.SearchTruncated
                    ? StatusCode(StatusCodes.Status206PartialContent, response)
                    : Ok(response);
            }

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
            when (request.TacticalPlanning is not null
                && exception is ArgumentException
                    or OptionsValidationException)
        {
            return TacticalProblem(
                "INVALID_TACTICAL_REQUEST",
                StatusCodes.Status400BadRequest);
        }
        catch (Exception exception)
            when (request.TacticalPlanning is not null
                && exception is not OperationCanceledException)
        {
            return TacticalProblem(
                "TACTICAL_BOUNDARY_FAILURE",
                StatusCodes.Status500InternalServerError);
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

    private IRecommendTacticalCombat RequiredTacticalWorkflow() =>
        recommendTacticalCombat
        ?? throw new InvalidOperationException(
            "Tactical recommendation workflow is unavailable.");

    private ActionResult<CombatRecommendationResponse> TacticalProblem(
        string code,
        int statusCode)
    {
        var problem = new ProblemDetails
        {
            Type = "urn:taiwu-helper:tactical-combat:"
                + code.ToLowerInvariant().Replace('_', '-'),
            Title = statusCode >= 500
                ? "Tactical planning is temporarily unavailable."
                : "Tactical planning request could not be accepted.",
            Detail = statusCode >= 500
                ? "The helper could not produce a coherent tactical result."
                : "Review the stable observation tokens and search bounds, then try again.",
            Status = statusCode
        };
        problem.Extensions["code"] = code;
        return StatusCode(statusCode, problem);
    }

    private static bool TacticalFailure(
        TacticalCombatRecommendationStatus status) => status is
        TacticalCombatRecommendationStatus.SourceFailure
        or TacticalCombatRecommendationStatus.EvidenceFailure
        or TacticalCombatRecommendationStatus.ContextFailure
        or TacticalCombatRecommendationStatus.RuleFailure
        or TacticalCombatRecommendationStatus.SearchFailure
        or TacticalCombatRecommendationStatus.ScoringFailure
        or TacticalCombatRecommendationStatus.PlanningFailure
        or TacticalCombatRecommendationStatus.UnexpectedFailure;

    private static int TacticalFailureStatus(
        TacticalCombatRecommendationStatus status) => status is
        TacticalCombatRecommendationStatus.SourceFailure
        or TacticalCombatRecommendationStatus.EvidenceFailure
        or TacticalCombatRecommendationStatus.ContextFailure
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

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
