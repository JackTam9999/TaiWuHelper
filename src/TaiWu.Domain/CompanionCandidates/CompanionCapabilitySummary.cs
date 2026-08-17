using System.Collections.Immutable;

namespace TaiWu.Domain.CompanionCandidates;

public enum CompanionCapabilitySummaryState
{
    Complete = 0,
    Incomplete = 1,
    Unsupported = 2,
    Stale = 3,
    Conflicting = 4
}

public enum CompanionCapabilityCategory
{
    MainAttributes = 0,
    MartialDisciplines = 1,
    LifeSkillDisciplines = 2
}

public enum CompanionCapabilitySummaryFormula
{
    EqualCategoryMean = 0
}

public sealed record CompanionCapabilityComponent
{
    internal CompanionCapabilityComponent(
        CandidateProfileFieldIdentity field,
        CandidateEvidenceState? evidenceState,
        short? value)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
        if (evidenceState.HasValue
            && !Enum.IsDefined(evidenceState.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(evidenceState),
                evidenceState,
                "Unknown capability-component evidence state.");
        }

        if ((evidenceState == CandidateEvidenceState.Confirmed) != value.HasValue)
        {
            throw new ArgumentException(
                "Only a confirmed capability component can carry a value.");
        }

        EvidenceState = evidenceState;
        Value = value;
    }

    public CandidateProfileFieldIdentity Field { get; }

    public CandidateEvidenceState? EvidenceState { get; }

    public short? Value { get; }
}

public sealed class CompanionCapabilityCategorySummary
{
    internal CompanionCapabilityCategorySummary(
        CompanionCapabilityCategory category,
        CompanionCapabilitySummaryState state,
        IEnumerable<CompanionCapabilityComponent> components,
        decimal? average)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unknown capability category.");
        }

        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknown capability summary state.");
        }

        ArgumentNullException.ThrowIfNull(components);
        var copied = components.ToImmutableArray();
        if (copied.Any(component => component is null)
            || copied.Select(component => component.Field.StableKey)
                .Distinct(StringComparer.Ordinal).Count() != copied.Length)
        {
            throw new ArgumentException(
                "Capability components cannot contain null or duplicate fields.",
                nameof(components));
        }

        if ((state == CompanionCapabilitySummaryState.Complete)
            != average.HasValue)
        {
            throw new ArgumentException(
                "Only a complete capability category can carry an average.",
                nameof(average));
        }

        Category = category;
        State = state;
        Components = copied;
        Average = average;
    }

    public CompanionCapabilityCategory Category { get; }

    public CompanionCapabilitySummaryState State { get; }

    public ImmutableArray<CompanionCapabilityComponent> Components { get; }

    public decimal? Average { get; }

    public int ConfirmedCount => Components.Count(component =>
        component.EvidenceState == CandidateEvidenceState.Confirmed);

    public int ExpectedCount => Components.Length;
}

public sealed class CompanionCapabilitySummary
{
    public const string RuleVersion = "1";
    public const int MainAttributeCount = 6;
    public const int MartialDisciplineCount = 14;
    public const int LifeSkillDisciplineCount = 16;

    internal CompanionCapabilitySummary(
        CompanionCapabilitySummaryState state,
        CompanionCapabilityCategorySummary mainAttributes,
        CompanionCapabilityCategorySummary martialDisciplines,
        CompanionCapabilityCategorySummary lifeSkillDisciplines,
        decimal? breadthIndex)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknown capability summary state.");
        }

        if ((state == CompanionCapabilitySummaryState.Complete)
            != breadthIndex.HasValue)
        {
            throw new ArgumentException(
                "Only a complete capability summary can carry a breadth index.",
                nameof(breadthIndex));
        }

        State = state;
        MainAttributes = mainAttributes
            ?? throw new ArgumentNullException(nameof(mainAttributes));
        MartialDisciplines = martialDisciplines
            ?? throw new ArgumentNullException(nameof(martialDisciplines));
        LifeSkillDisciplines = lifeSkillDisciplines
            ?? throw new ArgumentNullException(nameof(lifeSkillDisciplines));
        BreadthIndex = breadthIndex;
    }

    public CompanionCapabilitySummaryState State { get; }

    public string Version => RuleVersion;

    public CompanionCapabilitySummaryFormula Formula =>
        CompanionCapabilitySummaryFormula.EqualCategoryMean;

    public decimal? BreadthIndex { get; }

    public CompanionCapabilityCategorySummary MainAttributes { get; }

    public CompanionCapabilityCategorySummary MartialDisciplines { get; }

    public CompanionCapabilityCategorySummary LifeSkillDisciplines { get; }
}

