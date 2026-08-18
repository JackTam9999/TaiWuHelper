using TaiWu.Domain.SaveGames;
using Xunit;

namespace TaiWu.Domain.UnitTests.SaveGames;

public sealed class SaveGameReportTests
{
    [Fact]
    public void Constructor_defensively_copies_caller_owned_lines()
    {
        List<string> lines = ["TAIWU|1"];

        var report = new SaveGameReport(lines);
        lines[0] = "CHANGED";
        lines.Add("EXTRA");

        Assert.Equal(["TAIWU|1"], report.Lines);
        Assert.Equal("TAIWU|1", report.ToLegacyText());
    }

    [Fact]
    public void Constructor_rejects_null_lines()
    {
        Assert.Throws<ArgumentException>(() =>
            new SaveGameReport(["TAIWU|1", null!]));
    }
}
