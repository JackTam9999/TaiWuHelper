using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using TaiWuAPI.Contracts.CombatRecommendations;

namespace TaiWuAPI.Presentation;

public static class TacticalCombatViewModelMapper
{
    public static TacticalCombatViewModel Map(
        TacticalCombatRecommendationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var legacy = result.LegacyRecommendation
            ?? throw new ArgumentException(
                "A tactical presentation requires its coherent legacy recommendation.",
                nameof(result));
        var skillNames = legacy.Snapshot.Player.LearnedSkills.ToDictionary(
            item => item.SkillId,
            item => item.DisplayName.IsAvailable
                ? item.DisplayName.Value
                : "Unnamed skill");
        var targetName = legacy.Snapshot.Target.DisplayName.IsAvailable
            ? legacy.Snapshot.Target.DisplayName.Value
            : "Selected target";
        return Map(
            TacticalCombatResponseMapper.Map(result),
            targetName,
            legacy.RequestedPolicy,
            skillNames);
    }

    internal static TacticalCombatViewModel Map(
        TacticalCombatResponse response,
        string targetName,
        RecommendationPolicy policy,
        IReadOnlyDictionary<int, string> skillNames)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        ArgumentNullException.ThrowIfNull(skillNames);

        var transitionPurposes = response.TargetChain?.Transitions
            .ToDictionary(item => item.Identity, item => item.Purpose,
                StringComparer.Ordinal)
            ?? new Dictionary<string, TacticalRulePurpose>(
                StringComparer.Ordinal);
        var stages = response.Plan?.Stages
            .Select(stage => MapStage(
                stage,
                transitionPurposes,
                skillNames))
            .ToArray() ?? [];
        var selectedScore = response.Scoring?.RankedCandidates
            .FirstOrDefault(item => string.Equals(
                item.CandidateIdentity,
                response.SelectedLoadout?.CandidateIdentity,
                StringComparison.Ordinal))
            ?? response.Scoring?.RankedCandidates.FirstOrDefault();
        var evidence = CollectEvidence(response)
            .DistinctBy(item => (
                item.Source,
                item.GameDataVersion,
                item.RuleVersion,
                item.ScopeIdentity))
            .Select(MapEvidence)
            .ToArray();

