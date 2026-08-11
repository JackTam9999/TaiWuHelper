using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybookComposition;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;
using TaiWuAPI.Localization;

namespace TaiWuAPI.Contracts.CombatRecommendations;

public static class TargetStrategyResponseMapper
{
    public static TargetStrategyResponse Map(
        TargetPlaybookPersonalization value,
        PlayerCombatSnapshot player,
        TaiwuLanguage language)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(player);
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(nameof(language));
        }

        return new TargetStrategyResponse(
            MapProfile(value.Analysis.Profile),
            [.. value.Analysis.ArchetypeMatches.Matches.Select(match =>
                MapMatch(match, language))],
            MapComposition(value, player, language),
            MapAdjustments(value.Adjustments, language),
            [.. value.Counters.Select(counter =>
                MapAvailability(counter, language))]);
    }

    private static TargetCombatProfileResponse MapProfile(
        TargetCombatProfile profile) => new(
        profile.TargetCharacterId,
        profile.RuleVersion.Value,
        profile.Fingerprint,
        [.. profile.Facets.Select(MapFacet)],
        [.. profile.Diagnostics.Select(diagnostic =>
            new TargetProfileDiagnosticResponse(
                diagnostic.Code,
                diagnostic.Severity,
                diagnostic.Facet is null
                    ? null
                    : MapFacetReference(diagnostic.Facet),
                diagnostic.EvidenceReferences))]);

    private static TargetProfileFacetResponse MapFacet(
        TargetProfileFacet facet) => new(
        facet.Identity.Dimension,
        facet.Identity.Code,
        facet.State,
        facet.Value is null ? null : MapValue(facet.Value),
        [.. facet.Evidence.Select(MapEvidence)],
        [.. facet.ConflictCandidates.Select(candidate =>
            new TargetProfileConflictCandidateResponse(
                MapValue(candidate.Value),
                [.. candidate.Evidence.Select(MapEvidence)]))],
        facet.UnavailableReason is null
            ? null
            : new TargetProfileUnavailableReasonResponse(
                facet.UnavailableReason.Code,
                facet.UnavailableReason.Detail));

    private static TargetProfileFacetValueResponse MapValue(
        TargetProfileFacetValue value) => new(
        value.Dimension,
        value.Code,
        value.Kind,
        [.. value.Measurements.Select(measurement =>
            new TargetProfileMeasurementResponse(
                measurement.Code,
                measurement.Value,
                measurement.UnitCode))]);

    private static TargetProfileEvidenceResponse MapEvidence(
        TargetProfileEvidence evidence) => new(
        evidence.Reference,
        evidence.SourceKind,
        evidence.SourceIdentity,
        evidence.SourceVersion.Value);

    private static TargetArchetypeMatchResponse MapMatch(
        TargetArchetypeMatch match,
        TaiwuLanguage language) => new(
        match.Definition.Identity.Code,
        match.Definition.Identity.Version.Value,
        TargetStrategyUiText.Archetype(
            language,
            match.Definition.Identity.Code),
        match.Definition.ApplicableProfileRuleVersion.Value,
        match.State,
        [.. match.SupportingFacets.Select(MapFacetReference)],
        [.. match.MissingFacets.Select(MapFacetReference)],
        [.. match.ExcludingFacets.Select(MapFacetReference)],
        [.. match.ConflictingFacets.Select(MapFacetReference)],
        [.. match.Diagnostics.Select(diagnostic =>
            new TargetArchetypeMatchDiagnosticResponse(
                diagnostic.Code,
                diagnostic.PredicateCode,
                diagnostic.Facet is null
                    ? null
                    : MapFacetReference(diagnostic.Facet)))],
        match.Definition.EvidenceReferences);

    private static TargetPlaybookCompositionResponse MapComposition(
        TargetPlaybookPersonalization value,
        PlayerCombatSnapshot player,
        TaiwuLanguage language)
    {
        var eligibleGoalCodes = value.EligibleGoals
            .Select(goal => goal.Code)
            .ToHashSet(StringComparer.Ordinal);
        return new TargetPlaybookCompositionResponse(
            value.Composition.ProfileFingerprint,
            [.. value.Composition.SourcePlaybooks.Select(playbook =>
                new TargetPlaybookIdentityResponse(
                    playbook.Identity.Archetype.Code,
                    playbook.Identity.Archetype.Version.Value,
                    playbook.Identity.Version.Value,
                    playbook.EvidenceReferences))],
            [.. value.Composition.Goals.Select(goal => new
                TargetResponseGoalResponse(
                    goal.Code,
                    TargetStrategyUiText.Goal(language, goal.Code),
                    goal.Sequence,
                    goal.Priority,
                    goal.ResponseTiming,
                    eligibleGoalCodes.Contains(goal.Code),
                    goal.SourcePlaybookKeys,
                    [.. goal.ProfileFacets.Select(MapFacetReference)],
                    [.. goal.Threats.Select(threat =>
                        ThreatReference(threat.Code))],
                    [.. goal.Options.Select(option =>
                        MapOption(option, goal, player))],
                    goal.ConflictGroups,
                    goal.EvidenceReferences,
                    [.. goal.KnownGaps.Select(gap =>
                        MapGap(gap, language))]))],
            [.. value.Composition.Conflicts.Select(conflict =>
                new TargetPlaybookConflictResponse(
                    conflict.Kind,
                    conflict.ConflictGroup,
                    conflict.GoalCodes,
                    conflict.OptionCodes))],
            [.. value.Gaps.Select(gap => MapGap(gap, language))],
            [.. value.Composition.Diagnostics.Select(diagnostic =>
                new TargetPlaybookCompositionDiagnosticResponse(
                    diagnostic.Code,
                    diagnostic.Archetype.Code,
                    diagnostic.Archetype.Version.Value,
                    diagnostic.MatchState,
                    diagnostic.ResolutionStatus))]);
    }

    private static TargetCounterOptionResponse MapOption(
        ComposedTargetCounterOption option,
        ComposedTargetResponseGoal goal,
        PlayerCombatSnapshot player)
    {
        var skill = player.LearnedSkills.SingleOrDefault(value =>
            value.SkillId == option.Effect.SkillId);
        return new TargetCounterOptionResponse(
            option.StableKey,
            option.Effect.SkillId,
            skill?.DisplayName.IsAvailable == true
                ? skill.DisplayName.Value
                : null,
            option.CounterRule.RequiredDirection,
            option.Effect.RawEffectId,
            option.Strength,
            option.ActivationTiming,
            [.. option.ApplicableThreatCodes([goal])
                .Select(ThreatReference)],
            [.. option.Requirements.Select(MapRequirement)],
            option.SourcePlaybookKeys,
            option.SourceGoalCodes,
            option.ConflictGroups);
    }

    private static TargetCombatRequirementResponse MapRequirement(
        CombatRequirement value) => value switch
        {
            WeaponRequirement requirement => new(
                TargetCombatRequirementKind.Weapon,
                requirement.Criticality,
                requirement.EvidenceReference,
                WeaponTypeId: requirement.WeaponTypeId),
            TrickRequirement requirement => new(
                TargetCombatRequirementKind.Trick,
                requirement.Criticality,
                requirement.EvidenceReference,
                TrickTypeId: requirement.TrickTypeId,
                MinimumCount: requirement.MinimumCount),
            RangeRequirement requirement => new(
                TargetCombatRequirementKind.Range,
                requirement.Criticality,
                requirement.EvidenceReference,
                MinimumRangeInclusive: requirement.MinimumInclusive,
                MaximumRangeInclusive: requirement.MaximumInclusive),
            ResourceRequirement requirement => new(
                TargetCombatRequirementKind.Resource,
                requirement.Criticality,
                requirement.EvidenceReference,
                Resource: requirement.Resource,
                MinimumAmount: requirement.MinimumAmount),
            WeaponUnlockRequirement requirement => new(
                TargetCombatRequirementKind.WeaponUnlock,
                requirement.Criticality,
                requirement.EvidenceReference,
                WeaponTypeId: requirement.WeaponTypeId),
            SkillActivationRequirement requirement => new(
                TargetCombatRequirementKind.SkillActivation,
                requirement.Criticality,
                requirement.EvidenceReference,
                SkillId: requirement.SkillId,
                RequiredSkillState: requirement.RequiredState),
            _ => throw new ArgumentOutOfRangeException(
                nameof(value),
                value.GetType().Name,
                "Unknown combat requirement type.")
        };

    private static TargetPlaybookGapResponse MapGap(
        TargetCounterPlaybookGap gap,
        TaiwuLanguage language) => new(
        gap.Code,
        gap.Kind,
        TargetStrategyUiText.Gap(language, gap.LocalizedMessageKey),
        gap.RelatedCounterCode,
        gap.EvidenceReferences);

    private static TargetPlaybookAdjustmentSetResponse MapAdjustments(
        TargetPlaybookAdjustmentSet value,
        TaiwuLanguage language) => new(
        value.ProfileFingerprint,
        [.. value.Adjustments.Select(adjustment =>
            new TargetPlaybookAdjustmentResponse(
                adjustment.RuleCode,
                adjustment.Action,
                MapReference(adjustment.OriginalResponse),
                MapReference(adjustment.ResultResponse),
                adjustment.ReasonCode,
                TargetStrategyUiText.AdjustmentReason(
                    language,
                    adjustment.ReasonCode),
                [.. adjustment.Evidence.Select(evidence =>
                    new TargetPlaybookAdjustmentEvidenceResponse(
                        evidence.Kind,
                        evidence.State,
                        evidence.Identity,
                        evidence.EvidenceReferences))]))],
        [.. value.Diagnostics.Select(diagnostic =>
            new TargetPlaybookAdjustmentDiagnosticResponse(
                diagnostic.Code,
                diagnostic.RuleCode,
                diagnostic.EvidenceIdentities))]);

    private static TargetPlaybookResponseReferenceResponse? MapReference(
        TargetPlaybookResponseReference? value) => value is null
            ? null
            : new TargetPlaybookResponseReferenceResponse(
                value.Kind,
                value.StableCode);

    private static TargetCounterAvailabilityResponse MapAvailability(
        TargetPlaybookCounterAvailability value,
        TaiwuLanguage language) => new(
        value.Option.StableKey,
        value.State,
        [.. value.Access.Issues.Select(issue =>
            new TargetCounterAccessIssueResponse(
                issue.Code,
                Localize(language, issue.Reason)))],
        [.. value.Diagnostics.Select(diagnostic =>
            new TargetCounterGenerationDiagnosticResponse(
                diagnostic.Code,
                diagnostic.Occurrences,
                Localize(language, diagnostic.Reason)))],
        value.Gap is null ? null : MapGap(value.Gap, language));

    private static TargetProfileFacetReferenceResponse MapFacetReference(
        TargetProfileFacetIdentity facet) => new(
        facet.Dimension,
        facet.Code);

    private static string Localize(
        TaiwuLanguage language,
        string value) => language == TaiwuLanguage.Chinese
        ? DynamicUiText.Get(value)
        : value;

    private static string ThreatReference(string code) => $"threat:{code}";
}
