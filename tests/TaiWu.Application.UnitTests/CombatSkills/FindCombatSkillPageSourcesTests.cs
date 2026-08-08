using NSubstitute;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using Xunit;

namespace TaiWu.Application.UnitTests.CombatSkills;

public sealed class FindCombatSkillPageSourcesTests
{
    [Fact]
    public async Task Execute_forwards_a_valid_read_only_source_request()
    {
        var reader = Substitute.For<ICombatSkillPageSourceReader>();
        var request = new CombatSkillPageSourceReadRequest(
            606,
            ["outline-0"],
            CatalogueLanguage.TraditionalChinese);
        var expected = new CombatSkillPageSourceReadResult(
            CombatSkillPageSourceReadStatus.Available,
            606,
            ["outline-0"],
            Metadata: null,
            Candidates: [],
            Reason: null);
        reader.ReadAsync(request, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await new FindCombatSkillPageSources(reader)
            .ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
        await reader.Received(1).ReadAsync(
            request,
            TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData("outline-5")]
    [InlineData("unknown-0")]
    public void Request_rejects_an_unsupported_detail_id(string detailId)
    {
        Assert.Throws<ArgumentException>(() =>
            new CombatSkillPageSourceReadRequest(606, [detailId]));
    }

    [Fact]
    public void Request_rejects_duplicate_detail_ids()
    {
        Assert.Throws<ArgumentException>(() =>
            new CombatSkillPageSourceReadRequest(
                606,
                ["outline-0", "outline-0"]));
    }
}
