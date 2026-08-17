using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybookComposition;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;
using TaiWuAPI.Localization;

namespace TaiWuAPI.Presentation;

public static class TargetStrategyViewModelMapper
{
    public static TargetStrategyViewModel Map(
        TargetPlaybookPersonalization value,
        DateTimeOffset capturedAtUtc,
        TaiwuLanguage language,
        IReadOnlyList<ThreatViewModel> threats,
        IReadOnlyDictionary<int, string> playerSkillNames,
        IReadOnlyDictionary<int, string> targetSkillNames,
        bool currentLoadoutAlreadySatisfies)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(threats);
        ArgumentNullException.ThrowIfNull(playerSkillNames);
        ArgumentNullException.ThrowIfNull(targetSkillNames);
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(nameof(language));
        }

        var profile = value.Analysis.Profile;
        var facets = profile.Facets
            .ToDictionary(facet => FacetKey(facet.Identity));
        var profileGroups = profile.Facets
            .GroupBy(facet => facet.Identity.Dimension
                == TargetProfileDimension.AttackFamily
                    ? TargetProfileGroupKind.Context
                    : TargetProfileGroupKind.Mechanics)
            .OrderBy(group => group.Key)
            .Select(group => new TargetProfileGroupViewModel(
                group.Key,
                group.Key == TargetProfileGroupKind.Context
                    ? TargetStrategyUiText.Bilingual(
                        language,
                        "Attack-family context",
                        "攻擊類型背景")
                    : TargetStrategyUiText.Bilingual(
                        language,
                        "Verified combat mechanics",
                        "已驗證的戰鬥機制"),
                [.. group.Select(facet => MapFacet(facet, language))]))
            .ToArray();
        var archetypes = value.Analysis.ArchetypeMatches.Matches
            .OrderBy(match => MatchOrder(match.State))
            .ThenBy(match => match.Definition.Identity.Code,
                StringComparer.Ordinal)
            .Select(match => MapArchetype(
                match,
                profile.RuleVersion.Value,
                facets,
                language))
            .ToArray();
        var evidenceSkillNames = targetSkillNames
            .Concat(playerSkillNames.Where(pair =>
                !targetSkillNames.ContainsKey(pair.Key)))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var counters = MapCounters(value, language, evidenceSkillNames);
        var counterLinks = counters.ToDictionary(
            counter => counter.Code,
            counter => new TargetStrategyCounterLinkViewModel(
                counter.Anchor,
                counter.SkillName),
            StringComparer.Ordinal);
        var goals = value.Composition.Goals
            .Select(goal => MapGoal(
                goal,
                value.EligibleGoals.Any(eligible =>
                    string.Equals(
                        eligible.StableKey,
                        goal.StableKey,
                        StringComparison.Ordinal)),
                counterLinks,
                threats,
                language))
            .ToArray();
        var displayedGapKeys = value.Composition.Goals
            .SelectMany(goal => goal.KnownGaps)
            .Select(gap => gap.StableKey)
            .Concat(value.Counters
                .Where(counter => counter.Gap is not null)
                .Select(counter => counter.Gap!.StableKey))
            .ToHashSet(StringComparer.Ordinal);
        var standaloneGaps = value.Gaps
            .Where(gap => !displayedGapKeys.Contains(gap.StableKey))
            .Select(gap => MapGap(gap, language))
            .ToArray();
        var adjustmentMapper = new TargetAdjustmentViewModelMapper(
            facets,
            archetypes,
            goals,
            counters,
            value.Gaps,
            threats,
            evidenceSkillNames,
            language);
        var adjustments = value.Adjustments.Adjustments
            .Select(adjustmentMapper.Map)
            .ToArray();
        var feasibleCounterCount = counters.Count(counter =>
            counter.Availability
                == TargetPlaybookCounterAvailabilityState.Feasible);
        var feasibility = new TargetStrategyFeasibilityViewModel(
            FeasibilitySummary(
                counters.Length,
                feasibleCounterCount,
                currentLoadoutAlreadySatisfies,
                language),
            currentLoadoutAlreadySatisfies,
            feasibleCounterCount,
            counters.Length - feasibleCounterCount);
        var matchedCount = archetypes.Count(archetype => archetype.State
            == TargetArchetypeMatchState.Matched);
        var status = StrategyStatus(archetypes);

        return new TargetStrategyViewModel(
            status,
            StatusLabel(status, language),
            StatusSummary(status, matchedCount, language),
            capturedAtUtc,
            profile.RuleVersion.Value,
            profile.Facets
                .SelectMany(facet => facet.Evidence)
                .Select(evidence => evidence.Reference)
                .Concat(profile.Diagnostics.SelectMany(diagnostic =>
                    diagnostic.EvidenceReferences))
                .Distinct(StringComparer.Ordinal)
                .Count(),
            matchedCount,
            profileGroups,
            archetypes,
            goals,
            counters,
            standaloneGaps,
            adjustments,
            feasibility);
    }

    private static TargetProfileFacetSummaryViewModel MapFacet(
        TargetProfileFacet facet,
        TaiwuLanguage language)
    {
        var sources = facet.Evidence
            .Select(evidence => evidence.SourceKind)
            .Distinct()
            .Order()
            .Select(kind => TargetStrategyUiText.EvidenceSource(
                language,
                kind))
            .ToArray();
        return new TargetProfileFacetSummaryViewModel(
            $"profile-facet:{FacetKey(facet.Identity)}",
            facet.Identity.Dimension,
            TargetStrategyUiText.Dimension(
                language,
                facet.Identity.Dimension),
            TargetStrategyUiText.Facet(language, facet.Identity.Code),
            facet.State,
            TargetStrategyUiText.ProfileState(language, facet.State),
            MapValue(facet.Value, language),
            facet.Evidence.Length,
            sources.Length == 0
                ? TargetStrategyUiText.Bilingual(
                    language,
                    "Evidence source unavailable",
                    "無法取得證據來源")
                : string.Join(" · ", sources));
    }

    private static string? MapValue(
        TargetProfileFacetValue? value,
        TaiwuLanguage language)
    {
        if (value is null || value.Measurements.IsEmpty)
        {
            return null;
        }

        return string.Join(
            " · ",
            value.Measurements.Select(measurement =>
                $"{MeasurementLabel(language, measurement.Code)} "
                + measurement.Value));
    }

    private static string MeasurementLabel(
        TaiwuLanguage language,
        string code) => code switch
        {
            "OUTER" => TargetStrategyUiText.Bilingual(
                language,
                "Outer",
                "外傷抗性"),
            "INNER" => TargetStrategyUiText.Bilingual(
                language,
                "Inner",
                "內傷抗性"),
            _ => TargetStrategyUiText.Bilingual(
                language,
                "Verified value",
                "已驗證數值")
        };

    private static TargetArchetypeSummaryViewModel MapArchetype(
        TargetArchetypeMatch match,
        string profileVersion,
        IReadOnlyDictionary<string, TargetProfileFacet> facets,
        TaiwuLanguage language)
    {
        var related = match.SupportingFacets
            .Concat(match.MissingFacets)
            .Concat(match.ExcludingFacets)
            .Concat(match.ConflictingFacets)
            .Select(FacetKey)
            .Where(facets.ContainsKey)
            .SelectMany(key => facets[key].Evidence.Select(evidence =>
                evidence.Reference))
            .Concat(match.Definition.EvidenceReferences)
            .Distinct(StringComparer.Ordinal)
            .Count();
        return new TargetArchetypeSummaryViewModel(
            match.Definition.Identity.Code,
            TargetStrategyUiText.Archetype(
                language,
                match.Definition.Identity.Code),
            match.State,
            TargetStrategyUiText.MatchState(language, match.State),
            TargetStrategyUiText.Bilingual(
                language,
                $"Archetype {match.Definition.Identity.Version.Value} · "
                    + $"profile {profileVersion}",
                $"類型規則 {match.Definition.Identity.Version.Value} · "
                    + $"特徵規則 {profileVersion}"),
            related,
            EvidenceCount(language, related),
            FacetTitles(match.SupportingFacets, language),
            FacetTitles(match.MissingFacets, language),
            FacetTitles(match.ExcludingFacets, language),
            FacetTitles(match.ConflictingFacets, language));
    }

    private static string[] FacetTitles(
        IEnumerable<TargetProfileFacetIdentity> facets,
        TaiwuLanguage language) =>
    [
        .. facets.Select(facet => TargetStrategyUiText.Facet(
            language,
            facet.Code))
    ];

    private static TargetCounterSummaryViewModel[] MapCounters(
        TargetPlaybookPersonalization value,
        TaiwuLanguage language,
        IReadOnlyDictionary<int, string> skillNames)
    {
        var availability = value.Counters.ToDictionary(
            counter => counter.Option.StableKey,
            StringComparer.Ordinal);
        return
        [
            .. value.Composition.Options.Select((option, index) =>
            {
                availability.TryGetValue(option.StableKey, out var current);
                var state = current?.State
                    ?? TargetPlaybookCounterAvailabilityState.Unresolved;
                return new TargetCounterSummaryViewModel(
                    option.StableKey,
                    $"target-counter-{index + 1}",
                    option.Effect.SkillId,
                    option.Effect.SkillName,
                    $"/skills/{option.Effect.SkillId}"
                        + "?context=recommendation",
                    TargetStrategyUiText.Direction(
                        language,
                        option.CounterRule.RequiredDirection),
                    state,
                    TargetStrategyUiText.Availability(language, state),
                    CounterFeasibilityExplanation(
                        current,
                        state,
                        skillNames,
                        language),
                    [.. option.Requirements.Select(requirement =>
                        RequirementSummary(
                            requirement,
                            option.Effect.SkillName,
                            language))],
                    current?.Gap is null
                        ? null
                        : MapGap(current.Gap, language));
            })
        ];
    }

    private static TargetResponseGoalViewModel MapGoal(
        ComposedTargetResponseGoal goal,
        bool isEligible,
        IReadOnlyDictionary<string, TargetStrategyCounterLinkViewModel>
            counters,
        IReadOnlyList<ThreatViewModel> threats,
        TaiwuLanguage language) => new(
        goal.Code,
        TargetStrategyUiText.Goal(language, goal.Code),
        TargetStrategyUiText.Priority(language, goal.Priority),
        TargetStrategyUiText.Timing(language, goal.ResponseTiming),
        isEligible,
        [.. goal.Threats.Select(threat =>
        {
            var mapped = threats.SingleOrDefault(value => string.Equals(
                value.Code,
                threat.Code,
                StringComparison.Ordinal));
            return new TargetStrategyThreatLinkViewModel(
                mapped?.Reference ?? $"threat:{threat.Code}",
                mapped?.Title ?? threat.Title);
        })],
        [.. goal.Options
            .Where(option => counters.ContainsKey(option.StableKey))
            .Select(option => counters[option.StableKey])],
        [.. goal.KnownGaps.Select(gap => MapGap(gap, language))]);

    private static string CounterFeasibilityExplanation(
        TargetPlaybookCounterAvailability? counter,
        TargetPlaybookCounterAvailabilityState state,
        IReadOnlyDictionary<int, string> skillNames,
        TaiwuLanguage language)
    {
        if (counter is null)
        {
            return TargetStrategyUiText.Bilingual(
                language,
                "Exact-target evidence did not make this counter eligible "
                    + "for player feasibility checks.",
                "精確目標證據未令此應對功法進入角色可行性檢查。");
        }

        if (state == TargetPlaybookCounterAvailabilityState.Feasible)
        {
            return TargetStrategyUiText.Bilingual(
                language,
                "Passed player access checks and fits at least one "
                    + "generated loadout.",
                "已通過角色取得條件，並可放入至少一套產生的運功方案。");
        }

        var reasons = counter.Access.Issues
            .Select(issue => UiEntityText.UseNames(
                UiText.Get(language, issue.Reason),
                skillNames))
            .Concat(counter.Diagnostics.Select(diagnostic =>
                UiEntityText.UseNames(
                    UiText.Get(language, diagnostic.Reason),
                    skillNames)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (reasons.Length > 0)
        {
            return string.Join(" · ", reasons);
        }

        return state switch
        {
            TargetPlaybookCounterAvailabilityState.Inaccessible =>
                TargetStrategyUiText.Bilingual(
                    language,
                    "The player does not currently meet the verified access "
                        + "requirements.",
                    "目前角色未符合已驗證的取得條件。"),
            TargetPlaybookCounterAvailabilityState.Infeasible =>
                TargetStrategyUiText.Bilingual(
                    language,
                    "The counter is accessible but does not fit a generated "
                        + "legal loadout.",
                    "角色可取得此功法，但無法放入已產生的合法運功方案。"),
            TargetPlaybookCounterAvailabilityState.Unresolved =>
                TargetStrategyUiText.Bilingual(
                    language,
                    "Candidate-search limits leave player feasibility "
                        + "unresolved.",
                    "候選搜尋限制令角色可行性仍未確定。"),
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
    }

    private static string FeasibilitySummary(
        int counterCount,
        int feasibleCount,
        bool currentLoadoutAlreadySatisfies,
        TaiwuLanguage language)
    {
        if (currentLoadoutAlreadySatisfies)
        {
            return TargetStrategyUiText.Bilingual(
                language,
                "The final recommendation is unchanged because the current "
                    + "loadout already satisfies the composed response.",
                "目前運功已滿足合成應對策略，因此最終推薦無需改變。");
        }

        if (counterCount == 0)
        {
            return TargetStrategyUiText.Bilingual(
                language,
                "No verified counter reached player feasibility filtering.",
                "沒有已驗證應對功法進入角色可行性篩選。");
        }

        return TargetStrategyUiText.Bilingual(
            language,
            $"{feasibleCount} of {counterCount} verified counter options "
                + "pass the current player's feasibility checks.",
            $"{counterCount} 項已驗證應對功法中，有 {feasibleCount} 項通過目前角色的可行性檢查。");
    }

    private static TargetStrategyGapViewModel MapGap(
        TargetCounterPlaybookGap gap,
        TaiwuLanguage language) => new(
        gap.Code,
        TargetStrategyUiText.Gap(language, gap.LocalizedMessageKey));

    private static string RequirementSummary(
        CombatRequirement requirement,
        string skillName,
        TaiwuLanguage language) => requirement switch
        {
            WeaponRequirement => TargetStrategyUiText.Bilingual(
                language,
                "Verified weapon required",
                "需要指定武器"),
            TrickRequirement value => TargetStrategyUiText.Bilingual(
                language,
                $"Requires {value.MinimumCount} matching trick(s)",
                $"需要 {value.MinimumCount} 個相符招式"),
            RangeRequirement value => RangeSummary(value, language),
            ResourceRequirement value => TargetStrategyUiText.Bilingual(
                language,
                $"Requires {value.MinimumAmount} "
                    + ResourceLabel(language, value.Resource),
                $"需要 {value.MinimumAmount} "
                    + ResourceLabel(language, value.Resource)),
            WeaponUnlockRequirement => TargetStrategyUiText.Bilingual(
                language,
                "Verified weapon unlock required",
                "需要指定武器解封"),
            SkillActivationRequirement value => TargetStrategyUiText.Bilingual(
                language,
                $"{skillName}: {ActivationLabel(language, value.RequiredState)}",
                $"{skillName}：{ActivationLabel(language, value.RequiredState)}"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(requirement),
                requirement.GetType().Name,
                "Unknown combat requirement type.")
        };

    private static string RangeSummary(
        RangeRequirement value,
        TaiwuLanguage language)
    {
        var range = value.MinimumInclusive.HasValue
            && value.MaximumInclusive.HasValue
            ? $"{value.MinimumInclusive}–{value.MaximumInclusive}"
            : value.MinimumInclusive.HasValue
                ? $"≥ {value.MinimumInclusive}"
                : $"≤ {value.MaximumInclusive}";
        return TargetStrategyUiText.Bilingual(
            language,
            $"Required combat range: {range}",
            $"需要戰鬥距離：{range}");
    }

    private static string ResourceLabel(
        TaiwuLanguage language,
        CombatResourceKind resource) => resource switch
        {
            CombatResourceKind.Neili => TargetStrategyUiText.Bilingual(
                language,
                "inner power",
                "內力"),
            CombatResourceKind.Stance => TargetStrategyUiText.Bilingual(
                language,
                "stance",
                "架勢"),
            CombatResourceKind.Breath => TargetStrategyUiText.Bilingual(
                language,
                "breath",
                "提氣"),
            _ => throw new ArgumentOutOfRangeException(nameof(resource))
        };

    private static string ActivationLabel(
        TaiwuLanguage language,
        SkillActivationState state) => state switch
        {
            SkillActivationState.EquippedPassive =>
                TargetStrategyUiText.Bilingual(
                    language,
                    "equip as a passive",
                    "裝備為被動功法"),
            SkillActivationState.ActiveDefense =>
                TargetStrategyUiText.Bilingual(
                    language,
                    "use as active defense",
                    "設為主動護體"),
            SkillActivationState.ActiveAgility =>
                TargetStrategyUiText.Bilingual(
                    language,
                    "use as active agility",
                    "設為主動輕靈"),
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

    private static TargetStrategyStatus StrategyStatus(
        IReadOnlyList<TargetArchetypeSummaryViewModel> archetypes)
    {
        if (archetypes.Any(value => value.State
            == TargetArchetypeMatchState.Matched))
        {
            return TargetStrategyStatus.Available;
        }

        if (archetypes.Any(value => value.State
            == TargetArchetypeMatchState.Conflicting))
        {
            return TargetStrategyStatus.Conflicting;
        }

        if (archetypes.Any(value => value.State
            == TargetArchetypeMatchState.Partial))
        {
            return TargetStrategyStatus.Partial;
        }

        return archetypes.Any(value => value.State
            == TargetArchetypeMatchState.Unsupported)
                ? TargetStrategyStatus.Unsupported
                : TargetStrategyStatus.NoMatch;
    }

    private static string StatusLabel(
        TargetStrategyStatus status,
        TaiwuLanguage language) => status switch
        {
            TargetStrategyStatus.Available => TargetStrategyUiText.Bilingual(
                language,
                "Playbook available",
                "已有可用策略"),
            TargetStrategyStatus.Partial => TargetStrategyUiText.Bilingual(
                language,
                "Partial profile",
                "特徵資料不完整"),
            TargetStrategyStatus.Unsupported =>
                TargetStrategyUiText.Bilingual(
                    language,
                    "Unsupported version",
                    "版本不支援"),
            TargetStrategyStatus.Conflicting =>
                TargetStrategyUiText.Bilingual(
                    language,
                    "Evidence conflict",
                    "證據衝突"),
            TargetStrategyStatus.NoMatch => TargetStrategyUiText.Bilingual(
                language,
                "No verified match",
                "沒有已驗證的匹配"),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

    private static string StatusSummary(
        TargetStrategyStatus status,
        int matchedCount,
        TaiwuLanguage language) => status switch
        {
            TargetStrategyStatus.Available when matchedCount > 1 =>
                TargetStrategyUiText.Bilingual(
                    language,
                    $"{matchedCount} verified target patterns combine into "
                        + "one reusable response strategy.",
                    $"{matchedCount} 個已驗證目標類型合併為一套可重用的應對策略。"),
            TargetStrategyStatus.Available => TargetStrategyUiText.Bilingual(
                language,
                "One verified target pattern provides a reusable response "
                    + "strategy.",
                "一個已驗證目標類型提供可重用的應對策略。"),
            TargetStrategyStatus.Partial => TargetStrategyUiText.Bilingual(
                language,
                "Some facts match, but missing evidence prevents a verified "
                    + "playbook.",
                "部分特徵相符，但缺少證據，暫時不能建立已驗證策略。"),
            TargetStrategyStatus.Unsupported =>
                TargetStrategyUiText.Bilingual(
                    language,
                    "The installed data version is outside the verified "
                        + "profile rules.",
                    "目前安裝資料版本不在已驗證的特徵規則範圍內。"),
            TargetStrategyStatus.Conflicting =>
                TargetStrategyUiText.Bilingual(
                    language,
                    "Conflicting exact evidence prevents a mechanical "
                        + "playbook claim.",
                    "精確證據互相衝突，因此不能宣稱已確定的機械策略。"),
            TargetStrategyStatus.NoMatch => TargetStrategyUiText.Bilingual(
                language,
                "No evidence-backed target pattern matched this snapshot.",
                "此快照沒有匹配任何具證據支持的目標類型。"),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

    private static string EvidenceCount(
        TaiwuLanguage language,
        int count) => TargetStrategyUiText.Bilingual(
        language,
        count == 1 ? "1 evidence source" : $"{count} evidence sources",
        $"{count} 項證據來源");

    private static int MatchOrder(TargetArchetypeMatchState state) => state switch
    {
        TargetArchetypeMatchState.Matched => 0,
        TargetArchetypeMatchState.Partial => 1,
        TargetArchetypeMatchState.Unsupported => 2,
        TargetArchetypeMatchState.Conflicting => 3,
        TargetArchetypeMatchState.NotMatched => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };

    private static string FacetKey(TargetProfileFacetIdentity facet) =>
        $"{(int)facet.Dimension}:{facet.Code}";
}
