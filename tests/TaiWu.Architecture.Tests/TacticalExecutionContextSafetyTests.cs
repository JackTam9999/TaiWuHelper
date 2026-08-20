using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.TacticalCombat;
using TaiWu.Infrastructure;
using Xunit;

namespace TaiWu.Architecture.Tests;

public sealed class TacticalExecutionContextSafetyTests
{
    private static readonly string[] ForbiddenCapabilityTokens =
    [
        "File.Write",
        "File.OpenWrite",
        "File.Delete",
        "Directory.CreateDirectory",
        "Directory.Enumerate",
        "Directory.GetFiles",
        "SqliteConnection",
        "SQLiteConnection",
        "HttpClient",
        "Process.Start",
        "Process.GetProcess",
        "DllImport",
        "SendInput",
        "Harmony",
        "Registry."
    ];

    [Fact]
    public void Tactical_projection_has_no_mutation_control_or_unbounded_source_api()
    {
        var root = FindRepositoryRoot();
        var files = Directory.EnumerateFiles(
                Path.Combine(root, "src", "TaiWu.Domain", "TacticalCombat"),
                "TacticalExecution*.cs")
            .Concat(Directory.EnumerateFiles(
                Path.Combine(root, "src", "TaiWu.Domain", "TacticalCombat"),
                "TacticalCandidate*.cs"))
            .Concat(Directory.EnumerateFiles(
                Path.Combine(root, "src", "TaiWu.Domain", "TacticalCombat"),
                "TacticalLoadout*.cs"))
            .Concat(Directory.EnumerateFiles(
                Path.Combine(root, "src", "TaiWu.Domain", "TacticalCombat"),
                "TacticalScor*.cs"))
            .Concat(Directory.EnumerateFiles(
                Path.Combine(
                    root,
                    "src",
                    "TaiWu.Application",
                    "TacticalCombat"),
                "*.cs"))
            .ToArray();

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain(
                ForbiddenCapabilityTokens,
                token => source.Contains(token, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Result_contract_does_not_expose_identity_path_or_raw_snapshot()
    {
        var properties = typeof(TacticalExecutionContext).GetProperties(
            BindingFlags.Instance | BindingFlags.Public);
        var forbiddenNames = new[]
        {
            "Path",
            "Payload",
            "Process",
            "CharacterId",
            "DisplayName",
            "Description"
        };

        Assert.DoesNotContain(
            properties,
            property => forbiddenNames.Any(token => property.Name.Contains(
                token,
                StringComparison.OrdinalIgnoreCase)));
        Assert.DoesNotContain(
            properties,
            property => property.PropertyType.Name.Contains(
                "CombatSnapshot",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Use_case_depends_on_exactly_one_read_only_snapshot_port()
    {
        var parameters = Assert.Single(
                typeof(ReadTacticalExecutionContext).GetConstructors())
            .GetParameters();

        var parameter = Assert.Single(parameters);
        Assert.Equal(typeof(ICombatSnapshotReader), parameter.ParameterType);

        var services = new ServiceCollection();
        services.AddTaiwuInfrastructure();
        Assert.Contains(
            services,
            service => service.ServiceType
                == typeof(IReadTacticalExecutionContext)
                && service.ImplementationType
                    == typeof(ReadTacticalExecutionContext)
                && service.Lifetime == ServiceLifetime.Singleton);

        var discoveryParameter = Assert.Single(
                typeof(DiscoverTacticalCandidates).GetConstructors())
            .GetParameters();
        Assert.Equal(
            typeof(ICombatSnapshotReader),
            Assert.Single(discoveryParameter).ParameterType);
        Assert.Contains(
            services,
            service => service.ServiceType
                == typeof(IDiscoverTacticalCandidates)
                && service.ImplementationType
                    == typeof(DiscoverTacticalCandidates)
                && service.Lifetime == ServiceLifetime.Singleton);

        var searchParameters = Assert.Single(
                typeof(SearchTacticalLoadouts).GetConstructors())
            .GetParameters();
        Assert.Contains(
            searchParameters,
            parameter => parameter.ParameterType
                == typeof(ICombatSnapshotReader));
        Assert.Contains(
            searchParameters,
            parameter => parameter.ParameterType == typeof(TimeProvider));
        Assert.Equal(2, searchParameters.Length);
        Assert.Contains(
            services,
            service => service.ServiceType
                == typeof(ISearchTacticalLoadouts)
                && service.ImplementationType
                    == typeof(SearchTacticalLoadouts)
                && service.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void Candidate_result_does_not_expose_atlas_display_or_raw_text()
    {
        var properties = typeof(TacticalCandidateDiscoveryResult).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "TaiWu.Domain.TacticalCombat"
                && type.Name.StartsWith(
                    "TacticalCandidateDiscovery",
                    StringComparison.Ordinal))
            .SelectMany(type => type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public))
            .ToArray();

        Assert.DoesNotContain(
            properties,
            property => property.Name.Contains(
                    "Display",
                    StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains(
                    "Description",
                    StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains(
                    "Faction",
                    StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains(
                    "WeaponLabel",
                    StringComparison.OrdinalIgnoreCase));
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
