using System.Reflection;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.TargetObservations;
using TaiWu.Infrastructure;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.Architecture.Tests;

public sealed class TargetObservationSafetyTests
{
    private static readonly string[] ForbiddenFeatureTokens =
    [
        "File.Write",
        "File.OpenWrite",
        "Directory.CreateDirectory",
        "SqliteConnection",
        "SQLiteConnection",
        "HttpClient",
        "Process.Start",
        "Process.GetProcess",
        "DllImport",
        "SendInput",
        "Harmony",
        "localStorage",
        "sessionStorage",
        "indexedDB",
        "TargetObservationHistory"
    ];

    [Fact]
    public void Target_observation_feature_has_no_persistence_or_game_control_api()
    {
        var root = FindRepositoryRoot();
        var files = FeatureFiles(root).ToArray();

        Assert.NotEmpty(files);
        foreach (var path in files)
        {
            var source = File.ReadAllText(path);
            Assert.DoesNotContain(
                ForbiddenFeatureTokens,
                token => source.Contains(token, StringComparison.Ordinal));
        }

        var infrastructureTypes = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(type => type.Name.Contains(
                "TargetObservation",
                StringComparison.OrdinalIgnoreCase))
            .Select(type => type.FullName)
            .ToArray();
        Assert.Empty(infrastructureTypes);
    }

    [Fact]
    public void Observation_state_is_session_instance_state_only()
    {
        var fields = typeof(TargetObservationEditorState).GetFields(
            BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic);

        Assert.DoesNotContain(
            fields,
            field => field.IsStatic && !field.IsInitOnly);
        Assert.DoesNotContain(
            fields,
            field => field.FieldType == typeof(string)
                && field.Name.Contains("Path", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Observation_workflow_cannot_rebuild_or_clear_catalogue_caches()
    {
        var workflowParameters = Assert.Single(
                typeof(TargetObservationRecommendationWorkflow)
                    .GetConstructors())
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.Equal(
            [typeof(TaiWu.Application.CombatSnapshots.ICombatSnapshotReader),
                typeof(IResolveTargetSkillSelection)],
            workflowParameters);
        Assert.DoesNotContain(
            workflowParameters,
            type => type == typeof(ICharacterCombatSkillProgressCacheMaintenance));

        var root = FindRepositoryRoot();
        var resolverSource = File.ReadAllText(Path.Combine(
            root,
            "src",
            "TaiWu.Application",
            "CombatSkills",
            "ResolveTargetSkillSelection.cs"));
        Assert.DoesNotContain("ReplaceAsync(", resolverSource);
        Assert.DoesNotContain("ClearAsync(", resolverSource);
    }

    private static IEnumerable<string> FeatureFiles(string root)
    {
        var applicationRoot = Path.Combine(
            root,
            "src",
            "TaiWu.Application",
            "TargetObservations");
        foreach (var path in Directory.EnumerateFiles(applicationRoot, "*.cs"))
        {
            yield return path;
        }

        yield return Path.Combine(
            root,
            "src",
            "TaiWu.Application",
            "CombatSkills",
            "ResolveTargetSkillSelection.cs");
        yield return Path.Combine(
            root,
            "TaiWuAPI",
            "Presentation",
            "TargetObservationEditorState.cs");
        yield return Path.Combine(
            root,
            "TaiWuAPI",
            "Components",
            "Recommendations",
            "TargetObservationForm.razor");
        yield return Path.Combine(
            root,
            "TaiWuAPI",
            "Components",
            "Recommendations",
            "TargetObservationImpactPanel.razor");
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TaiWu.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root containing TaiWu.slnx.");
    }
}
