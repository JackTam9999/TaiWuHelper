using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;
using System.Text.RegularExpressions;
using TaiWu.Application.GameData;
using TaiWu.Application.SaveGames;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.SaveGames;
using TaiWu.Infrastructure;
using TaiWuAPI.Controllers;
using Xunit;

namespace TaiWu.Architecture.Tests;

public sealed partial class ArchitectureBoundaryTests
{
    private static readonly string[] AllowedGameDataQueryPrefixes =
    [
        "Read",
        "Get",
        "Find",
        "Search",
        "Inspect",
        "Query"
    ];

    private static readonly (string Description, Regex Pattern)[] SaveAdapterForbiddenApis =
    [
        ("file write", FileWritePattern()),
        ("write-capable stream", WriteStreamPattern()),
        ("destructive file operation", DestructiveFilePattern()),
        ("archive save", ArchiveSavePattern())
    ];

    private static readonly (string Description, Regex Pattern)[] GameControlForbiddenApis =
    [
        ("process start or termination", ProcessControlPattern()),
        ("process memory access", ProcessMemoryPattern()),
        ("remote-thread injection", RemoteThreadPattern()),
        ("operating-system hook", HookPattern()),
        ("automated input", AutomatedInputPattern()),
        ("Harmony patching", HarmonyPatchPattern())
    ];

    [Fact]
    public void Inner_layers_do_not_reference_outer_or_GameData_assemblies()
    {
        AssertHasNoReferences(
            typeof(SaveGameReport).Assembly,
            "TaiWu.Application",
            "TaiWu.Infrastructure",
            "TaiWuAPI",
            "GameData");
        AssertHasNoReferences(
            typeof(ISaveGameReader).Assembly,
            "TaiWu.Infrastructure",
            "TaiWuAPI",
            "GameData");
    }

