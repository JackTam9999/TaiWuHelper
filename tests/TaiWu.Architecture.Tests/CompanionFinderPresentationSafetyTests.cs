using System.Reflection;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.Architecture.Tests;

public sealed class CompanionFinderPresentationSafetyTests
{
    [Fact]
    public void Companion_page_has_dedicated_route_navigation_and_one_read_action()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(
            root,
            "TaiWuAPI",
            "Components",
            "Pages",
            "CompanionFinder.razor"));
        var layout = File.ReadAllText(Path.Combine(
            root,
            "TaiWuAPI",
            "Components",
            "Layout",
            "MainLayout.razor"));
        var program = File.ReadAllText(Path.Combine(
            root,
            "TaiWuAPI",
            "Program.cs"));

        Assert.Contains("@page \"/companions\"", page);
        Assert.Contains("<NavLink href=\"/companions\">", layout);
        Assert.True(
            layout.IndexOf("href=\"/skills\"", StringComparison.Ordinal)
            < layout.IndexOf("href=\"/companions\"", StringComparison.Ordinal));
        Assert.Equal(1, CountOccurrences(page, "FindCandidates.ExecuteAsync("));
        Assert.Contains(
            "AddScoped<IFindCompanionCandidates, FindCompanionCandidates>()",
            program);
        Assert.DoesNotContain("ICompanionCandidateSnapshotReader", page);
        Assert.DoesNotContain("EvaluateAndRank", page);
        Assert.DoesNotContain("CompanionRoleEvaluator", page);
        Assert.DoesNotContain("HttpClient", page);
    }

    [Fact]
    public void Companion_result_uses_native_semantics_and_never_renders_raw_ids()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(
            root,
            "TaiWuAPI",
            "Components",
            "Pages",
            "CompanionFinder.razor"));
        var result = File.ReadAllText(Path.Combine(
            root,
            "TaiWuAPI",
            "Components",
            "Companions",
            "CompanionCandidateResults.razor"));

        Assert.Contains("type=\"radio\"", page);
        Assert.Contains("<select", page);
        Assert.Contains("disabled=", page);
        Assert.Contains("type=\"radio\"", result);
        Assert.Contains("type=\"checkbox\"", result);
        Assert.Contains("scope=\"col\"", result);
        Assert.Contains("scope=\"row\"", result);
        Assert.Contains("aria-live=\"polite\"", result);
        Assert.Contains("aria-label=\"@candidate.RankLabel\"", result);
        Assert.Contains("inert", result);
        Assert.DoesNotContain("> @candidate.CharacterId", result);
        Assert.DoesNotContain(">@candidate.CharacterId", result);
        Assert.DoesNotContain("value=\"@option.Type\"", page);
        Assert.DoesNotContain("data-character-id", result);
        Assert.DoesNotContain("<progress", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<meter", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("winner", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("crown", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Companion_filters_and_comparison_cannot_re_evaluate_or_read_sources()
    {
        var root = FindRepositoryRoot();
        var component = File.ReadAllText(Path.Combine(
            root,
            "TaiWuAPI",
            "Components",
            "Companions",
            "CompanionCandidateResults.razor"));
        var interaction = File.ReadAllText(Path.Combine(
            root,
            "TaiWuAPI",
            "Presentation",
            "CompanionFinderInteractionState.cs"));
        var mapper = File.ReadAllText(Path.Combine(
            root,
            "TaiWuAPI",
            "Presentation",
            "CompanionFinderViewModelMapper.cs"));
        var combined = string.Join(Environment.NewLine, component, interaction, mapper);

        Assert.Contains("State.SetFilter(filter)", component);
        Assert.Contains("State.SetNameQuery", component);
        Assert.Contains("State.ToggleComparison", component);
        Assert.Equal(
            1,
            CountOccurrences(
                mapper,
                "CompanionRoleComparisonBuilder.Compare("));
        Assert.DoesNotContain("FindCompanionCandidates", combined);
        Assert.DoesNotContain("ICompanionCandidateSnapshotReader", combined);
        Assert.DoesNotContain("EvaluateAndRank", combined);
        Assert.DoesNotContain("CompanionRoleEvaluator", combined);
        Assert.DoesNotContain("File.", combined);
        Assert.DoesNotContain("Process.", combined);
        Assert.DoesNotContain("HttpClient", combined);
    }

    [Fact]
    public void Companion_presentation_is_responsive_typed_and_game_non_mutating()
    {
        var root = FindRepositoryRoot();
        var styles = File.ReadAllText(Path.Combine(
            root,
            "TaiWuAPI",
            "wwwroot",
            "app.css"));
        Assert.Contains("container-type: inline-size", styles);
        Assert.Contains("@container (max-width: 959px)", styles);
        Assert.Contains("@media (max-width: 620px)", styles);
        Assert.Contains(".companion-mobile-label", styles);
        Assert.Contains("overflow-wrap: anywhere", styles);

        var presentationTypes = typeof(CompanionFinderViewModel).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "TaiWuAPI.Presentation"
                && type.Name.Contains("Companion", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(presentationTypes);
        Assert.DoesNotContain(
            presentationTypes.SelectMany(type => type.GetMethods(
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.DeclaredOnly)),
            method => new[]
            {
                "Recruit",
                "Train",
                "Move",
                "Equip",
                "Assign",
                "Persist",
                "Upload",
                "Automate",
                "WriteSave"
            }.Any(token => method.Name.Contains(
                token,
                StringComparison.OrdinalIgnoreCase)));
        Assert.DoesNotContain(
            presentationTypes.SelectMany(PublicSignatureTypes),
            type => type.Namespace?.StartsWith(
                    "GameData",
                    StringComparison.Ordinal) == true
                || type.Namespace?.StartsWith(
                    "TaiWu.Infrastructure",
                    StringComparison.Ordinal) == true
                || type == typeof(FileInfo)
                || type == typeof(DirectoryInfo)
                || type == typeof(Stream)
                || type == typeof(System.Diagnostics.Process));
    }

    private static IEnumerable<Type> PublicSignatureTypes(Type type)
    {
        yield return type;
        foreach (var property in type.GetProperties(
                     BindingFlags.Instance | BindingFlags.Public))
        {
            yield return Unwrap(property.PropertyType);
        }

        foreach (var method in type.GetMethods(
                     BindingFlags.Instance
                     | BindingFlags.Static
                     | BindingFlags.Public
                     | BindingFlags.DeclaredOnly))
        {
            yield return Unwrap(method.ReturnType);
            foreach (var parameter in method.GetParameters())
            {
                yield return Unwrap(parameter.ParameterType);
            }
        }
    }

    private static Type Unwrap(Type type)
    {
        if (type.IsArray)
        {
            return Unwrap(type.GetElementType()!);
        }

        if (type.IsGenericType)
        {
            return Unwrap(type.GetGenericArguments().Last());
        }

        return Nullable.GetUnderlyingType(type) ?? type;
    }

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
