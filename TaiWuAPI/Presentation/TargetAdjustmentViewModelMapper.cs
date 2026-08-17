using TaiWu.Application.Localization;
using TaiWu.Domain.TargetPlaybookComposition;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;
using TaiWuAPI.Localization;

namespace TaiWuAPI.Presentation;

internal sealed class TargetAdjustmentViewModelMapper
{
    private readonly IReadOnlyDictionary<string, TargetProfileFacet> _facets;
    private readonly IReadOnlyList<TargetArchetypeSummaryViewModel> _archetypes;
    private readonly IReadOnlyList<TargetResponseGoalViewModel> _goals;
    private readonly IReadOnlyList<TargetCounterSummaryViewModel> _counters;
    private readonly IReadOnlyList<TargetCounterPlaybookGap> _gaps;
    private readonly IReadOnlyList<ThreatViewModel> _threats;
    private readonly IReadOnlyDictionary<int, string> _skillNames;
    private readonly TaiwuLanguage _language;

    public TargetAdjustmentViewModelMapper(
        IReadOnlyDictionary<string, TargetProfileFacet> facets,
        IReadOnlyList<TargetArchetypeSummaryViewModel> archetypes,
        IReadOnlyList<TargetResponseGoalViewModel> goals,
        IReadOnlyList<TargetCounterSummaryViewModel> counters,
        IEnumerable<TargetCounterPlaybookGap> gaps,
        IReadOnlyList<ThreatViewModel> threats,
        IReadOnlyDictionary<int, string> skillNames,
        TaiwuLanguage language)
    {
        ArgumentNullException.ThrowIfNull(facets);
        ArgumentNullException.ThrowIfNull(archetypes);
        ArgumentNullException.ThrowIfNull(goals);
        ArgumentNullException.ThrowIfNull(counters);
        ArgumentNullException.ThrowIfNull(gaps);
        ArgumentNullException.ThrowIfNull(threats);
        ArgumentNullException.ThrowIfNull(skillNames);
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(nameof(language));
        }