    [Fact]
    public void Game_data_source_ports_expose_query_operations_only()
    {
        var marker = typeof(IReadOnlyGameDataSource);
        var ports = marker.Assembly
            .GetTypes()
            .Where(type =>
                type.IsInterface
                && type != marker
                && marker.IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(ports);

        var violations = ports
            .SelectMany(port => port.GetMethods())
            .Where(method => !AllowedGameDataQueryPrefixes.Any(
                prefix => method.Name.StartsWith(prefix, StringComparison.Ordinal)))
            .Select(method =>
                $"{method.DeclaringType?.FullName}.{method.Name}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Game-data source ports must expose queries only:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Infrastructure_public_surface_does_not_expose_GameData_types()
    {
        var assembly = typeof(DependencyInjection).Assembly;
        var violations = assembly
            .GetExportedTypes()
            .SelectMany(GetPublicSignatureTypes)
            .Where(IsGameDataType)
            .Select(type => type.FullName ?? type.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Infrastructure exposes GameData types:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Current_screen_observation_is_an_immutable_helper_value()
    {
        var observationType = typeof(PlayerLoadoutObservation);

        Assert.Equal(typeof(CombatSnapshot).Assembly, observationType.Assembly);
        Assert.All(
            observationType.GetProperties(),
            property => Assert.False(property.CanWrite));

        var mergerMethods = typeof(CombatSnapshotObservationMerger)
            .GetMethods(
                BindingFlags.Public
                | BindingFlags.Static
                | BindingFlags.DeclaredOnly);
        var merger = Assert.Single(mergerMethods);
        Assert.Equal(
            nameof(CombatSnapshotObservationMerger.Merge),
            merger.Name);
        Assert.Equal(typeof(CombatSnapshot), merger.ReturnType);
    }

    [Fact]
    public void Api_does_not_expose_game_mutation_commands()
    {
        var controllerAssembly = typeof(SaveGamesController).Assembly;
        var violations = new List<string>();

        foreach (var controller in controllerAssembly
                     .GetExportedTypes()
                     .Where(type => typeof(ControllerBase).IsAssignableFrom(type)))
        {
            var route = controller.GetCustomAttribute<RouteAttribute>()?.Template
                ?? string.Empty;

            foreach (var action in controller.GetMethods(
                         BindingFlags.Instance
                         | BindingFlags.Public
                         | BindingFlags.DeclaredOnly))
            {
                var httpAttributes = action
                    .GetCustomAttributes()
                    .OfType<HttpMethodAttribute>()
                    .ToArray();
                if (httpAttributes.Length == 0)
                {
                    continue;
                }

                if (GameMutationActionPattern().IsMatch(action.Name))
                {
                    violations.Add(
                        $"{controller.Name}.{action.Name} is mutation-oriented.");
                }

                if (route.Contains("game", StringComparison.OrdinalIgnoreCase)
                    && httpAttributes
                        .SelectMany(attribute => attribute.HttpMethods)
                        .Any(method =>
                            method is "PUT" or "PATCH" or "DELETE"))
                {
                    violations.Add(
                        $"{controller.Name}.{action.Name} uses a command HTTP verb "
                        + $"on route '{route}'.");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "The API must remain information-only:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Production_source_has_no_save_write_or_game_control_apis()
    {
        var repositoryRoot = FindRepositoryRoot();
        var violations = new List<string>();

        ScanSource(
            Path.Combine(
                repositoryRoot,
                "src",
                "TaiWu.Infrastructure",
                "SaveGames"),
            SaveAdapterForbiddenApis,
            violations,
            repositoryRoot);

        ScanSource(
            Path.Combine(repositoryRoot, "src"),
            GameControlForbiddenApis,
            violations,
            repositoryRoot);
        ScanSource(
            Path.Combine(repositoryRoot, "TaiWuAPI"),
            GameControlForbiddenApis,
            violations,
            repositoryRoot);

        Assert.True(
            violations.Count == 0,
            "Forbidden save-write or game-control APIs were found:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Snapshot_adapter_avoids_standalone_unsafe_cost_calculations()
    {
        var repositoryRoot = FindRepositoryRoot();
        var source = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "TaiWu.Infrastructure",
                "SaveGames",
                "TaiwuCombatSnapshotReader.cs"));

        Assert.DoesNotContain("GetCombatSkillGridCost(", source);
        Assert.DoesNotContain("ModifyData(", source);
    }

    [Fact]
    public void Archive_session_clears_handlers_before_each_load()
    {
        var repositoryRoot = FindRepositoryRoot();
        var source = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "TaiWu.Infrastructure",
                "SaveGames",
                "TaiwuArchiveReadSession.cs"));

        var clearHandlers = source.IndexOf(
            "ClearMonitoredData()",
            StringComparison.Ordinal);
        var loadArchive = source.IndexOf(
            "archive.Load()",
            StringComparison.Ordinal);

        Assert.True(clearHandlers >= 0);
        Assert.True(loadArchive > clearHandlers);
    }

    [Fact]
    public void Repository_tree_contains_no_proprietary_save_or_game_binaries()
    {
        var repositoryRoot = FindRepositoryRoot();
        var violations = Directory
            .EnumerateFiles(
                repositoryRoot,
                "*",
                SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .Where(IsProprietaryGameArtifact)
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Proprietary save or game runtime files were found:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    private static void AssertHasNoReferences(
        Assembly assembly,
        params string[] forbiddenPrefixes)
    {
        var violations = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => forbiddenPrefixes.Any(
                prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"{assembly.GetName().Name} has forbidden references:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<Type> GetPublicSignatureTypes(Type exportedType)
    {
        yield return exportedType;

        foreach (var constructor in exportedType.GetConstructors())
        {
            foreach (var parameter in constructor.GetParameters())
            {
                foreach (var type in FlattenType(parameter.ParameterType))
                {
                    yield return type;
                }
            }
        }

        foreach (var method in exportedType.GetMethods(
                     BindingFlags.Instance
                     | BindingFlags.Static
                     | BindingFlags.Public
                     | BindingFlags.DeclaredOnly))
        {
            foreach (var type in FlattenType(method.ReturnType))
            {
                yield return type;
            }

            foreach (var parameter in method.GetParameters())
            {
                foreach (var type in FlattenType(parameter.ParameterType))
                {
                    yield return type;
                }
            }
        }

        foreach (var property in exportedType.GetProperties())
        {
            foreach (var type in FlattenType(property.PropertyType))
            {
                yield return type;
            }
        }

        foreach (var field in exportedType.GetFields())
        {
            foreach (var type in FlattenType(field.FieldType))
            {
                yield return type;
            }
        }

        foreach (var eventInfo in exportedType.GetEvents())
        {
            if (eventInfo.EventHandlerType is null)
            {
                continue;
            }

            foreach (var type in FlattenType(eventInfo.EventHandlerType))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<Type> FlattenType(Type type)
    {
        yield return type;

        if (type.HasElementType && type.GetElementType() is { } elementType)
        {
            foreach (var nestedType in FlattenType(elementType))
            {
                yield return nestedType;
            }
        }

        if (!type.IsGenericType)
        {
            yield break;
        }

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nestedType in FlattenType(argument))
            {
                yield return nestedType;
            }
        }
    }

    private static bool IsGameDataType(Type type)
    {
        var assemblyName = type.Assembly.GetName().Name;
        return assemblyName?.StartsWith("GameData", StringComparison.Ordinal)
            == true;
    }

    private static void ScanSource(
        string root,
        (string Description, Regex Pattern)[] rules,
        List<string> violations,
        string repositoryRoot)
    {
        foreach (var file in Directory.EnumerateFiles(
                     root,
                     "*.cs",
                     SearchOption.AllDirectories)
                 .Where(path => !IsBuildOutput(path)))
        {
            var source = File.ReadAllText(file);
            foreach (var (description, pattern) in rules)
            {
                if (pattern.IsMatch(source))
                {
                    violations.Add(
                        $"{Path.GetRelativePath(repositoryRoot, file)}: {description}");
                }
            }
        }
    }

    private static bool IsBuildOutput(string path)
    {
        var segments = path.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        return segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
            || segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsProprietaryGameArtifact(string path)
    {
        var fileName = Path.GetFileName(path);
        var extension = Path.GetExtension(path);

        if (extension.Equals(".sav", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (fileName.Equals(
                "steam_api64.dll",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var isRuntimeExtension =
            extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase);

        return fileName.StartsWith(
                   "GameData",
                   StringComparison.OrdinalIgnoreCase)
               && isRuntimeExtension;
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

    [GeneratedRegex(
        @"\bFile\s*\.\s*(?:WriteAllBytes|WriteAllBytesAsync|WriteAllLines|WriteAllLinesAsync|WriteAllText|WriteAllTextAsync|AppendAllLines|AppendAllLinesAsync|AppendAllText|AppendAllTextAsync|OpenWrite)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex FileWritePattern();

    [GeneratedRegex(
        @"(?:new\s+FileStream|File\s*\.\s*Open)\s*\([^;]*(?:FileAccess\s*\.\s*(?:Write|ReadWrite)|FileMode\s*\.\s*(?:Create|CreateNew|Append|Truncate))",
        RegexOptions.IgnoreCase)]
    private static partial Regex WriteStreamPattern();

    [GeneratedRegex(
        @"\bFile\s*\.\s*(?:Delete|Move|Replace|Copy)\b|\bDirectory\s*\.\s*(?:Delete|Move)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex DestructiveFilePattern();

    [GeneratedRegex(
        @"\b(?:archive|localArchive)\s*\.\s*(?:Save|Write|Modify|Repair|Convert)\s*\(",
        RegexOptions.IgnoreCase)]
    private static partial Regex ArchiveSavePattern();

    [GeneratedRegex(
        @"\bProcess\s*\.\s*(?:Start|Kill)\s*\(|\bProcess\s*\.\s*GetProcess",
        RegexOptions.IgnoreCase)]
    private static partial Regex ProcessControlPattern();

    [GeneratedRegex(
        @"\b(?:OpenProcess|ReadProcessMemory|WriteProcessMemory|VirtualProtectEx)\s*\(",
        RegexOptions.IgnoreCase)]
    private static partial Regex ProcessMemoryPattern();

    [GeneratedRegex(
        @"\b(?:VirtualAllocEx|CreateRemoteThread|NtCreateThreadEx|QueueUserAPC)\s*\(",
        RegexOptions.IgnoreCase)]
    private static partial Regex RemoteThreadPattern();

    [GeneratedRegex(
        @"\b(?:SetWindowsHookEx|CallNextHookEx|UnhookWindowsHookEx)\s*\(",
        RegexOptions.IgnoreCase)]
    private static partial Regex HookPattern();

    [GeneratedRegex(
        @"\b(?:SendInput|mouse_event|keybd_event)\s*\(",
        RegexOptions.IgnoreCase)]
    private static partial Regex AutomatedInputPattern();

    [GeneratedRegex(
        @"\bHarmony\s*\.\s*Patch\s*\(|\bPatchAll\s*\(",
        RegexOptions.IgnoreCase)]
    private static partial Regex HarmonyPatchPattern();

    [GeneratedRegex(
        @"^(?:(?:Apply|Equip).*(?:Loadout|Skill)|(?:Write|Save|Modify|Update|Delete|Repair|Replace|Patch).*(?:Save|Game|Character|Skill|Loadout)|Inject|Hook|Automate|Control|SendInput|Trainer|Cheat)",
        RegexOptions.IgnoreCase)]
    private static partial Regex GameMutationActionPattern();
}
