using GameData.Domains.Character;
using System.Reflection;
using Xunit;

namespace TaiWu.Infrastructure.IntegrationTests;

public sealed class StoryTargetDiagnosticTests
{
    [Fact]
    public void Print_character_template_members()
    {
        var members = typeof(Character)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .Where(member => member.Name.Contains(
                "Template",
                StringComparison.OrdinalIgnoreCase))
            .Select(member => $"{member.MemberType}:{member}")
            .Order()
            .ToArray();

        throw new InvalidOperationException(string.Join(" | ", members));
    }
}