        return new TacticalCombatViewModel(
            response.Status,
            targetName.Trim(),
            response.Scoring?.Policy ?? policy,
            response.Snapshot?.CapturedAtUtc,
            response.Snapshot?.LatestObservationAtUtc,
            response.Snapshot?.GameDataVersion,
            response.Plan?.FinishDisposition,
            stages,
            MapGaps(response),
            response.Search is null ? null : MapSearch(response.Search),
            selectedScore?.Components.Select(MapScore).ToArray() ?? [],
            MapCandidates(response, skillNames),
            evidence,
            response.Identity?.SemanticFingerprint ?? "—");
    }

    private static TacticalStageViewModel MapStage(
        TacticalPlanStageResponse stage,
        IReadOnlyDictionary<string, TacticalRulePurpose> transitionPurposes,
        IReadOnlyDictionary<int, string> skillNames) => new(
            stage.Stage,
            stage.State,
            StageLimitation(stage),
            stage.Steps.Select(step => MapStep(
                stage.Stage,
                step,
                transitionPurposes,
                skillNames)).ToArray());

    private static TacticalStepViewModel MapStep(
        TacticalPlanStage stage,
        TacticalPlanStepResponse step,
        IReadOnlyDictionary<string, TacticalRulePurpose> transitionPurposes,
        IReadOnlyDictionary<int, string> skillNames)
    {
        var state = ConditionState(step);
        var purpose = step.Transitions
            .Select(identity => transitionPurposes.TryGetValue(
                identity,
                out var value)
                    ? value
                    : (TacticalRulePurpose?)null)
            .FirstOrDefault(value => value.HasValue);
        return new TacticalStepViewModel(
            step.Order,
            step.BranchKind,
            state,
            ConditionText(stage, state),
            ActionText(stage, step, skillNames),
            PurposeText(purpose),
            StepLimitation(state),
            step.Requirements.Select(MapRequirement).ToArray(),
            step.Evidence
                .DistinctBy(item => (
                    item.Source,
                    item.GameDataVersion,
                    item.RuleVersion,
                    item.ScopeIdentity))
                .Select(MapEvidence)
                .ToArray());
    }

    private static TacticalConditionPresentationState ConditionState(
        TacticalPlanStepResponse step)
    {
        var outcomes = step.Requirements.Select(item => item.Outcome).ToArray();
        if (outcomes.Contains(TacticalRequirementOutcome.Conflicting))
        {
            return TacticalConditionPresentationState.Conflicting;
        }

        if (outcomes.Contains(TacticalRequirementOutcome.Unsupported))
        {
            return TacticalConditionPresentationState.Unsupported;
        }

        if (outcomes.Contains(TacticalRequirementOutcome.Unsatisfied))
        {
            return TacticalConditionPresentationState.Unsatisfied;
        }

        if (outcomes.Contains(TacticalRequirementOutcome.Unknown))
        {
            return TacticalConditionPresentationState.NeedsConfirmation;
        }

        return step.BranchKind == TacticalStepBranchKind.Fallback
            ? TacticalConditionPresentationState.Fallback
            : TacticalConditionPresentationState.Confirmed;
    }

    private static BilingualText ConditionText(
        TacticalPlanStage stage,
        TacticalConditionPresentationState state)
    {
        if (state == TacticalConditionPresentationState.Conflicting)
        {
            return new(
                "Confirm the conflicting target or player state before acting.",
                "操作前，請先確認互相衝突的目標或玩家狀態。");
        }

        if (state is TacticalConditionPresentationState.NeedsConfirmation
            or TacticalConditionPresentationState.Unresolved)
        {
            return new(
                "Confirm the listed target or player state before acting.",
                "操作前，請先確認列出的目標或玩家狀態。");
        }

        return stage switch
        {
            TacticalPlanStage.Preparation => new(
                "Before combat, confirm this preparation requirement.",
                "戰鬥前，請確認此準備需求。"),
            TacticalPlanStage.Opening => new(
                "At combat opening, use this only when its requirements are present.",
                "戰鬥開場時，只有在需求成立時才使用。"),
            TacticalPlanStage.TargetStateResponse => new(
                "When the listed target state is observed, use the manual response.",
                "觀察到列出的目標狀態時，請手動應對。"),
            TacticalPlanStage.Recovery => new(
                "After the verified execution cost, confirm the recovery condition.",
                "出現已驗證的執行代價後，請確認恢復條件。"),
            TacticalPlanStage.Finish => new(
                "Only when the supported finish window is confirmed.",
                "只有在確認受支援的收尾時機時才操作。"),
            TacticalPlanStage.Fallback => new(
                "If the primary condition fails or remains unresolved.",
                "主要條件不成立或仍未解決時。"),
            _ => new("Confirm the typed condition.", "請確認此條件。")
        };
    }

    private static BilingualText ActionText(
        TacticalPlanStage stage,
        TacticalPlanStepResponse step,
        IReadOnlyDictionary<int, string> skillNames)
    {
        var name = step.SkillId.HasValue && skillNames.TryGetValue(
            step.SkillId.Value,
            out var value)
                ? value
                : null;
        if (name is not null)
        {
            return stage == TacticalPlanStage.Preparation
                ? new(
                    $"Prepare {name} manually as specified by the selected loadout.",
                    $"依所選運功方案手動準備{name}。")
                : new(
                    $"Use {name} manually when the listed condition is present.",
                    $"列出的條件成立時，請手動使用{name}。");
        }

        return new(
            "Carry out the typed manual action only when the listed condition is present.",
            "只有在列出的條件成立時，才執行此手動操作。");
    }

    private static BilingualText PurposeText(TacticalRulePurpose? purpose) =>
        purpose switch
        {
            TacticalRulePurpose.DirectMagicMindPressure => new(
                "Recognize the verified direct-practice mind-pressure transition.",
                "辨識已驗證的正練失神壓力轉換。"),
            TacticalRulePurpose.DistractionMarkAccumulation => new(
                "Address verified distraction-mark accumulation.",
                "應對已驗證的失神標記累積。"),
            TacticalRulePurpose.MindResonanceCountdown => new(
                "Address the verified mind-resonance countdown.",
                "應對已驗證的心神共鳴倒數。"),
            TacticalRulePurpose.MindResonanceCascade => new(
                "Address the verified mind-resonance cascade.",
                "應對已驗證的心神共鳴連鎖。"),
            TacticalRulePurpose.DefeatMarkReset => new(
                "Account for the verified defeat-mark reset.",
                "納入已驗證的戰敗標記清除。"),
            TacticalRulePurpose.CastSuppression => new(
                "Suppress the verified direct-practice cast window.",
                "壓制已驗證的正練施展時機。"),
            TacticalRulePurpose.DirectPracticeSelfLock => new(
                "Account for the verified direct-practice self-lock.",
                "納入已驗證的正練自我封鎖。"),
            TacticalRulePurpose.DirectPracticeLockRecovery => new(
                "Recover from the verified direct-practice lock.",
                "從已驗證的正練封鎖中恢復。"),
            TacticalRulePurpose.MarkDurationReduction => new(
                "Reduce the verified mark duration.",
                "縮短已驗證的標記持續時間。"),
            TacticalRulePurpose.ResonanceDurationReduction => new(
                "Reduce the verified resonance duration.",
                "縮短已驗證的共鳴持續時間。"),
            TacticalRulePurpose.HindranceMarkRemoval => new(
                "Remove hindrance marks under the verified condition.",
                "在已驗證條件下移除妨害標記。"),
            TacticalRulePurpose.EnemyAttackPowerReduction => new(
                "Reduce enemy attack power under the verified condition.",
                "在已驗證條件下降低敵方摧破威力。"),
            TacticalRulePurpose.ResetResourcePressure => new(
                "Apply the verified reset-resource pressure route.",
                "使用已驗證的清除資源壓力路徑。"),
            TacticalRulePurpose.ConditionalMarkTransfer => new(
                "Use the verified conditional mark-transfer route.",
                "使用已驗證的條件式標記轉移路徑。"),
            TacticalRulePurpose.DamageChannelChoice => new(
                "Use the evidence-backed attack channel.",
                "使用有證據支持的攻擊管道。"),
            TacticalRulePurpose.FinishWindowSupport => new(
                "Use the separately verified finish window.",
                "使用另行驗證的收尾時機。"),
            _ => new(
                "Obtain the verified purpose represented by this plan step.",
                "取得此計畫步驟所代表的已驗證用途。")
        };

    private static BilingualText StageLimitation(
        TacticalPlanStageResponse stage) => stage.State switch
        {
            TacticalPlanStageState.Unsupported when
                stage.Stage == TacticalPlanStage.Finish => new(
                    "Finish evidence unavailable; no finish action is inferred.",
                    "缺少收尾證據；不推測任何收尾操作。"),
            TacticalPlanStageState.Unsupported => new(
                "This stage is unsupported by the current verified evidence.",
                "目前已驗證證據不支援此階段。"),
            TacticalPlanStageState.Omitted => new(
                "No separate action is required for this stage.",
                "此階段不需要獨立操作。"),
            _ => new(
                "Follow only the listed conditions and manual actions.",
                "只依照列出的條件及手動操作執行。")
        };

    private static BilingualText StepLimitation(
        TacticalConditionPresentationState state) => state switch
        {
            TacticalConditionPresentationState.Confirmed => new(
                "This purpose is limited to the listed verified condition.",
                "此用途只限列出的已驗證條件。"),
            TacticalConditionPresentationState.Conflicting => new(
                "Conflicting evidence prevents treating this condition as satisfied.",
                "證據互相衝突，因此不能視為已符合條件。"),
            TacticalConditionPresentationState.Unsupported => new(
                "The current evidence does not support this condition.",
                "目前證據不支援此條件。"),
            TacticalConditionPresentationState.Unsatisfied => new(
                "The required condition is not satisfied.",
                "所需條件尚未符合。"),
            TacticalConditionPresentationState.Fallback => new(
                "Use only as the separately supported fallback branch.",
                "只能作為另行支援的後備分支使用。"),
            _ => new(
                "Confirm this condition manually; no default value is assumed.",
                "請手動確認此條件；系統不會假定預設值。")
        };

    private static TacticalRequirementViewModel MapRequirement(
        TacticalRequirementEvaluationResponse requirement) => new(
            requirement.Outcome,
            requirement.Outcome switch
            {
                TacticalRequirementOutcome.Satisfied => new(
                    "The typed prerequisite is satisfied for this result.",
                    "此結果已符合該項型別化前置需求。"),
                TacticalRequirementOutcome.Unsatisfied => new(
                    "The typed prerequisite is not satisfied.",
                    "尚未符合該項型別化前置需求。"),
                TacticalRequirementOutcome.Unknown => new(
                    "The prerequisite must be confirmed manually.",
                    "必須手動確認該項前置需求。"),
                TacticalRequirementOutcome.Unsupported => new(
                    "The current verified rules do not support this prerequisite.",
                    "目前已驗證規則不支援該項前置需求。"),
                TacticalRequirementOutcome.Conflicting => new(
                    "The prerequisite has conflicting evidence.",
                    "該項前置需求的證據互相衝突。"),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(requirement),
                    requirement.Outcome,
                    "Unknown tactical requirement outcome.")
            });

    private static IReadOnlyList<TacticalGapViewModel> MapGaps(
        TacticalCombatResponse response)
    {
        var gaps = new List<TacticalGapViewModel>();
        if (response.TargetChain is not null)
        {
            foreach (var transition in response.TargetChain.Transitions
                .Where(item => item.Applicability !=
                    TacticalRuleApplicability.Applicable))
            {
                gaps.Add(new TacticalGapViewModel(
                    transition.Applicability switch
                    {
                        TacticalRuleApplicability.Conflicting =>
                            TacticalConditionPresentationState.Conflicting,
                        TacticalRuleApplicability.Contrary =>
                            TacticalConditionPresentationState.Unsatisfied,
                        _ => TacticalConditionPresentationState
                            .NeedsConfirmation
                    },
                    PurposeText(transition.Purpose),
                    new BilingualText(
                        "This transition and every dependent plan branch remain inactive or conditional.",
                        "此轉換及所有依賴它的計畫分支仍未啟用或屬條件式。")));
            }
        }

        if (response.ExecutionContext is not null)
        {
            foreach (var fact in response.ExecutionContext.Current
                .Concat(response.ExecutionContext.Proposed)
                .Where(item => item.State != TacticalContextFactState.Available)
                .DistinctBy(item => item.State)
                .Take(3))
            {
                gaps.Add(new TacticalGapViewModel(
                    fact.State switch
                    {
                        TacticalContextFactState.Conflicting =>
                            TacticalConditionPresentationState.Conflicting,
                        TacticalContextFactState.Unsupported =>
                            TacticalConditionPresentationState.Unsupported,
                        _ => TacticalConditionPresentationState
                            .NeedsConfirmation
                    },
                    new BilingualText(
                        "A required execution-context value is not confirmed.",
                        "一項必要的執行情境值尚未確認。"),
                    new BilingualText(
                        "Affected candidates and steps remain unresolved instead of using a default.",
                        "受影響的候選方案及步驟維持未解決，不會使用預設值。")));
            }
        }

        return gaps.Take(8).ToArray();
    }

    private static TacticalSearchSummaryViewModel MapSearch(
        TacticalSearchResponse search)
    {
        var coverage = search.Coverage;
        var bound = coverage.FirstTerminator switch
        {
            TacticalSearchTerminator.OptionLimit =>
                coverage.Bounds.MaximumOptions,
            TacticalSearchTerminator.ExplorationLimit =>
                coverage.Bounds.MaximumExploredCombinations,
            TacticalSearchTerminator.TimeLimit =>
                coverage.Bounds.MaximumElapsedMilliseconds,
            TacticalSearchTerminator.ResultLimit =>
                coverage.Bounds.MaximumResults,
            _ => 0
        };
        return new TacticalSearchSummaryViewModel(
            search.IsComplete,
            coverage.CandidateUniverseCount,
            coverage.AdmittedCount,
            coverage.RejectedCount,
            coverage.UnsupportedCount,
            coverage.IrrelevantCount,
            coverage.DominatedCount,
            coverage.ExploredCombinationCount,
            coverage.FeasibleResultCount,
            coverage.RetainedResultCount,
            coverage.FirstTerminator,
            bound);
    }

    private static TacticalScoreComponentViewModel MapScore(
        TacticalScoreComponentResponse component) => new(
            component.Kind,
            component.State,
            component.BaseWeight,
            component.AppliedWeight,
            component.NormalizedValue,
            component.Contribution,
            ScoreMeaning(component.Kind),
            component.State == TacticalScoreComponentState.Available
                ? new BilingualText(
                    "Contribution applies only to this verified result.",
                    "此貢獻只適用於本次已驗證結果。")
                : new BilingualText(
                    "Evidence is unavailable, so this component is excluded rather than treated as zero.",
                    "證據無法取得，因此此項會被排除，而非視為零。"));

    private static BilingualText ScoreMeaning(
        TacticalScoreComponentKind kind) => kind switch
        {
            TacticalScoreComponentKind.CausalValue => new(
                "Supported marginal contribution to the applicable target chain.",
                "對適用目標因果鏈的受支援邊際貢獻。"),
            TacticalScoreComponentKind.LayeredProtection => new(
                "Separately verified protection or fallback interaction.",
                "另行驗證的保護或後備互動。"),
            TacticalScoreComponentKind.TimingOpportunity => new(
                "Whether the verified timing window can be recognized.",
                "能否辨識已驗證的時機窗口。"),
            TacticalScoreComponentKind.ExecutionReliability => new(
                "Known preparation, resources, and execution requirements.",
                "已知的準備、資源及執行需求。"),
            TacticalScoreComponentKind.RecoveryCost => new(
                "Verified self-lock, resource, and recovery consequences.",
                "已驗證的自我封鎖、資源及恢復後果。"),
            TacticalScoreComponentKind.FinishPath => new(
                "Separately supported attack route and finish condition.",
                "另行支援的攻擊路徑及收尾條件。"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    private static IReadOnlyList<TacticalCandidateGroupViewModel> MapCandidates(
        TacticalCombatResponse response,
        IReadOnlyDictionary<int, string> skillNames)
    {
        if (response.CandidateDiscovery is null)
        {
            return [];
        }

        var selected = response.SelectedLoadout?.SelectedCandidates
            .ToHashSet(StringComparer.Ordinal)
            ?? new HashSet<string>(StringComparer.Ordinal);
        var decisions = response.Search?.CandidateDecisions.ToDictionary(
            item => item.Identity,
            StringComparer.Ordinal)
            ?? new Dictionary<string, TacticalSearchCandidateResponse>(
                StringComparer.Ordinal);
        return response.CandidateDiscovery.Candidates
            .Select(candidate =>
            {
                decisions.TryGetValue(candidate.Identity, out var decision);
                var group = selected.Contains(candidate.Identity)
                    ? TacticalCandidatePresentationGroup.Selected
                    : MapCandidateGroup(decision?.Decision
                        ?? candidate.Decision);
                return (group, candidate: new TacticalCandidateViewModel(
                    skillNames.TryGetValue(candidate.SkillId, out var name)
                        ? name
                        : "Unnamed skill",
                    candidate.Category,
                    candidate.Direction,
                    candidate.RequiresBreakthrough,
                    CandidateReason(group)));
            })
            .GroupBy(item => item.group)
            .OrderBy(group => group.Key)
            .Select(group => new TacticalCandidateGroupViewModel(
                group.Key,
                group.Select(item => item.candidate).ToArray()))
            .ToArray();
    }

    private static TacticalCandidatePresentationGroup MapCandidateGroup(
        TacticalCandidateDecision decision) => decision switch
        {
            TacticalCandidateDecision.Admitted =>
                TacticalCandidatePresentationGroup.AdmittedAlternative,
            TacticalCandidateDecision.Rejected =>
                TacticalCandidatePresentationGroup.Rejected,
            TacticalCandidateDecision.Unsupported =>
                TacticalCandidatePresentationGroup.Unsupported,
            TacticalCandidateDecision.Irrelevant =>
                TacticalCandidatePresentationGroup.Irrelevant,
            TacticalCandidateDecision.Dominated =>
                TacticalCandidatePresentationGroup.Dominated,
            _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, null)
        };

    private static BilingualText CandidateReason(
        TacticalCandidatePresentationGroup group) => group switch
        {
            TacticalCandidatePresentationGroup.Selected => new(
                "Selected in the displayed feasible loadout.",
                "已選入目前顯示的可行運功方案。"),
            TacticalCandidatePresentationGroup.AdmittedAlternative => new(
                "Passed the known hard gates but was not selected.",
                "已通過已知硬性條件，但未被選取。"),
            TacticalCandidatePresentationGroup.Rejected => new(
                "A hard feasibility requirement failed.",
                "未通過一項硬性可行性需求。"),
            TacticalCandidatePresentationGroup.Unsupported => new(
                "No verified tactical role or effect supports this option.",
                "沒有已驗證的戰術角色或效果支援此選項。"),
            TacticalCandidatePresentationGroup.Irrelevant => new(
                "A proof shows no contribution to the selected target chain.",
                "證明顯示此選項不會貢獻於所選目標因果鏈。"),
            TacticalCandidatePresentationGroup.Dominated => new(
                "A documented option is strictly preferable in this exact context.",
                "在此精確情境中，另一個有記錄的選項嚴格更合適。"),
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
        };

    private static IEnumerable<TacticalEvidenceResponse> CollectEvidence(
        TacticalCombatResponse response)
    {
        if (response.TargetChain is not null)
        {
            foreach (var value in response.TargetChain.Transitions
                .SelectMany(item => item.Evidence)
                .Concat(response.TargetChain.Roles.SelectMany(item =>
                    item.Evidence)))
            {
                yield return value;
            }
        }

        if (response.Plan is not null)
        {
            foreach (var value in response.Plan.SharedEvidence
                .Concat(response.Plan.Stages.SelectMany(stage =>
                    stage.Evidence))
                .Concat(response.Plan.Stages.SelectMany(stage =>
                    stage.Steps).SelectMany(step => step.Evidence)))
            {
                yield return value;
            }
        }
    }

    private static TacticalEvidenceSummaryViewModel MapEvidence(
        TacticalEvidenceResponse evidence) => new(
            evidence.Source,
            evidence.GameDataVersion,
            evidence.RuleVersion,
            evidence.ScopeIdentity.Contains("EXACT", StringComparison.Ordinal)
                ? new BilingualText(
                    "Exact-target scope",
                    "精確目標範圍")
                : new BilingualText(
                    "Broad verified-rule scope",
                    "廣泛的已驗證規則範圍"));

}
