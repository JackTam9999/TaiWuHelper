using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests;

public sealed class CombatSkillPageSourceMappingTests
{
    [Theory]
    [InlineData("outline-0", 0x0001)]
    [InlineData("outline-4", 0x0010)]
    [InlineData("direct-0", 0x0020)]
    [InlineData("direct-4", 0x0200)]
    [InlineData("reverse-0", 0x0400)]
    [InlineData("reverse-4", 0x4000)]
    public void Detail_ids_map_to_the_verified_fifteen_bit_layout(
        string detailId,
        int expectedMask)
    {
        Assert.Equal(
            expectedMask,
            TaiwuCombatSkillPageSourceReader.GetDetailMask(detailId));
    }

    [Fact]
    public void Book_layout_maps_outline_and_five_normal_page_types()
    {
        var details = TaiwuCombatSkillPageSourceReader
            .DecodeBookDetailIds([0, 0, 1, 0, 1, 0]);

        Assert.NotNull(details);
        Assert.Equal(
            [
                "direct-0",
                "direct-2",
                "direct-4",
                "outline-0",
                "reverse-1",
                "reverse-3"
            ],
            details.Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(UnsupportedBookLayouts))]
    public void Unsupported_book_layout_is_not_inferred(sbyte[] pageTypes)
    {
        Assert.Null(
            TaiwuCombatSkillPageSourceReader.DecodeBookDetailIds(pageTypes));
    }

    public static TheoryData<sbyte[]> UnsupportedBookLayouts => new()
    {
        Array.Empty<sbyte>(),
        new sbyte[] { 5, 0, 0, 0, 0, 0 },
        new sbyte[] { 0, 0, 0, 2, 0, 0 }
    };
}
