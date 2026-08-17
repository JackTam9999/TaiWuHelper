using TaiWu.Domain.CompanionCandidates;
using Xunit;

namespace TaiWu.Domain.UnitTests.CompanionCandidates;

public sealed class CompanionCapabilitySummaryTests
{
    [Fact]
    public void Complete_saved_base_categories_produce_equal_weight_breadth_index()
    {
        var summary = CompanionCapabilitySummaryBuilder.Build(Profile());

        Assert.Equal(CompanionCapabilitySummaryState.Complete, summary.State);
        Assert.Equal("1", summary.Version);
        Assert.Equal(
            CompanionCapabilitySummaryFormula.EqualCategoryMean,
            summary.Formula);
        Assert.Equal(35m, summary.MainAttributes.Average);
        Assert.Equal(7.5m, summary.MartialDisciplines.Average);
        Assert.Equal(28.5m, summary.LifeSkillDisciplines.Average);
        Assert.Equal(23.67m, summary.BreadthIndex);
        Assert.Equal(6, summary.MainAttributes.ConfirmedCount);
        Assert.Equal(14, summary.MartialDisciplines.ConfirmedCount);
        Assert.Equal(16, summary.LifeSkillDisciplines.ConfirmedCount);
        Assert.All(
            summary.MainAttributes.Components,
            component => Assert.Equal(
                CandidateProfileField.BaseMainAttribute,
                component.Field.Field));
    }

    [Fact]
    public void Missing_category_fact_blocks_that_average_and_the_breadth_index()
    {
        var missing = new CandidateProfileFieldIdentity(
            CandidateProfileField.BaseMainAttribute,
            CandidateMainAttribute.Intelligence);
        var summary = CompanionCapabilitySummaryBuilder.Build(Profile(missing));

        Assert.Equal(CompanionCapabilitySummaryState.Incomplete, summary.State);
        Assert.Equal(
            CompanionCapabilitySummaryState.Incomplete,
            summary.MainAttributes.State);
        Assert.Equal(5, summary.MainAttributes.ConfirmedCount);
        Assert.Equal(6, summary.MainAttributes.ExpectedCount);
        Assert.Null(summary.MainAttributes.Average);
        Assert.Null(summary.BreadthIndex);
        Assert.Equal(7.5m, summary.MartialDisciplines.Average);
        Assert.Null(summary.MainAttributes.Components.Single(component =>
            component.Field == missing).Value);
    }

    [Fact]
    public void Main_attribute_identity_is_typed_and_cannot_label_other_fields()
    {
        var field = new CandidateProfileFieldIdentity(
            CandidateProfileField.BaseMainAttribute,
            CandidateMainAttribute.Strength);

        Assert.Equal(CandidateMainAttribute.Strength, field.MainAttribute);
        Assert.Null(field.Discipline);
        Assert.Throws<ArgumentException>(() =>
            new CandidateProfileFieldIdentity(
                CandidateProfileField.LivingState,
                CandidateMainAttribute.Strength));
        Assert.Throws<ArgumentException>(() =>
            new CandidateProfileFieldIdentity(
                CandidateProfileField.BaseMainAttribute));
    }

    private static CandidateProfile Profile(
        CandidateProfileFieldIdentity? incomplete = null)
    {
        var facts = new List<CandidateProfileFact>();
        facts.AddRange(Enum.GetValues<CandidateMainAttribute>().Select(attribute =>
            Fact(
                new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseMainAttribute,
                    attribute),
                checked((short)(10 + (int)attribute * 10)),
                incomplete)));
        facts.AddRange(Enumerable.Range(
                0,
                CompanionCapabilitySummary.MartialDisciplineCount)
            .Select(type => Fact(
                DisciplineField(
                    CandidateProfileField.BaseMartialQualification,
                    CandidateDisciplineDomain.Martial,
                    type),
                checked((short)(type + 1)),
                incomplete)));
        facts.AddRange(Enumerable.Range(
                0,
                CompanionCapabilitySummary.LifeSkillDisciplineCount)
            .Select(type => Fact(
                DisciplineField(
                    CandidateProfileField.BaseLifeSkillQualification,
                    CandidateDisciplineDomain.LifeSkill,
                    type),
                checked((short)(type + 21)),
                incomplete)));
        return new CandidateProfile(
            new CandidateIdentity(42),
            CandidateUniverseState.Eligible,
            new CandidateProfileSourceVersions(
                new string('A', 64),
                "1.0.0+verified",
                "2",
                "1",
                "2"),
            facts,
            diagnostics: []);
    }

    private static CandidateProfileFact Fact(
        CandidateProfileFieldIdentity field,
        short value,
        CandidateProfileFieldIdentity? incomplete) => field == incomplete
        ? CandidateProfileFact.Incomplete(
            field,
            new CandidateUnavailableReason(
                "BASE_VALUE_UNAVAILABLE",
                "The saved base value is unavailable."),
            evidence: [])
        : CandidateProfileFact.Confirmed(
            field,
            CandidateFactValue.Int16(value),
            Provenance(),
            evidence: []);

    private static CandidateProfileFieldIdentity DisciplineField(
        CandidateProfileField field,
        CandidateDisciplineDomain domain,
        int type) => new(
        field,
        new CandidateDisciplineIdentity(domain, checked((short)type)));

    private static CandidateFactProvenance Provenance() => new(
        CandidateEvidenceSourceKind.ConfiguredSave,
        "TAIWU_CONFIGURED_SAVE",
        "2",
        new string('A', 64));
}
