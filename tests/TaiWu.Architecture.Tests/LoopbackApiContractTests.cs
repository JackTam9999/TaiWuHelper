using TaiWu.Application.Localization;
using TaiWu.Domain.CombatRecommendations;
using TaiWuAPI.Contracts.CompanionCandidates;
using TaiWuAPI.Contracts.VillageWorkforce;
using Xunit;

namespace TaiWu.Architecture.Tests;

public sealed class LoopbackApiContractTests
{
    [Fact]
    public void Village_workforce_contracts_are_api_owned_at_every_nested_level()
    {
        var contractAssembly = typeof(VillageWorkforceResultResponse).Assembly;
        var actual = contractAssembly.GetExportedTypes()
            .Where(type => type.Namespace?.Equals(
                "TaiWuAPI.Contracts.VillageWorkforce",
                StringComparison.Ordinal) == true)
            .SelectMany(type => type.GetProperties())
            .SelectMany(property => FlattenType(property.PropertyType))
            .Where(type => type.Assembly != contractAssembly
                && type.Assembly != typeof(string).Assembly)
            .Select(type => type.FullName!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(actual);
    }

    [Fact]
    public void Cross_layer_contract_types_are_explicitly_inventoried()
    {
        var contractAssembly = typeof(CompanionFinderResponse).Assembly;
        var layerAssemblies = new[]
        {
            typeof(TaiwuLanguage).Assembly,
            typeof(RecommendationPolicy).Assembly
        };
        var actual = contractAssembly.GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith(
                "TaiWuAPI.Contracts",
                StringComparison.Ordinal) == true)
            .SelectMany(type => type.GetProperties())
            .SelectMany(property => FlattenType(property.PropertyType))
            .Where(type => layerAssemblies.Contains(type.Assembly))
            .Select(type => type.FullName!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedCrossLayerContractTypes, actual);
        Assert.All(actual, typeName => Assert.True(
            typeName.StartsWith("TaiWu.Application.", StringComparison.Ordinal)
            || typeName.StartsWith("TaiWu.Domain.", StringComparison.Ordinal)));
    }

    private static IEnumerable<Type> FlattenType(Type type)
    {
        yield return type;

        if (type.HasElementType && type.GetElementType() is { } elementType)
        {
            foreach (var nestedType in FlattenType(elementType))
            {
                yield return nestedType;
            }
        }

        foreach (var genericArgument in type.GetGenericArguments())
        {
            foreach (var nestedType in FlattenType(genericArgument))
            {
                yield return nestedType;
            }
        }
    }