public static class CompanionCapabilitySummaryBuilder
{
    public static CompanionCapabilitySummary Build(CandidateProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var mainAttributes = BuildCategory(
            profile,
            CompanionCapabilityCategory.MainAttributes,
            Enum.GetValues<CandidateMainAttribute>().Select(attribute =>
                new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseMainAttribute,
                    attribute)));
        var martial = BuildCategory(
            profile,
            CompanionCapabilityCategory.MartialDisciplines,
            Enumerable.Range(0, CompanionCapabilitySummary.MartialDisciplineCount)
                .Select(type => new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseMartialQualification,
                    new CandidateDisciplineIdentity(
                        CandidateDisciplineDomain.Martial,
                        checked((short)type)))));
        var lifeSkills = BuildCategory(
            profile,
            CompanionCapabilityCategory.LifeSkillDisciplines,
            Enumerable.Range(0, CompanionCapabilitySummary.LifeSkillDisciplineCount)
                .Select(type => new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseLifeSkillQualification,
                    new CandidateDisciplineIdentity(
                        CandidateDisciplineDomain.LifeSkill,
                        checked((short)type)))));
        var state = AggregateState(
            mainAttributes.State,
            martial.State,
            lifeSkills.State);
        decimal? breadth = state == CompanionCapabilitySummaryState.Complete
            ? Round((mainAttributes.Average!.Value
                + martial.Average!.Value
                + lifeSkills.Average!.Value) / 3m)
            : null;
        return new CompanionCapabilitySummary(
            state,
            mainAttributes,
            martial,
            lifeSkills,
            breadth);
    }

    private static CompanionCapabilityCategorySummary BuildCategory(
        CandidateProfile profile,
        CompanionCapabilityCategory category,
        IEnumerable<CandidateProfileFieldIdentity> fields)
    {
        var components = fields.Select(field =>
        {
            var fact = profile.FindFact(field);
            return new CompanionCapabilityComponent(
                field,
                fact?.State,
                fact is
                {
                    State: CandidateEvidenceState.Confirmed,
                    Value.Kind: CandidateFactValueKind.Int16
                }
                    ? fact.Value.Int16Value
                    : null);
        }).ToArray();
        var state = AggregateState(components.Select(ComponentState));
        decimal? average = state == CompanionCapabilitySummaryState.Complete
            ? Round(components.Average(component =>
                (decimal)component.Value!.Value))
            : null;
        return new CompanionCapabilityCategorySummary(
            category,
            state,
            components,
            average);
    }

    private static CompanionCapabilitySummaryState ComponentState(
        CompanionCapabilityComponent component) => component.EvidenceState switch
        {
            CandidateEvidenceState.Confirmed when component.Value.HasValue =>
                CompanionCapabilitySummaryState.Complete,
            CandidateEvidenceState.Conflicting =>
                CompanionCapabilitySummaryState.Conflicting,
            CandidateEvidenceState.Stale =>
                CompanionCapabilitySummaryState.Stale,
            CandidateEvidenceState.Unsupported =>
                CompanionCapabilitySummaryState.Unsupported,
            CandidateEvidenceState.Incomplete or null =>
                CompanionCapabilitySummaryState.Incomplete,
            _ => CompanionCapabilitySummaryState.Incomplete
        };

    private static CompanionCapabilitySummaryState AggregateState(
        params CompanionCapabilitySummaryState[] states) =>
        AggregateState(states.AsEnumerable());

    private static CompanionCapabilitySummaryState AggregateState(
        IEnumerable<CompanionCapabilitySummaryState> states)
    {
        var values = states.ToArray();
        if (values.Contains(CompanionCapabilitySummaryState.Conflicting))
        {
            return CompanionCapabilitySummaryState.Conflicting;
        }

        if (values.Contains(CompanionCapabilitySummaryState.Stale))
        {
            return CompanionCapabilitySummaryState.Stale;
        }

        if (values.Contains(CompanionCapabilitySummaryState.Unsupported))
        {
            return CompanionCapabilitySummaryState.Unsupported;
        }

        return values.Contains(CompanionCapabilitySummaryState.Incomplete)
            ? CompanionCapabilitySummaryState.Incomplete
            : CompanionCapabilitySummaryState.Complete;
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
