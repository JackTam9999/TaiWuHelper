using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.Architecture.Tests;

public sealed class VillageWorkforcePresentationSafetyTests
{
    [Fact]
    public void Page_has_dedicated_route_navigation_and_one_inspection_action()
    {
        var root = FindRepositoryRoot();
        var page = Read(root,
            "TaiWuAPI", "Components", "Pages", "VillageWorkforce.razor");
        var result = Read(root,
            "TaiWuAPI", "Components", "VillageWorkforce",
            "VillageWorkforceResults.razor");
        var layout = Read(root,
            "TaiWuAPI", "Components", "Layout", "MainLayout.razor");

        Assert.Contains("@page \"/village-workforce\"", page);
        Assert.Contains("<NavLink href=\"/village-workforce\">", layout);
        Assert.Equal(1, CountOccurrences(
            page,
            "BuildWorkforce.Execute("));
        Assert.Equal(1, CountOccurrences(
            page,
            "SnapshotReader.ReadAsync("));
        Assert.DoesNotContain("BuildWorkforce", result);
        Assert.DoesNotContain("SnapshotReader", result);
        Assert.Contains("State.SetFilter(filter)", result);
        Assert.Contains("State.SetNameQuery", result);
        Assert.Contains("State.ToggleComparison", result);
    }

    [Fact]
    public void Result_uses_native_semantics_and_never_renders_raw_ids()
    {
        var root = FindRepositoryRoot();
        var result = Read(root,
            "TaiWuAPI", "Components", "VillageWorkforce",
            "VillageWorkforceResults.razor");

        Assert.Contains("type=\"radio\"", result);
        Assert.Contains("type=\"checkbox\"", result);
        Assert.Contains("<details class=\"workforce-candidate-evidence\">", result);
        Assert.Contains("scope=\"col\"", result);
        Assert.Contains("scope=\"row\"", result);
        Assert.Contains("aria-live=\"polite\"", result);
        Assert.Contains("inert=", result);
        Assert.DoesNotContain(
            "<details class=\"workforce-candidate-evidence\" open",
            result);
        Assert.DoesNotContain(">@candidate.CharacterId", result);
        Assert.DoesNotContain("> @candidate.CharacterId", result);
        Assert.DoesNotContain("data-character-id", result);
        Assert.DoesNotContain("<progress", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<meter", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("winner", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("crown", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Presentation_is_one_dom_responsive_and_non_mutating()
    {
        var root = FindRepositoryRoot();
        var styles = Read(root, "TaiWuAPI", "wwwroot", "app.css");
        var page = Read(root,
            "TaiWuAPI", "Components", "Pages", "VillageWorkforce.razor");
        var result = Read(root,
            "TaiWuAPI", "Components", "VillageWorkforce",
            "VillageWorkforceResults.razor");
        var interaction = Read(root,
            "TaiWuAPI", "Presentation", "VillageWorkforceInteractionState.cs");
        var combined = string.Join(Environment.NewLine, page, result, interaction);

        Assert.Contains(".workforce-result-shell", styles);
        Assert.Contains("container-type: inline-size", styles);
        Assert.Contains("@container (max-width: 959px)", styles);
        Assert.Contains("@media (max-width: 620px)", styles);
        Assert.Contains("content: attr(data-label)", styles);
        Assert.Contains("overflow-wrap: anywhere", styles);
        Assert.Equal(1, CountOccurrences(result, "workforce-candidate-table"));
        foreach (var forbidden in new[]
                 {
                     "File.", "Directory.", "Process.", "HttpClient",
                     "WriteSave", "Upload", "Automation", "SendInput"
                 })
        {
            Assert.DoesNotContain(forbidden, combined, StringComparison.Ordinal);
        }

        var methods = typeof(VillageWorkforceInteractionState)
            .GetMethods()
            .Where(method => method.DeclaringType
                == typeof(VillageWorkforceInteractionState));
        Assert.DoesNotContain(methods, method => new[]
        {
            "Assign", "Build", "Collect", "Recruit", "Persist",
            "Upload", "Automate", "WriteSave"
        }.Any(token => method.Name.Contains(
            token,
            StringComparison.OrdinalIgnoreCase)));
    }

    private static string Read(string root, params string[] parts) =>
        File.ReadAllText(Path.Combine([root, .. parts]));

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
               && !File.Exists(Path.Combine(directory.FullName, "TaiWu.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException(
                "Could not locate the repository root.");
    }
}
