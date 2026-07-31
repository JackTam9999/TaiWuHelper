using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed class ManualChecklistStateTests
{
    [Fact]
    public void Toggle_changes_only_helper_local_completion_state()
    {
        var state = new ManualChecklistState();
        const string reference = "checklist:add:604";

        state.Toggle(reference);

        Assert.True(state.IsCompleted(reference));

        state.Toggle(reference);

        Assert.False(state.IsCompleted(reference));
        Assert.Empty(
            typeof(ManualChecklistState)
                .GetConstructors()
                .Single()
                .GetParameters());
    }

    [Fact]
    public void Synchronize_removes_completion_for_missing_items()
    {
        var state = new ManualChecklistState();
        var first = Item("checklist:first");
        var second = Item("checklist:second");
        state.Synchronize([first, second]);
        state.Toggle(first.Reference);
        state.Toggle(second.Reference);

        state.Synchronize([second]);

        Assert.False(state.IsCompleted(first.Reference));
        Assert.True(state.IsCompleted(second.Reference));
    }

    private static ManualChecklistItemViewModel Item(string reference) =>
        new(
            reference,
            ManualChecklistItemKind.AddSkill,
            "Test skill",
            "Add a skill manually.",
            "reason:test",
            ["evidence:test"]);
}