    private static readonly string[] ExpectedCrossLayerContractTypes =
    [
        "TaiWu.Application.CombatRecommendations.TargetPlaybookCounterAvailabilityState",
        "TaiWu.Application.CombatRecommendations.TargetRecommendationChangeCause",
        "TaiWu.Application.CombatRecommendations.TargetRecommendationImpactKind",
        "TaiWu.Application.CombatRecommendations.TargetThreatImpactKind",
        "TaiWu.Application.CombatSkills.CatalogueRecoveryStatus",
        "TaiWu.Application.CombatSkills.CharacterProgressReadStatus",
        "TaiWu.Application.CombatSkills.ClearCharacterCombatSkillProgressCacheStatus",
        "TaiWu.Application.CombatSkills.CombatSkillCatalogueStatus",
        "TaiWu.Application.CombatSkills.CombatSkillQueryIssue",
        "TaiWu.Application.CombatSkills.EnsureCombatSkillCatalogueStatus",
        "TaiWu.Application.CombatSkills.TargetSkillMatchKind",
        "TaiWu.Application.CombatSkills.TargetSkillSnapshotPresence",
        "TaiWu.Application.CompanionCandidates.CompanionCandidateEnrichmentState",
        "TaiWu.Application.CompanionCandidates.CompanionCandidateEnrichmentStatus",
        "TaiWu.Application.CompanionCandidates.CompanionCandidateSnapshotReadStatus",
        "TaiWu.Application.CompanionCandidates.CompanionDetailedProgressState",
        "TaiWu.Application.CompanionCandidates.CompanionFinderStatus",
        "TaiWu.Application.CompanionCandidates.CompanionMembershipEvidenceState",
        "TaiWu.Application.CompanionCandidates.CompanionSkillDefinitionState",
        "TaiWu.Application.Localization.TaiwuLanguage",
        "TaiWu.Application.TacticalCombat.TacticalCombatRecommendationStatus",
        "TaiWu.Application.Targets.TargetLookupKind",
        "TaiWu.Application.Targets.TargetLookupStatus",
        "TaiWu.Domain.CombatCounters.CombatCounterAccessIssueCode",
        "TaiWu.Domain.CombatCounters.CombatCounterActivationTiming",
        "TaiWu.Domain.CombatCounters.CombatCounterStrength",
        "TaiWu.Domain.CombatEffects.CombatEffectMechanic",
        "TaiWu.Domain.CombatRecommendations.BattlePlanInstructionKind",
        "TaiWu.Domain.CombatRecommendations.CombatLoadoutGenerationDiagnosticCode",
        "TaiWu.Domain.CombatRecommendations.ManualLoadoutChangeKind",
        "TaiWu.Domain.CombatRecommendations.RecommendationCaveatKind",
        "TaiWu.Domain.CombatRecommendations.RecommendationPolicy",
        "TaiWu.Domain.CombatRecommendations.RecommendationScoreComponentKind",
        "TaiWu.Domain.CombatSkills.CatalogueFieldStatus",
        "TaiWu.Domain.CombatSkills.CatalogueLanguage",
        "TaiWu.Domain.CombatSkills.CatalogueSourceKind",
        "TaiWu.Domain.CombatSkills.CombatSkillPowerContext",
        "TaiWu.Domain.CombatSkills.CombatSkillStudyDetailGroup",
        "TaiWu.Domain.CombatSkills.RawCombatSkillDescriptionKind",
        "TaiWu.Domain.CombatSkills.SkillProgressFieldStatus",
        "TaiWu.Domain.CombatSkills.SkillProgressSourceKind",
        "TaiWu.Domain.CombatSnapshots.CombatRequirementCriticality",
        "TaiWu.Domain.CombatSnapshots.CombatResourceKind",
        "TaiWu.Domain.CombatSnapshots.CombatSkillElement",
        "TaiWu.Domain.CombatSnapshots.LegendaryBookAssignmentOrigin",
        "TaiWu.Domain.CombatSnapshots.PracticeDirection",
        "TaiWu.Domain.CombatSnapshots.SkillActivationState",
        "TaiWu.Domain.CombatSnapshots.SkillCategory",
        "TaiWu.Domain.CombatSnapshots.SnapshotDataSource",
        "TaiWu.Domain.CombatSnapshots.SnapshotEvidenceStatus",
        "TaiWu.Domain.CombatSnapshots.TargetLoadoutCoverageKind",
        "TaiWu.Domain.CombatSnapshots.TargetLoadoutMergeStatus",
        "TaiWu.Domain.CombatSnapshots.TargetObservationContext",
        "TaiWu.Domain.CombatThreats.TargetThreatActivationTiming",
        "TaiWu.Domain.CombatThreats.TargetThreatSeverity",
        "TaiWu.Domain.CombatThreats.TargetThreatSourceKind",
        "TaiWu.Domain.CompanionCandidates.CandidateConflictDecisionKind",
        "TaiWu.Domain.CompanionCandidates.CandidateDisciplineDomain",
        "TaiWu.Domain.CompanionCandidates.CandidateEvidenceSourceKind",
        "TaiWu.Domain.CompanionCandidates.CandidateFactValueKind",
        "TaiWu.Domain.CompanionCandidates.CandidateMainAttribute",
        "TaiWu.Domain.CompanionCandidates.CandidateProfileField",
        "TaiWu.Domain.CompanionCandidates.CompanionCapabilityCategory",
        "TaiWu.Domain.CompanionCandidates.CompanionCapabilitySummaryFormula",
        "TaiWu.Domain.CompanionCandidates.CompanionCapabilitySummaryState",
        "TaiWu.Domain.CompanionRoles.CompanionRoleCandidateRankingState",
        "TaiWu.Domain.CompanionRoles.CompanionRoleComparisonEvidenceState",
        "TaiWu.Domain.CompanionRoles.CompanionRoleComparisonOutcome",
        "TaiWu.Domain.CompanionRoles.CompanionRoleEvaluationState",
        "TaiWu.Domain.CompanionRoles.CompanionRoleExplanationKind",
        "TaiWu.Domain.CompanionRoles.CompanionRoleGateOutcome",
        "TaiWu.Domain.CompanionRoles.CompanionRoleNormalizationKind",
        "TaiWu.Domain.CompanionRoles.CompanionRoleRequirementKind",
        "TaiWu.Domain.CompanionRoles.CompanionRoleScoreDirection",
        "TaiWu.Domain.CompanionRoles.CompanionRoleShortlistFilter",
        "TaiWu.Domain.LoadoutComparisons.LoadoutComparisonBaselineField",
        "TaiWu.Domain.LoadoutComparisons.LoadoutComparisonColumnKind",
        "TaiWu.Domain.LoadoutComparisons.LoadoutComparisonColumnStatus",
        "TaiWu.Domain.LoadoutComparisons.LoadoutComparisonMembership",
        "TaiWu.Domain.LoadoutComparisons.LoadoutComparisonSkillActionKind",
        "TaiWu.Domain.TacticalCombat.TacticalBranchOutcome",
        "TaiWu.Domain.TacticalCombat.TacticalCandidateAdmissionState",
        "TaiWu.Domain.TacticalCombat.TacticalCandidateDecision",
        "TaiWu.Domain.TacticalCombat.TacticalCandidateGateKind",
        "TaiWu.Domain.TacticalCombat.TacticalCandidateGateState",
        "TaiWu.Domain.TacticalCombat.TacticalCandidateSupportState",
        "TaiWu.Domain.TacticalCombat.TacticalContextAvailability",
        "TaiWu.Domain.TacticalCombat.TacticalContextFactState",
        "TaiWu.Domain.TacticalCombat.TacticalContextOrigin",
        "TaiWu.Domain.TacticalCombat.TacticalEvidenceSourceKind",
        "TaiWu.Domain.TacticalCombat.TacticalEvidenceState",
        "TaiWu.Domain.TacticalCombat.TacticalFactValueKind",
        "TaiWu.Domain.TacticalCombat.TacticalFinishDisposition",
        "TaiWu.Domain.TacticalCombat.TacticalPlanStage",
        "TaiWu.Domain.TacticalCombat.TacticalPlanStageState",
        "TaiWu.Domain.TacticalCombat.TacticalPreparationCheckKind",
        "TaiWu.Domain.TacticalCombat.TacticalPruningRuleKind",
        "TaiWu.Domain.TacticalCombat.TacticalRequirementOperator",
        "TaiWu.Domain.TacticalCombat.TacticalRequirementOutcome",
        "TaiWu.Domain.TacticalCombat.TacticalResolvedRuleKind",
        "TaiWu.Domain.TacticalCombat.TacticalRuleApplicability",
        "TaiWu.Domain.TacticalCombat.TacticalRuleEvidenceDisposition",
        "TaiWu.Domain.TacticalCombat.TacticalRuleEvidenceScope",
        "TaiWu.Domain.TacticalCombat.TacticalRulePurpose",
        "TaiWu.Domain.TacticalCombat.TacticalRuleSetResolutionStatus",
        "TaiWu.Domain.TacticalCombat.TacticalScoreComponentKind",
        "TaiWu.Domain.TacticalCombat.TacticalScoreComponentState",
        "TaiWu.Domain.TacticalCombat.TacticalScoreInputKind",
        "TaiWu.Domain.TacticalCombat.TacticalSearchTerminator",
        "TaiWu.Domain.TacticalCombat.TacticalStepBranchKind",
        "TaiWu.Domain.TacticalCombat.TacticalTransitionTiming",
        "TaiWu.Domain.TargetArchetypes.TargetArchetypeMatchState",
        "TaiWu.Domain.TargetPlaybookComposition.TargetPlaybookAdjustmentAction",
        "TaiWu.Domain.TargetPlaybookComposition.TargetPlaybookAdjustmentEvidenceKind",
        "TaiWu.Domain.TargetPlaybookComposition.TargetPlaybookAdjustmentEvidenceState",
        "TaiWu.Domain.TargetPlaybookComposition.TargetPlaybookCompositionConflictKind",
        "TaiWu.Domain.TargetPlaybookComposition.TargetPlaybookResponseReferenceKind",
        "TaiWu.Domain.TargetPlaybooks.TargetCounterPlaybookGapKind",
        "TaiWu.Domain.TargetPlaybooks.TargetCounterPlaybookResolutionStatus",
        "TaiWu.Domain.TargetPlaybooks.TargetResponsePriority",
        "TaiWu.Domain.TargetProfiles.TargetProfileDiagnosticSeverity",
        "TaiWu.Domain.TargetProfiles.TargetProfileDimension",
        "TaiWu.Domain.TargetProfiles.TargetProfileEvidenceSourceKind",
        "TaiWu.Domain.TargetProfiles.TargetProfileEvidenceState",
        "TaiWu.Domain.TargetProfiles.TargetProfileFacetValueKind"
    ];
}
