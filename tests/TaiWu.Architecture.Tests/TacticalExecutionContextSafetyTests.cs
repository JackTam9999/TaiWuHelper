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