        _facets = facets;
        _archetypes = archetypes;
        _goals = goals;
        _counters = counters;
        _gaps = [.. gaps];
        _threats = threats;
        _skillNames = skillNames;
        _language = language;
    }

    public TargetAdjustmentExplanationViewModel Map(
        TargetPlaybookAdjustment adjustment)
    {
        ArgumentNullException.ThrowIfNull(adjustment);
        var original = MapReference(adjustment.OriginalResponse);
        var result = MapReference(adjustment.ResultResponse);
        return new TargetAdjustmentExplanationViewModel(
            adjustment.Action,
            TargetStrategyUiText.AdjustmentAction(
                _language,
                adjustment.Action),
            AdjustmentSummary(adjustment.Action),
            TargetStrategyUiText.AdjustmentReason(
                _language,
                adjustment.ReasonCode),
            original,
            result,
            [.. adjustment.Evidence.Select(MapEvidence)]);
    }

    private TargetAdjustmentReferenceViewModel? MapReference(
        TargetPlaybookResponseReference? response)
    {
        if (response is null)
        {
            return null;
        }

        return response.Kind switch
        {
            TargetPlaybookResponseReferenceKind.Goal =>
                _goals.SingleOrDefault(goal => string.Equals(
                    goal.Code,
                    response.StableCode,
                    StringComparison.Ordinal)) is { } goal
                    ? new TargetAdjustmentReferenceViewModel(
                        goal.Title,
                        $"#target-goal-{goal.Code}")
                    : new TargetAdjustmentReferenceViewModel(
                        TargetStrategyUiText.Goal(
                            _language,
                            response.StableCode),
                        Href: null),
            TargetPlaybookResponseReferenceKind.Option =>
                _counters.SingleOrDefault(counter => string.Equals(
                    counter.Code,
                    response.StableCode,
                    StringComparison.Ordinal)) is { } counter
                    ? new TargetAdjustmentReferenceViewModel(
                        counter.SkillName,
                        $"#{counter.Anchor}")
                    : new TargetAdjustmentReferenceViewModel(
                        TargetStrategyUiText.Bilingual(
                            _language,
                            "Verified counter option",
                            "已驗證應對選項"),
                        Href: null),
            TargetPlaybookResponseReferenceKind.Gap =>
                _gaps.SingleOrDefault(gap => string.Equals(
                    gap.Code,
                    response.StableCode,
                    StringComparison.Ordinal)) is { } gap
                    ? new TargetAdjustmentReferenceViewModel(
                        TargetStrategyUiText.Gap(
                            _language,
                            gap.LocalizedMessageKey),
                        $"#target-gap-{gap.Code}")
                    : new TargetAdjustmentReferenceViewModel(
                        TargetStrategyUiText.Gap(_language, string.Empty),
                        Href: null),
            TargetPlaybookResponseReferenceKind.Threat =>
                _threats.SingleOrDefault(threat => string.Equals(
                    threat.Code,
                    response.StableCode,
                    StringComparison.Ordinal)) is { } threat
                    ? new TargetAdjustmentReferenceViewModel(
                        threat.Title,
                        "#target-threats-heading",
                        threat.Reference)
                    : new TargetAdjustmentReferenceViewModel(
                        TargetStrategyUiText.Bilingual(
                            _language,
                            "Verified target threat",
                            "已驗證目標威脅"),
                        Href: null),
            _ => throw new ArgumentOutOfRangeException(nameof(response))
        };
    }

    private TargetAdjustmentEvidenceViewModel MapEvidence(
        TargetPlaybookAdjustmentEvidence evidence)
    {
        var (title, href, threatReference) = evidence.Kind switch
        {
            TargetPlaybookAdjustmentEvidenceKind.ProfileFacet =>
                FacetEvidence(evidence.Identity),
            TargetPlaybookAdjustmentEvidenceKind.Threat =>
                ThreatEvidence(evidence.Identity),
            TargetPlaybookAdjustmentEvidenceKind.Skill =>
                SkillEvidence(evidence.Identity),
            TargetPlaybookAdjustmentEvidenceKind.Effect =>
                EffectEvidence(evidence.Identity),
            TargetPlaybookAdjustmentEvidenceKind.Equipment =>
                FacetSourceEvidence(
                    evidence.Identity,
                    "Equipped-loadout evidence",
                    "已裝備運功證據"),
            TargetPlaybookAdjustmentEvidenceKind.Observation =>
                FacetSourceEvidence(
                    evidence.Identity,
                    "Current-screen observation",
                    "目前畫面觀察"),
            TargetPlaybookAdjustmentEvidenceKind.Gap =>
                GapEvidence(evidence.Identity),
            TargetPlaybookAdjustmentEvidenceKind.ArchetypeMatch =>
                ArchetypeEvidence(evidence.Identity),
            _ => throw new ArgumentOutOfRangeException(nameof(evidence))
        };
        return new TargetAdjustmentEvidenceViewModel(
            evidence.Kind,
            evidence.State,
            TargetStrategyUiText.AdjustmentEvidenceState(
                _language,
                evidence.State),
            title,
            href,
            threatReference,
            evidence.EvidenceReferences.Length);
    }

    private (string Title, string? Href, string? ThreatReference)
        FacetEvidence(string identity)
    {
        var key = RemovePrefix(identity, "FACET:");
        if (key is not null && _facets.TryGetValue(key, out var facet))
        {
            return (
                TargetStrategyUiText.Facet(_language, facet.Identity.Code),
                $"#profile-facet:{key}",
                null);
        }

        return (TargetStrategyUiText.Bilingual(
            _language,
            "Verified target fact",
            "已驗證目標特徵"), null, null);
    }

    private (string Title, string? Href, string? ThreatReference)
        ThreatEvidence(string identity)
    {
        var code = RemovePrefix(identity, "THREAT:");
        var threat = _threats.SingleOrDefault(value => string.Equals(
            value.Code,
            code,
            StringComparison.Ordinal));
        return threat is null
            ? (TargetStrategyUiText.Bilingual(
                _language,
                "Verified target threat",
                "已驗證目標威脅"), null, null)
            : (threat.Title, "#target-threats-heading", threat.Reference);
    }

    private (string Title, string? Href, string? ThreatReference)
        SkillEvidence(string identity)
    {
        var value = RemovePrefix(identity, "SKILL:");
        return int.TryParse(value, out var skillId)
            && _skillNames.TryGetValue(skillId, out var name)
                ? (name, $"/skills/{skillId}?context=recommendation", null)
                : (TargetStrategyUiText.Bilingual(
                    _language,
                    "Verified target skill",
                    "已驗證目標功法"), null, null);
    }

    private (string Title, string? Href, string? ThreatReference)
        EffectEvidence(string identity)
    {
        var parts = identity.Split(':');
        if (parts.Length >= 2
            && int.TryParse(parts[1], out var skillId)
            && _skillNames.TryGetValue(skillId, out var name))
        {
            return (TargetStrategyUiText.Bilingual(
                _language,
                $"Verified effect from {name}",
                $"{name} 的已驗證效果"),
                $"/skills/{skillId}?context=recommendation",
                null);
        }

        return (TargetStrategyUiText.Bilingual(
            _language,
            "Verified target effect",
            "已驗證目標效果"), null, null);
    }

    private (string Title, string? Href, string? ThreatReference)
        FacetSourceEvidence(
            string identity,
            string english,
            string chinese)
    {
        var marker = identity.IndexOf("FACET:", StringComparison.Ordinal);
        var key = marker < 0
            ? null
            : identity[(marker + "FACET:".Length)..];
        var source = TargetStrategyUiText.Bilingual(
            _language,
            english,
            chinese);
        if (key is not null && _facets.TryGetValue(key, out var facet))
        {
            return (
                $"{source} · "
                    + TargetStrategyUiText.Facet(
                        _language,
                        facet.Identity.Code),
                $"#profile-facet:{key}",
                null);
        }

        return (source, null, null);
    }

    private (string Title, string? Href, string? ThreatReference)
        GapEvidence(string identity)
    {
        var code = RemovePrefix(identity, "GAP:");
        var gap = _gaps.SingleOrDefault(value => string.Equals(
            value.Code,
            code,
            StringComparison.Ordinal));
        return gap is null
            ? (TargetStrategyUiText.Gap(_language, string.Empty), null, null)
            : (TargetStrategyUiText.Gap(
                _language,
                gap.LocalizedMessageKey),
                $"#target-gap-{gap.Code}",
                null);
    }

    private (string Title, string? Href, string? ThreatReference)
        ArchetypeEvidence(string identity)
    {
        var code = RemovePrefix(identity, "ARCHETYPE:");
        var archetype = _archetypes.SingleOrDefault(value => string.Equals(
            value.Code,
            code,
            StringComparison.Ordinal));
        return archetype is null
            ? (TargetStrategyUiText.Bilingual(
                _language,
                "Verified target pattern",
                "已驗證目標類型"), null, null)
            : (archetype.Title, $"#target-archetype-{archetype.Code}", null);
    }

    private string AdjustmentSummary(TargetPlaybookAdjustmentAction action) =>
        action switch
        {
            TargetPlaybookAdjustmentAction.Retained =>
                TargetStrategyUiText.Bilingual(
                    _language,
                    "Keep this reusable response for the target.",
                    "為此目標保留這項可重用應對。"),
            TargetPlaybookAdjustmentAction.Elevated =>
                TargetStrategyUiText.Bilingual(
                    _language,
                    "Raise this reusable response's priority.",
                    "提高這項可重用應對的優先度。"),
            TargetPlaybookAdjustmentAction.Reduced =>
                TargetStrategyUiText.Bilingual(
                    _language,
                    "Reduce this broad response's priority; exact "
                        + "contrary evidence remains visible.",
                    "降低這項廣泛應對的優先度；精確相反證據仍會保留。"),
            TargetPlaybookAdjustmentAction.Added =>
                TargetStrategyUiText.Bilingual(
                    _language,
                    "Add an exact-target response.",
                    "加入一項精確目標應對。"),
            TargetPlaybookAdjustmentAction.Replaced =>
                TargetStrategyUiText.Bilingual(
                    _language,
                    "Replace the reusable response with an exact-target "
                        + "response.",
                    "以精確目標應對取代可重用應對。"),
            TargetPlaybookAdjustmentAction.Unresolved =>
                TargetStrategyUiText.Bilingual(
                    _language,
                    "Leave this response unresolved; this is not "
                        + "completed mitigation.",
                    "這項應對仍未解決；這不代表已完成緩解。"),
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };

    private static string? RemovePrefix(string value, string prefix) =>
        value.StartsWith(prefix, StringComparison.Ordinal)
            ? value[prefix.Length..]
            : null;
}
