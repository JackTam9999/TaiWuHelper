using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class SnapshotValueTests
{
    [Fact]
    public void Available_value_exposes_the_captured_value()
    {
        var value = SnapshotValue<int>.Available(52);

        Assert.True(value.IsAvailable);
        Assert.Equal(52, value.Value);
        Assert.Null(value.UnavailableReason);
    }

    [Fact]
    public void Unavailable_value_requires_and_exposes_a_reason()
    {
        var value = SnapshotValue<string>.Unavailable(
            "Localized name was not initialized.");

        Assert.False(value.IsAvailable);
        Assert.Equal(
            "Localized name was not initialized.",
            value.UnavailableReason);
        Assert.Throws<InvalidOperationException>(() => value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Unavailable_value_rejects_a_missing_reason(string reason)
    {
        Assert.Throws<ArgumentException>(
            () => SnapshotValue<int>.Unavailable(reason));
    }

    [Fact]
    public void Practice_directions_preserve_GameData_semantics_as_domain_values()
    {
        Assert.Equal(-1, (int)PracticeDirection.Reverse);
        Assert.Equal(0, (int)PracticeDirection.Neutral);
        Assert.Equal(1, (int)PracticeDirection.Direct);
    }
}
