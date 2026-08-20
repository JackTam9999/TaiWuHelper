using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.LoadoutComparisons;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public sealed class RecommendTacticalCombat(
    ICombatSnapshotReader reader,
    TimeProvider timeProvider,
    ITacticalCombatRecommendationFaultReporter faultReporter)
    : IRecommendTacticalCombat
{
    public async Task<TacticalCombatRecommendationResult> ExecuteAsync(
        TacticalCombatRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var work = new WorkTracker();
        var stage = PipelineStage.Source;
        CombatLoadoutRecommendation? legacy = null;
        Domain.LoadoutComparisons.LoadoutComparison? comparison = null;
        TacticalExecutionContextReadResult? contextRead = null;
        TacticalCombatRuleResolution? resolution = null;
        TacticalCandidateDiscoveryResult? discovery = null;
        TacticalLoadoutSearchResult? search = null;
        TacticalCombatScoringResult? scoring = null;
        TacticalCompiledCombatPlan? plan = null;

        try
        {
            work.SnapshotReads++;
            var snapshot = await reader.ReadAsync(
                request.SearchRequest.ContextRequest.SnapshotRequest,
                cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (snapshot.Player.CharacterId != request.PlayerCharacterId
                || snapshot.Target.CharacterId != request.TargetCharacterId)
            {
                return Result(
                    TacticalCombatRecommendationStatus.SourceFailure,
                    "REQUESTED_CHARACTER_IDENTITY_MISMATCH");
            }

            stage = PipelineStage.Context;
            work.LegacyRecommendationBuilds++;
            legacy = RecommendCombatLoadout.Build(
                snapshot,
                request.Policy,
                targetObservation: null,
                cancellationToken);
            work.ComparisonBuilds++;
            comparison = CombatLoadoutComparisonBuilder.Build(legacy);
            cancellationToken.ThrowIfCancellationRequested();

            stage = PipelineStage.Rule;
            work.RuleResolutions++;
            var contextRequest = request.SearchRequest.ContextRequest;
            var gameDataVersion = snapshot.Metadata.GameDataVersion.IsAvailable
                ? snapshot.Metadata.GameDataVersion.Value
                : TacticalContextGameDataVersions.Unavailable;
            resolution = VerifiedTacticalCombatRuleSets.HistoricalMagicSound
                .Resolve(
                    gameDataVersion,
                    contextRequest.TargetGoalCodes,
                    contextRequest.Evidence);

            stage = PipelineStage.Context;
            work.ContextProjections++;
            var context = TacticalExecutionContextProjector.Project(
                snapshot,
                resolution,
                contextRequest.Proposal,
                cancellationToken);
            contextRead = new TacticalExecutionContextReadResult(
                context,
                snapshot.Metadata.CapturedAtUtc,
                LatestObservation(snapshot));

            if (!resolution.IsResolved)
            {
                return Result(
                    TacticalCombatRecommendationStatus.UnsupportedChain,
                    "UNSUPPORTED_GAME_DATA_RULE_CHAIN");
            }

            stage = PipelineStage.Discovery;
            work.CandidateDiscoveries++;
            discovery = TacticalCandidateDiscovery.Discover(
                snapshot.Player,
                context,
                resolution,
                request.SearchRequest.DiscoveryLimits,
                cancellationToken);

            var searchRequest = new TacticalLoadoutSearchRequest(
                snapshot.Player,
                context,
                resolution,
                discovery,
                request.SearchRequest.Bounds,
                request.SearchRequest.IrrelevanceProofs,
                request.SearchRequest.DominanceProofs);
            stage = PipelineStage.Search;
            work.Searches++;
            search = TacticalLoadoutSearch.Search(
                searchRequest,
                timeProvider,
                cancellationToken);

            var scoringRequest = new TacticalCombatScoringRequest(
                request.Policy,
                searchRequest,
                search,
                request.LayeringProofs,
                request.TriggerObservations,
                request.FinishProofs);
            stage = PipelineStage.Scoring;
            work.Scores++;
            scoring = TacticalCombatScorer.Score(
                scoringRequest,
                cancellationToken);

            var selected = scoring.RankedCandidates.FirstOrDefault(item =>
                !item.Candidate.SelectedCandidates.IsEmpty);
            if (selected is not null)
            {
                stage = PipelineStage.Planning;
                work.PlanCompilations++;
                plan = TacticalCombatPlanCompiler.Compile(
                    new TacticalPlanCompilationRequest(
                        scoringRequest,
                        scoring,
                        selected.Candidate.StableKey),
                    cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!search.IsComplete)
            {
                return Result(
                    TacticalCombatRecommendationStatus.SearchTruncated,
                    "BOUNDED_SEARCH_TRUNCATED");
            }

            if (HasPartialEvidence(resolution))
            {
                return Result(
                    TacticalCombatRecommendationStatus.PartialEvidence,
                    "TACTICAL_EVIDENCE_PARTIAL");
            }

            return plan is null
                ? Result(
                    TacticalCombatRecommendationStatus.NoCandidate,
                    "NO_NONEMPTY_TACTICAL_LOADOUT")
                : Result(
                    TacticalCombatRecommendationStatus.Success,
                    "TACTICAL_PLAN_COMPILED");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (IsSourceFailure(exception, stage))
        {
            return Result(
                TacticalCombatRecommendationStatus.SourceFailure,
                "SNAPSHOT_SOURCE_FAILURE");
        }
        catch (ArgumentException) when (stage == PipelineStage.Rule)
        {
            return Result(
                TacticalCombatRecommendationStatus.EvidenceFailure,
                "TACTICAL_EVIDENCE_REJECTED");
        }
        catch (InvalidOperationException) when (stage == PipelineStage.Rule)
        {
            return Result(
                TacticalCombatRecommendationStatus.RuleFailure,
                "TACTICAL_RULE_RESOLUTION_FAILED");
        }
        catch (Exception exception) when (
            IsStageFailure(exception, stage, PipelineStage.Context))
        {
            return Result(
                TacticalCombatRecommendationStatus.ContextFailure,
                "TACTICAL_CONTEXT_PROJECTION_FAILED");
        }
        catch (Exception exception) when (
            IsStageFailure(exception, stage, PipelineStage.Discovery))
        {
            return Result(
                exception is InvalidOperationException
                    ? TacticalCombatRecommendationStatus.RuleFailure
                    : TacticalCombatRecommendationStatus.ContextFailure,
                exception is InvalidOperationException
                    ? "TACTICAL_DISCOVERY_RULE_FAILURE"
                    : "TACTICAL_DISCOVERY_CONTEXT_FAILURE");
        }
        catch (Exception exception) when (
            IsStageFailure(exception, stage, PipelineStage.Search))
        {
            return Result(
                TacticalCombatRecommendationStatus.SearchFailure,
                "TACTICAL_SEARCH_FAILED");
        }
        catch (Exception exception) when (
            IsStageFailure(exception, stage, PipelineStage.Scoring))
        {
            return Result(
                TacticalCombatRecommendationStatus.ScoringFailure,
                "TACTICAL_SCORING_FAILED");
        }
        catch (Exception exception) when (
            IsStageFailure(exception, stage, PipelineStage.Planning))
        {
            return Result(
                TacticalCombatRecommendationStatus.PlanningFailure,
                "TACTICAL_PLAN_COMPILATION_FAILED");
        }
        catch (Exception exception)
        {
            try
            {
                faultReporter.Report(
                    exception,
                    StageIdentity(stage));
            }
            catch
            {
                // Reporting must never make the safe client boundary fail.
            }

            return Result(
                TacticalCombatRecommendationStatus.UnexpectedFailure,
                "UNEXPECTED_TACTICAL_RECOMMENDATION_FAILURE");
        }

        TacticalCombatRecommendationResult Result(
            TacticalCombatRecommendationStatus status,
            string reasonIdentity)
        {
            var identity = contextRead is null || resolution is null
                ? null
                : new TacticalCombatRecommendationIdentity(
                    contextRead.Context.SourceRevisionFingerprint,
                    plan?.ObservationRevisionFingerprint
                        ?? contextRead.Context.ObservationRevisionFingerprint,
                    TacticalCombatRecommendationIdentity.TargetChain(
                        request,
                        resolution),
                    resolution.RuleSetFingerprint,
                    search?.SemanticFingerprint
                        ?? discovery?.SemanticFingerprint,
                    TacticalCombatRecommendationIdentity.Bounds(
                        request.SearchRequest.Bounds),
                    TacticalCombatRecommendationIdentity.Policy(
                        request.Policy),
                    plan?.SelectedLoadoutFingerprint,
                    plan?.SemanticFingerprint);
            return new TacticalCombatRecommendationResult(
                status,
                reasonIdentity,
                work.ToCounts(),
                legacy,
                comparison,
                contextRead,
                resolution,
                discovery,
                search,
                scoring,
                plan,
                identity);
        }
    }

    private static DateTimeOffset? LatestObservation(CombatSnapshot snapshot) =>
        snapshot.FieldSources
            .Where(item => item.Source
                == SnapshotDataSource.CurrentScreenObservation)
            .Select(item => (DateTimeOffset?)item.CapturedAtUtc)
            .Max();

    private static bool HasPartialEvidence(
        TacticalCombatRuleResolution resolution) =>
        resolution.Transitions.Any(item => item.Applicability is
            TacticalRuleApplicability.Incomplete or
            TacticalRuleApplicability.Conflicting)
        || resolution.Roles.Any(item => item.Applicability is
            TacticalRuleApplicability.Incomplete or
            TacticalRuleApplicability.Conflicting);

    private static bool IsSourceFailure(
        Exception exception,
        PipelineStage stage) =>
        stage == PipelineStage.Source
        && exception is CombatSnapshotTargetNotFoundException
            or IOException
            or UnauthorizedAccessException
            or InvalidDataException
            or ArgumentException;

    private static bool IsStageFailure(
        Exception exception,
        PipelineStage stage,
        PipelineStage expected) =>
        stage == expected
        && exception is ArgumentException or InvalidOperationException;

    private static string StageIdentity(PipelineStage stage) =>
        $"TACTICAL_RECOMMENDATION_{stage.ToString().ToUpperInvariant()}";

    private enum PipelineStage
    {
        Source,
        Rule,
        Context,
        Discovery,
        Search,
        Scoring,
        Planning
    }

    private sealed class WorkTracker
    {
        internal int SnapshotReads { get; set; }

        internal int LegacyRecommendationBuilds { get; set; }

        internal int ComparisonBuilds { get; set; }

        internal int RuleResolutions { get; set; }

        internal int ContextProjections { get; set; }

        internal int CandidateDiscoveries { get; set; }

        internal int Searches { get; set; }

        internal int Scores { get; set; }

        internal int PlanCompilations { get; set; }

        internal TacticalRecommendationWorkCounts ToCounts() => new(
            SnapshotReads,
            LegacyRecommendationBuilds,
            ComparisonBuilds,
            RuleResolutions,
            ContextProjections,
            CandidateDiscoveries,
            Searches,
            Scores,
            PlanCompilations);
    }
}
