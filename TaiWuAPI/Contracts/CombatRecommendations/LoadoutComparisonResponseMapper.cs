using TaiWu.Application.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.LoadoutComparisons;

namespace TaiWuAPI.Contracts.CombatRecommendations;

public static class LoadoutComparisonResponseMapper
{
    public const string ScoreScopeNotice =
        "Scores rank candidates only inside this policy; they are not win "
        + "odds.";

    public static LoadoutComparisonResponse Map(
        LoadoutComparison comparison,
        CombatLoadoutRecommendation recommendation)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        ArgumentNullException.ThrowIfNull(recommendation);

        var skills = recommendation.Snapshot.Player.LearnedSkills
            .ToDictionary(skill => skill.SkillId);
        var threats = recommendation.RecommendationThreats
            .ToDictionary(threat => threat.Code, StringComparer.Ordinal);
        return new LoadoutComparisonResponse(
            comparison.ComparisonReference.Value,
            comparison.SnapshotReference.Value,
            comparison.TargetReference.Value,
            [.. comparison.Columns.Select(column => MapColumn(
                column,
                skills,
                threats))],
            [.. comparison.BaselineProvenance.Select(value =>
                new LoadoutComparisonProvenanceResponse(
                    value.Field,
                    value.Source,
                    value.CapturedAtUtc,
                    value.EvidenceReference.Value))]);
    }

    private static LoadoutComparisonColumnResponse MapColumn(
        LoadoutComparisonColumn column,
        IReadOnlyDictionary<int, CombatSkillSnapshot> skills,
        IReadOnlyDictionary<string, TargetThreat> threats)
    {
        return new LoadoutComparisonColumnResponse(
            column.Kind,
            column.Status,
            column.Policy,
            column.Loadout is null
                ? null
                : MapLoadout(column.Loadout, skills),
            column.TacticalSummary is null
                ? null
                : MapTactical(column.TacticalSummary, threats),
            column.Diagnostic is null
                ? null
                : new LoadoutComparisonDiagnosticResponse(
                    column.Diagnostic.Code.Value,
                    column.Diagnostic.Summary,
                    [.. column.Diagnostic.EvidenceReferences.Select(
                        value => value.Value)]));
    }

    private static LoadoutComparisonLoadoutResponse MapLoadout(
        LoadoutComparisonLoadout loadout,
        IReadOnlyDictionary<int, CombatSkillSnapshot> skills)
    {
        return new LoadoutComparisonLoadoutResponse(
            [.. loadout.Categories.Select(category =>
                new LoadoutComparisonCategoryResponse(
                    category.Category,
                    MapCapacity(category.Capacity),
                    [.. category.Skills.Select(cell =>
                        MapSkill(cell, skills))]))],
            MapGenericSlots(loadout.GenericSlotAllocation));
    }

    private static LoadoutComparisonSkillResponse MapSkill(
        LoadoutComparisonSkillCell cell,
        IReadOnlyDictionary<int, CombatSkillSnapshot> skills)
    {
        if (!skills.TryGetValue(cell.Identity.SkillId, out var skill)
            || skill.Category != cell.Identity.Category)
        {
            throw new InvalidOperationException(
                $"Comparison skill {cell.Identity.SkillId} is absent from "
                + "the matching player snapshot.");
        }

        return new LoadoutComparisonSkillResponse(
            MapIdentity(cell.Identity),
            MapString(skill.DisplayName),
            MapDirection(skill.Direction),
            MapMembership(cell.Membership),
            MapInt(cell.EffectiveCost),
            [.. cell.Actions.Select(action =>
                new LoadoutComparisonSkillActionResponse(
                    action.Kind,
                    action.RequiredDirection,
                    new LoadoutComparisonReasonResponse(
                        action.Reason.Code.Value,
                        action.Reason.Summary,
                        [.. action.Reason.EvidenceReferences.Select(
                            value => value.Value)],
                        [.. action.Reason.ThreatReferences.Select(
                            value => ThreatReference(value.Value))]))) ]);
    }

    private static LoadoutComparisonCapacityResponse MapCapacity(
        LoadoutComparisonCapacitySummary capacity)
    {
        return new LoadoutComparisonCapacityResponse(
            MapInt(capacity.Used),
            MapInt(capacity.Capacity),
            MapInt(capacity.Remaining),
            MapInt(capacity.CategoryContribution),
            MapInt(capacity.GenericContribution));
    }

    private static LoadoutComparisonTacticalSummaryResponse MapTactical(
        LoadoutComparisonTacticalSummary summary,
        IReadOnlyDictionary<string, TargetThreat> threats)
    {
        return new LoadoutComparisonTacticalSummaryResponse(
            MapInt(summary.ManualActionCount),
            MapIdentity(summary.ActiveDefense),
            MapIdentity(summary.ActiveAgility),
            [.. summary.CoveredThreats.Select(value =>
                MapThreat(value, threats))],
            [.. summary.UnresolvedThreats.Select(value =>
                MapThreat(value, threats))],
            [.. summary.Conditions.Select(value => value.Value)],
            [.. summary.Caveats.Select(value => value.Value)],
            [.. summary.EvidenceReferences.Select(value => value.Value)],
            [.. summary.ScoreComponents.Select(component =>
                new LoadoutComparisonScoreResponse(
                    component.Kind,
                    component.Weight,
                    MapDecimal(component.Score),
                    component.Explanation,
                    component.EvidenceReference.Value))],
            ScoreScopeNotice);
    }

    private static LoadoutComparisonThreatResponse MapThreat(
        LoadoutComparisonReference reference,
        IReadOnlyDictionary<string, TargetThreat> threats)
    {
        threats.TryGetValue(reference.Value, out var threat);
        return new LoadoutComparisonThreatResponse(
            ThreatReference(reference.Value),
            reference.Value,
            threat?.Title);
    }

    private static LoadoutComparisonGenericSlotValueResponse MapGenericSlots(
        LoadoutComparisonValue<GenericSlotAllocation> value)
    {
        return value.IsAvailable
            ? new LoadoutComparisonGenericSlotValueResponse(
                IsAvailable: true,
                new GenericSlotPlanResponse(
                    value.Value.TotalSlots,
                    value.Value.Attack,
                    value.Value.Agility,
                    value.Value.Defense,
                    value.Value.Assistance),
                UnavailableReason: null)
            : new LoadoutComparisonGenericSlotValueResponse(
                IsAvailable: false,
                Value: null,
                value.UnavailableReason);
    }

    private static LoadoutComparisonSkillIdentityValueResponse MapIdentity(
        LoadoutComparisonValue<LoadoutComparisonSkillIdentity> value)
    {
        return value.IsAvailable
            ? new LoadoutComparisonSkillIdentityValueResponse(
                IsAvailable: true,
                MapIdentity(value.Value),
                UnavailableReason: null)
            : new LoadoutComparisonSkillIdentityValueResponse(
                IsAvailable: false,
                Value: null,
                value.UnavailableReason);
    }

    private static LoadoutComparisonSkillIdentityResponse MapIdentity(
        LoadoutComparisonSkillIdentity identity) => new(
            identity.Category,
            identity.SkillId);

    private static LoadoutComparisonIntValueResponse MapInt(
        LoadoutComparisonValue<int> value) => value.IsAvailable
        ? new LoadoutComparisonIntValueResponse(
            IsAvailable: true,
            value.Value,
            UnavailableReason: null)
        : new LoadoutComparisonIntValueResponse(
            IsAvailable: false,
            Value: null,
            value.UnavailableReason);

    private static LoadoutComparisonDecimalValueResponse MapDecimal(
        LoadoutComparisonValue<decimal> value) => value.IsAvailable
        ? new LoadoutComparisonDecimalValueResponse(
            IsAvailable: true,
            value.Value,
            UnavailableReason: null)
        : new LoadoutComparisonDecimalValueResponse(
            IsAvailable: false,
            Value: null,
            value.UnavailableReason);

    private static LoadoutComparisonStringValueResponse MapString(
        SnapshotValue<string> value) => value.IsAvailable
        ? new LoadoutComparisonStringValueResponse(
            IsAvailable: true,
            value.Value,
            UnavailableReason: null)
        : new LoadoutComparisonStringValueResponse(
            IsAvailable: false,
            Value: null,
            value.UnavailableReason);

    private static LoadoutComparisonPracticeDirectionValueResponse
        MapDirection(SnapshotValue<PracticeDirection> value) =>
        value.IsAvailable
            ? new LoadoutComparisonPracticeDirectionValueResponse(
                IsAvailable: true,
                value.Value,
                UnavailableReason: null)
            : new LoadoutComparisonPracticeDirectionValueResponse(
                IsAvailable: false,
                Value: null,
                value.UnavailableReason);

    private static LoadoutComparisonMembershipValueResponse MapMembership(
        LoadoutComparisonValue<LoadoutComparisonMembership> value) =>
        value.IsAvailable
            ? new LoadoutComparisonMembershipValueResponse(
                IsAvailable: true,
                value.Value,
                UnavailableReason: null)
            : new LoadoutComparisonMembershipValueResponse(
                IsAvailable: false,
                Value: null,
                value.UnavailableReason);

    private static string ThreatReference(string code) => $"threat:{code}";
}
