using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.GameData;
using TaiWu.Application.SaveGames;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.SaveGames;
using TaiWu.Infrastructure;
using TaiWu.Infrastructure.Catalogue;
using TaiWuAPI.Controllers;
using TaiWuAPI.Presentation;
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
        ("database persistence", SqlitePersistencePattern()),
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
    public void Presentation_models_expose_no_GameData_or_game_commands()
    {
        var presentationTypes = typeof(CombatRecommendationViewModel)
            .Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "TaiWuAPI.Presentation")
            .ToArray();
        var gameDataTypes = presentationTypes
            .SelectMany(GetPublicSignatureTypes)
            .Where(IsGameDataType)
            .Select(type => type.FullName ?? type.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var gameCommands = presentationTypes
            .SelectMany(type => type.GetMethods(
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.DeclaredOnly))
            .Where(method => GameMutationActionPattern().IsMatch(method.Name))
            .Select(method => $"{method.DeclaringType?.Name}.{method.Name}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(presentationTypes);
        Assert.Empty(gameDataTypes);
        Assert.Empty(gameCommands);
    }

    [Fact]
    public void Blazor_shell_is_local_cancellable_and_information_only()
    {
        var repositoryRoot = FindRepositoryRoot();
        var program = File.ReadAllText(
            Path.Combine(repositoryRoot, "TaiWuAPI", "Program.cs"));
        var page = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "TaiWuAPI",
                "Components",
                "Pages",
                "CombatRecommendation.razor"));
        var layout = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "TaiWuAPI",
                "Components",
                "Layout",
                "MainLayout.razor"));

        Assert.Contains("ListenLocalhost(", program);
        Assert.Contains("AddRazorComponents()", program);
        Assert.Contains("AddInteractiveServerComponents()", program);
        Assert.Contains("MapRazorComponents<App>()", program);
        Assert.Contains("@page \"/\"", page);
        Assert.Contains("FindTargets.ExecuteAsync(", page);
        Assert.Contains("RecommendCombatLoadout.ExecuteAsync(", page);
        Assert.Contains("CancellationTokenSource", page);
        Assert.Contains("Analysis input only", page);
        Assert.Contains("Information only", layout);
        Assert.DoesNotContain(">Apply<", page);
        Assert.DoesNotContain("Equip automatically", page);
        Assert.False(
            File.Exists(Path.Combine(repositoryRoot, "package.json")));
        Assert.False(
            File.Exists(
                Path.Combine(repositoryRoot, "TaiWuAPI", "package.json")));
    }

    [Fact]
    public void Recommendation_layout_exposes_linked_read_only_details()
    {
        var repositoryRoot = FindRepositoryRoot();
        var componentRoot = Path.Combine(
            repositoryRoot,
            "TaiWuAPI",
            "Components");
        var page = File.ReadAllText(
            Path.Combine(
                componentRoot,
                "Pages",
                "CombatRecommendation.razor"));
        var threatPanel = File.ReadAllText(
            Path.Combine(
                componentRoot,
                "Recommendations",
                "ThreatPanel.razor"));
        var skillCard = File.ReadAllText(
            Path.Combine(
                componentRoot,
                "Recommendations",
                "SkillCard.razor"));
        var capacity = File.ReadAllText(
            Path.Combine(
                componentRoot,
                "Recommendations",
                "CapacityBar.razor"));

        Assert.Contains("OrderByDescending(threat => threat.Severity)", threatPanel);
        Assert.Contains("<ThreatPanel", page);
        Assert.Contains("<LoadoutCategory", page);
        Assert.Contains("<BattlePlan", page);
        Assert.Contains("SelectedThreatReference", page);
        Assert.Contains("Actual cost", skillCard);
        Assert.Contains("Effective cost", skillCard);
        Assert.Contains("Practice", skillCard);
        Assert.Contains("Activation", skillCard);
        Assert.Contains("Skill.Conditions", skillCard);
        Assert.Contains("Skill.Cost.EvidenceReferences", skillCard);
        Assert.Contains("Category.GenericSlots", capacity);
        Assert.Contains("This is not a win probability.", page);
        Assert.DoesNotContain(">Apply<", page);
    }

    [Fact]
    public void Project_wide_ui_rules_require_names_and_non_interference()
    {
        var repositoryRoot = FindRepositoryRoot();
        var componentRoot = Path.Combine(
            repositoryRoot,
            "TaiWuAPI",
            "Components");
        var uiRule = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "docs",
            "architecture",
            "UI-PRESENTATION-GUIDELINES.md"));
        var combatLayout = File.ReadAllText(Path.Combine(
                repositoryRoot,
                "docs",
                "roadmap",
                "epic-001",
                "UI-001-combat-recommendation-layout.md"));
        var componentSources = Directory
            .EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = Path.GetRelativePath(repositoryRoot, path),
                Source = File.ReadAllText(path)
            })
            .ToArray();
        var forbiddenVisibleFragments = new[]
        {
            "<code>",
            "character ID",
            "skill ID",
            " IDs",
            "· ID",
            "#@",
            "@warning.Code",
            "@caveat.Code",
            "LocationIdText("
        };
        var violations = componentSources
            .SelectMany(file => forbiddenVisibleFragments
                .Where(fragment => file.Source.Contains(
                    fragment,
                    StringComparison.OrdinalIgnoreCase))
                .Select(fragment => $"{file.Path}: {fragment}"))
            .Concat(componentSources.SelectMany(file =>
                PlayerVisibleIdentifierPattern()
                    .Matches(file.Source)
                    .Select(match => $"{file.Path}: {match.Value.Trim()}")))
            .ToArray();

        Assert.Contains("## Absolute game non-interference", uiRule);
        Assert.Contains("must never modify", uiRule);
        Assert.Contains("## Player-visible identity", uiRule);
        Assert.Contains("never by a numeric ID", uiRule);
        Assert.Contains("UI-PRESENTATION-GUIDELINES.md", combatLayout);
        Assert.Empty(violations);
    }

    [Fact]
    public void Manual_workflow_changes_helper_state_only()
    {
        var repositoryRoot = FindRepositoryRoot();
        var componentRoot = Path.Combine(
            repositoryRoot,
            "TaiWuAPI",
            "Components",
            "Recommendations");
        var checklist = File.ReadAllText(
            Path.Combine(componentRoot, "ManualChecklist.razor"));
        var checklistState = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "TaiWuAPI",
                "Presentation",
                "ManualChecklistState.cs"));
        var battlePlan = File.ReadAllText(
            Path.Combine(componentRoot, "BattlePlan.razor"));
        var helperScript = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "TaiWuAPI",
                "wwwroot",
                "helper.js"));
        var styles = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "TaiWuAPI",
                "wwwroot",
                "app.css"));

        Assert.Contains(
            "TaiWu Helper cannot perform these steps.",
            checklist);
        Assert.Contains("type=\"checkbox\"", checklist);
        Assert.Contains("ManualChecklistState", checklist);
        Assert.Contains("HashSet<string>", checklistState);
        Assert.DoesNotContain("TaiWu.Application", checklistState);
        Assert.DoesNotContain("TaiWu.Infrastructure", checklistState);
        Assert.DoesNotContain("IRecommendCombatLoadout", checklist);
        Assert.DoesNotContain("IFindTargets", checklist);
        Assert.Contains("Why this step", checklist);
        Assert.Contains("@T(item.Reason)", checklist);
        Assert.DoesNotContain("EvidenceCount", checklist);
        Assert.Contains("Reason and evidence", battlePlan);
        Assert.Contains("navigator.clipboard.writeText", helperScript);
        Assert.Contains("window.print()", helperScript);
        Assert.DoesNotContain("fetch(", helperScript);
        Assert.DoesNotContain("XMLHttpRequest", helperScript);
        Assert.DoesNotContain("WebSocket", helperScript);
        Assert.Contains("@media print", styles);
        Assert.Contains(".control-panel", styles);
        Assert.DoesNotContain(">Apply<", checklist);
        Assert.DoesNotContain(">Execute<", checklist);
    }

    [Fact]
    public void Warning_and_supporting_details_keep_uncertainty_explicit()
    {
        var repositoryRoot = FindRepositoryRoot();
        var componentRoot = Path.Combine(
            repositoryRoot,
            "TaiWuAPI",
            "Components",
            "Recommendations");
        var page = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "TaiWuAPI",
                "Components",
                "Pages",
                "CombatRecommendation.razor"));
        var warnings = File.ReadAllText(
            Path.Combine(componentRoot, "WarningBanner.razor"));
        var supporting = File.ReadAllText(
            Path.Combine(componentRoot, "SupportingDetails.razor"));

        Assert.True(
            page.IndexOf("<WarningBanner", StringComparison.Ordinal)
            < page.IndexOf("<section class=\"result-shell\"", StringComparison.Ordinal));
        Assert.DoesNotContain("<details", warnings);
        Assert.Contains("Effect on recommendation:", warnings);
        Assert.Contains("warning.IsCritical", warnings);
        Assert.Contains("Alternatives", supporting);
        Assert.Contains("Assumptions and unavailable data", supporting);
        Assert.Contains("Conditional requirements", supporting);
        Assert.Contains("Score contributions", supporting);
        Assert.Contains("Detailed evidence", supporting);
        Assert.Contains("Details.UnknownValuePolicy", supporting);
        Assert.DoesNotContain("win probability", supporting);
    }

    [Fact]
    public void Save_game_api_reads_only_the_configured_path_with_get()
    {
        var repositoryRoot = FindRepositoryRoot();
        var actions = typeof(SaveGamesController).GetMethods(
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.DeclaredOnly)
            .Select(method => new
            {
                Method = method,
                HttpAttributes = method
                    .GetCustomAttributes()
                    .OfType<HttpMethodAttribute>()
                    .ToArray()
            })
            .Where(item => item.HttpAttributes.Length > 0)
            .ToArray();

        var action = Assert.Single(actions);
        Assert.Equal("ReadConfigured", action.Method.Name);
        Assert.All(
            action.HttpAttributes.SelectMany(value => value.HttpMethods),
            method => Assert.Equal("GET", method));
        Assert.DoesNotContain(
            action.Method.GetParameters(),
            parameter => parameter.ParameterType == typeof(string));

        var program = File.ReadAllText(
            Path.Combine(repositoryRoot, "TaiWuAPI", "Program.cs"));
        Assert.Contains("ListenLocalhost(", program);
        Assert.Contains("options.HasValidSaveFilePath()", program);
        Assert.DoesNotContain("ValidateOnStart()", program);

        var developmentSettings = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "TaiWuAPI",
                "appsettings.Development.json"));
        Assert.DoesNotContain("Program Files", developmentSettings);
        Assert.DoesNotContain("SaveGames\\\\world_", developmentSettings);
    }

    [Fact]
    public void Recommendation_page_states_are_accessible_and_read_only()
    {
        var repositoryRoot = FindRepositoryRoot();
        var componentRoot = Path.Combine(
            repositoryRoot,
            "TaiWuAPI",
            "Components");
        var page = File.ReadAllText(
            Path.Combine(
                componentRoot,
                "Pages",
                "CombatRecommendation.razor"));
        var stateNotice = File.ReadAllText(
            Path.Combine(
                componentRoot,
                "Recommendations",
                "PageStateNotice.razor"));
        var skillCard = File.ReadAllText(
            Path.Combine(
                componentRoot,
                "Recommendations",
                "SkillCard.razor"));
        var layout = File.ReadAllText(
            Path.Combine(
                componentRoot,
                "Layout",
                "MainLayout.razor"));
        var styles = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "TaiWuAPI",
                "wwwroot",
                "app.css"));

        Assert.Contains("aria-busy=", page);
        Assert.Contains("role=\"group\"", page);
        Assert.Contains("aria-pressed=", page);
        Assert.Contains("Retry read", stateNotice);
        Assert.Contains("aria-live=", stateNotice);
        Assert.DoesNotContain("repair", stateNotice);
        Assert.DoesNotContain("modify", stateNotice);
        Assert.Contains("condition-status", skillCard);
        Assert.Contains("Skip to main content", layout);
        Assert.Contains(":focus-visible", styles);
        Assert.Contains("@media (max-width: 1279px)", styles);
        Assert.Contains(
            "grid-template-columns: minmax(280px, 0.72fr) "
            + "minmax(0, 1.8fr)",
            styles);
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
    public void Catalogue_storage_boundary_is_infrastructure_owned()
    {
        var providerType = typeof(CatalogueStoragePathProvider);

        Assert.Equal(typeof(DependencyInjection).Assembly, providerType.Assembly);
        Assert.False(providerType.IsPublic);
        Assert.Equal(
            "TaiWu.Infrastructure.Catalogue",
            providerType.Namespace);

        var repositoryRoot = FindRepositoryRoot();
        var contractRoots = new[]
        {
            Path.Combine(repositoryRoot, "src", "TaiWu.Domain"),
            Path.Combine(repositoryRoot, "src", "TaiWu.Application"),
            Path.Combine(repositoryRoot, "TaiWuAPI")
        };
        var contractSource = string.Join(
            Environment.NewLine,
            contractRoots.SelectMany(root =>
                Directory
                    .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                    .Where(path => !IsBuildOutput(path))
                    .Select(File.ReadAllText)));

        Assert.DoesNotMatch(CataloguePathContractPattern(), contractSource);
    }

    [Fact]
    public void Catalogue_application_ports_are_path_free_and_inner_layer_only()
    {
        var ports = new[]
        {
            typeof(ICombatSkillDefinitionSource),
            typeof(ICombatSkillCatalogueRepository),
            typeof(ICharacterCombatSkillProgressReader)
        };
        var signatureTypes = ports
            .SelectMany(port => port.GetMethods())
            .SelectMany(method =>
                method.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .Append(method.ReturnType))
            .SelectMany(FlattenType)
            .Where(type => type.Assembly != typeof(object).Assembly)
            .Distinct()
            .ToArray();

        Assert.True(
            typeof(IReadOnlyGameDataSource).IsAssignableFrom(
                typeof(ICombatSkillDefinitionSource)));
        Assert.DoesNotContain(
            ports.SelectMany(port => port.GetMethods())
                .SelectMany(method => method.GetParameters()),
            parameter => parameter.Name?.Contains(
                "path",
                StringComparison.OrdinalIgnoreCase) == true);
        Assert.All(
            signatureTypes,
            type => Assert.Contains(
                type.Assembly,
                new[]
                {
                    typeof(ICombatSkillDefinitionSource).Assembly,
                    typeof(CombatSkillSnapshot).Assembly
                }));
    }

    [Fact]
    public void Combat_skill_api_is_query_only_except_named_cache_maintenance()
    {
        var controllers = new[]
        {
            typeof(CombatSkillsController),
            typeof(CharacterSkillAtlasController)
        };
        var actions = controllers
            .SelectMany(controller => controller.GetMethods(
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.DeclaredOnly))
            .Where(method =>
                method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToArray();

        Assert.DoesNotContain(
            actions.SelectMany(action => action.GetParameters()),
            parameter => parameter.Name?.Contains(
                "path",
                StringComparison.OrdinalIgnoreCase) == true
                || parameter.GetCustomAttribute<FromBodyAttribute>() is not null);
        Assert.DoesNotContain(
            actions,
            action => action.GetCustomAttribute<HttpPutAttribute>() is not null
                || action.GetCustomAttribute<HttpPatchAttribute>() is not null
                || action.GetCustomAttribute<HttpDeleteAttribute>() is not null);
        var maintenance = Assert.Single(
            actions,
            action => action.GetCustomAttribute<HttpPostAttribute>() is not null);
        Assert.Equal(
            "catalogue-cache/rebuild",
            maintenance.GetCustomAttribute<HttpPostAttribute>()!.Template);

        var repositoryRoot = FindRepositoryRoot();
        var sources = controllers.Select(controller => File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "TaiWuAPI",
                "Controllers",
                $"{controller.Name}.cs")));
        var combined = string.Join(Environment.NewLine, sources);
        Assert.DoesNotContain("SaveGameOptions", combined);
        Assert.DoesNotContain("DefaultSaveFilePath", combined);
        Assert.DoesNotContain("File.", combined);
        Assert.DoesNotContain("Directory.", combined);
    }

    [Fact]
    public void Production_persistence_is_confined_to_named_catalogue_store()
    {
        var repositoryRoot = FindRepositoryRoot();
        var allowedStore = Path.Combine(
            "src",
            "TaiWu.Infrastructure",
            "Catalogue",
            "SqliteCombatSkillCatalogueStore.cs");
        var persistenceRules = SaveAdapterForbiddenApis
            .Where(rule => rule.Description != "archive save")
            .ToArray();
        var violations = new List<string>();

        foreach (var root in new[]
                 {
                     Path.Combine(repositoryRoot, "src"),
                     Path.Combine(repositoryRoot, "TaiWuAPI")
                 })
        {
            foreach (var file in Directory
                         .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                         .Where(path => !IsBuildOutput(path)))
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file);
                var source = File.ReadAllText(file);
                foreach (var (description, pattern) in persistenceRules)
                {
                    if (pattern.IsMatch(source)
                        && !string.Equals(
                            relativePath,
                            allowedStore,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add($"{relativePath}: {description}");
                    }
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Persistence APIs are allowed only in the named helper-owned "
            + "catalogue store:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Named_catalogue_store_is_an_internal_path_safe_adapter()
    {
        var storeType = typeof(SqliteCombatSkillCatalogueStore);

        Assert.Equal(typeof(DependencyInjection).Assembly, storeType.Assembly);
        Assert.Equal("TaiWu.Infrastructure.Catalogue", storeType.Namespace);
        Assert.False(storeType.IsPublic);
        Assert.True(
            typeof(ICombatSkillCatalogueRepository).IsAssignableFrom(storeType));

        var constructors = storeType.GetConstructors(
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic);
        Assert.NotEmpty(constructors);
        Assert.All(
            constructors,
            constructor =>
            {
                var parameters = constructor.GetParameters();
                Assert.Contains(
                    parameters,
                    parameter => parameter.ParameterType
                        == typeof(CatalogueStoragePathProvider));
                Assert.DoesNotContain(
                    parameters,
                    parameter => parameter.ParameterType == typeof(string)
                        || parameter.ParameterType == typeof(FileInfo)
                        || parameter.ParameterType == typeof(DirectoryInfo));
            });
    }

    [Fact]
    public void Catalogue_database_is_generated_and_uses_the_pinned_native_runtime()
    {
        var repositoryRoot = FindRepositoryRoot();
        var ignore = File.ReadAllText(
            Path.Combine(repositoryRoot, ".gitignore"));
        var infrastructureProject = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "TaiWu.Infrastructure",
                "TaiWu.Infrastructure.csproj"));

        Assert.Contains("combat-skill-catalogue*.db", ignore);
        Assert.Contains("combat-skill-catalogue*.db-*", ignore);
        Assert.Contains(
            "Microsoft.Data.Sqlite\" Version=\"10.0.10\"",
            infrastructureProject);
        Assert.Contains(
            "SQLitePCLRaw.lib.e_sqlite3\" Version=\"2.1.12\"",
            infrastructureProject);
        Assert.DoesNotContain(
            "$(TaiwuBackendDirectory)\\e_sqlite3.dll",
            infrastructureProject);
    }

    [Fact]
    public void Presentation_events_are_read_only_or_helper_local()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apiRoot = Path.Combine(repositoryRoot, "TaiWuAPI");
        var componentRoot = Path.Combine(apiRoot, "Components");
        var uiFiles = Directory
            .EnumerateFiles(apiRoot, "*", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .Where(path => Path.GetExtension(path) is ".cs" or ".razor" or ".js")
            .ToArray();
        var violations = new List<string>();
        foreach (var file in uiFiles)
        {
            var source = File.ReadAllText(file);
            foreach (var (description, pattern) in
                     SaveAdapterForbiddenApis.Concat(
                         GameControlForbiddenApis))
            {
                if (pattern.IsMatch(source))
                {
                    violations.Add(
                        $"{Path.GetRelativePath(repositoryRoot, file)}: "
                        + description);
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Presentation code contains a file-write or game-control API:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));

        var componentSource = string.Join(
            Environment.NewLine,
            Directory
                .EnumerateFiles(
                    componentRoot,
                    "*.razor",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));
        var eventHandlers = UiEventHandlerPattern()
            .Matches(componentSource)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expectedHandlers = new[]
        {
            "() => SelectTarget(target)",
            "() => SelectedReferenceChanged.InvokeAsync(null)",
            "() => SelectedReferenceChanged.InvokeAsync(threat.Reference)",
            "() => SetLanguage(TaiwuLanguage.Chinese)",
            "() => SetLanguage(TaiwuLanguage.English)",
            "() => ShowStyle(style.Style)",
            "() => Toggle(item.Reference)",
            "() => ToggleObservationSkill(skill.SkillId)",
            "ApplyFiltersAsync",
            "ChangeAsync",
            "ClearFiltersAsync",
            "CopyAsync",
            "GetRecommendationAsync",
            "NextPageAsync",
            "PreviousPageAsync",
            "PrintAsync",
            "RebuildAsync",
            "ReloadAsync",
            "RetryRead",
            "SearchTargetsAsync"
        }.Order(StringComparer.Ordinal);

        Assert.Equal(expectedHandlers, eventHandlers);

        var page = File.ReadAllText(
            Path.Combine(
                componentRoot,
                "Pages",
                "CombatRecommendation.razor"));
        Assert.Contains("FindTargets.ExecuteAsync(", page);
        Assert.Contains("RecommendCombatLoadout.ExecuteAsync(", page);
        Assert.Contains(
            "PageReadOperation.TargetSearch => SearchTargetsAsync()",
            page);
        Assert.Contains(
            "PageReadOperation.Recommendation => GetRecommendationAsync()",
            page);
        Assert.DoesNotContain("ISaveGameReader", page);
        Assert.DoesNotContain("using GameData", page);
        Assert.DoesNotContain("GameData.", page);

        var atlasPage = File.ReadAllText(
            Path.Combine(
                componentRoot,
                "Pages",
                "SkillCatalogue.razor"));
        Assert.Contains("ReadCombatSkillCatalogueStatus(", atlasPage);
        Assert.Contains("ReadCharacterCombatSkillAtlas(", atlasPage);
        Assert.Contains("EnsureCombatSkillCatalogue(", atlasPage);
        Assert.Contains("characterId: null", atlasPage);
        Assert.DoesNotContain("ISaveGameReader", atlasPage);
        Assert.DoesNotContain("DefaultSaveFilePath", atlasPage);
        Assert.DoesNotContain("using GameData", atlasPage);
        Assert.Contains("aria-live", atlasPage);
        Assert.Contains("Candidate limit reached", atlasPage);

        var atlasCard = File.ReadAllText(
            Path.Combine(
                componentRoot,
                "Skills",
                "SkillAtlasCard.razor"));
        Assert.Contains("<details>", atlasCard);
        Assert.Contains("<summary>", atlasCard);
        Assert.Contains("role=\"list\"", atlasCard);
        Assert.Contains("@T(\"Learned\")", atlasCard);
        Assert.DoesNotContain(
            "<svg",
            atlasCard,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "<img",
            atlasCard,
            StringComparison.OrdinalIgnoreCase);

        var style = File.ReadAllText(
            Path.Combine(apiRoot, "wwwroot", "app.css"));
        Assert.Contains("@media (max-width: 620px)", style);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", style);
        Assert.Contains(".skill-card-grid", style);

        var checklist = File.ReadAllText(
            Path.Combine(
                componentRoot,
                "Recommendations",
                "ManualChecklist.razor"));
        Assert.Contains("_state.Toggle(reference)", checklist);
        Assert.DoesNotContain("IRecommendCombatLoadout", checklist);
        Assert.DoesNotContain("IFindTargets", checklist);

        var helperScript = File.ReadAllText(
            Path.Combine(apiRoot, "wwwroot", "helper.js"));
        Assert.Contains("navigator.clipboard.writeText(text)", helperScript);
        Assert.Contains("window.print()", helperScript);
        Assert.DoesNotMatch(ClientPersistencePattern(), helperScript);
        Assert.DoesNotMatch(ClientNetworkPattern(), helperScript);

        Assert.DoesNotContain(">Apply<", componentSource);
        Assert.DoesNotContain(">Equip<", componentSource);
        Assert.DoesNotContain(">Execute<", componentSource);
        Assert.DoesNotContain(">Repair<", componentSource);
        Assert.DoesNotContain(">Patch<", componentSource);
        Assert.DoesNotContain(">Control game<", componentSource);
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
    public void Snapshot_adapter_does_not_use_collection_capacity_as_slots()
    {
        var repositoryRoot = FindRepositoryRoot();
        var source = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "TaiWu.Infrastructure",
                "SaveGames",
                "TaiwuCombatSnapshotReader.cs"));

        Assert.DoesNotContain("equipment.Neigong.Capacity", source);
        Assert.DoesNotContain("equipment.Attack.Capacity", source);
        Assert.DoesNotContain("equipment.Agility.Capacity", source);
        Assert.DoesNotContain("equipment.Defense.Capacity", source);
        Assert.DoesNotContain("equipment.Assistance.Capacity", source);
        Assert.Contains(
            "CombatSlotBudgetCalculator.CalculateConfiguredCapacity",
            source);
    }

    [Fact]
    public void Snapshot_adapter_reads_legendary_book_cost_assignments()
    {
        var repositoryRoot = FindRepositoryRoot();
        var source = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "TaiWu.Infrastructure",
                "SaveGames",
                "TaiwuCombatSnapshotReader.cs"));

        Assert.Contains(
            "TryGetElement_LegendaryBookSkillSlot",
            source);
        Assert.Contains("new LegendaryBookCostSlot(", source);
        Assert.Contains("new LegendaryBookCostAssignment(", source);
    }

    [Fact]
    public void Character_progress_adapter_uses_typed_archive_data_only()
    {
        var repositoryRoot = FindRepositoryRoot();
        var source = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "TaiWu.Infrastructure",
                "SaveGames",
                "TaiwuCharacterCombatSkillProgressReader.cs"));
        var mapping = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "TaiWu.Infrastructure",
                "SaveGames",
                "CombatSkillProgressMapping.cs"));
        var labels = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "TaiWu.Infrastructure",
                "Catalogue",
                "CombatSkillStudyDetailLabelSource.cs"));

        Assert.Contains("GetCharCombatSkills(characterId)", source);
        Assert.Contains("TryGetElement_CombatSkillProficiencies", source);
        Assert.Contains("GetReadingState()", source);
        Assert.Contains("GetActivationState()", source);
        Assert.Contains("typeof(DomainManager).Assembly.Location", source);
        Assert.DoesNotContain(
            "typeof(Config.CombatSkill).Assembly.Location",
            source);
        Assert.Contains("labelSource.ReadAsync", source);
        Assert.Contains("request.PreferredLanguage", source);
        Assert.Contains("CombatSkillStudyDetailDecoder.Decode", mapping);
        Assert.Contains("studyDetails.Details", mapping);
        Assert.Contains("normalReadDetails", mapping);
        Assert.DoesNotContain("CountReadNormalPages", mapping);
        Assert.Contains("before != after", labels);
        Assert.Contains("TaiwuLanguageCatalog.ReadAsync", labels);
        Assert.Contains("DomainManager.Taiwu.GetTaiwuCharId()", source);
        Assert.Contains("fingerprintProvider.CaptureAsync", labels);
        Assert.DoesNotContain("File.Write", labels);
        Assert.DoesNotContain("SaveGameReport", source);
        Assert.DoesNotContain("LegacyReport", source);
        Assert.DoesNotContain("SKILL|", source);
        Assert.DoesNotContain("File.Write", source);
    }

    [Fact]
    public void Publishing_is_blocked_and_game_binaries_are_not_publish_items()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apiProject = File.ReadAllText(
            Path.Combine(repositoryRoot, "TaiWuAPI", "TaiWuAPI.csproj"));
        var infrastructureProject = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "TaiWu.Infrastructure",
                "TaiWu.Infrastructure.csproj"));

        Assert.Contains(
            "PreventProprietaryRuntimePublication",
            apiProject);
        Assert.Contains(
            "Publishing TaiWu Helper is disabled",
            apiProject);
        Assert.Contains(
            "BeforeTargets=\"PrepareForPublish\"",
            apiProject);
        Assert.DoesNotContain(
            "CopyToPublishDirectory=\"PreserveNewest\"",
            infrastructureProject);
        Assert.Contains(
            "<CopyToPublishDirectory>Never</CopyToPublishDirectory>",
            infrastructureProject);
    }

    [Fact]
    public void Committed_evidence_metadata_is_sanitized()
    {
        var repositoryRoot = FindRepositoryRoot();
        var metadata = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "docs",
                "scenarios",
                "evidence",
                "M1-002-golden-save-metadata.json"));

        Assert.DoesNotContain("sourcePath", metadata);
        Assert.DoesNotContain("targetCharacterId", metadata);
        Assert.DoesNotContain("capturedAtUtc", metadata);
        Assert.DoesNotContain("lastWriteTimeUtc", metadata);
        Assert.DoesNotContain("sha256", metadata);
    }

    [Fact]
    public void Epic_2_golden_skill_atlas_metadata_is_sanitized()
    {
        var repositoryRoot = FindRepositoryRoot();
        var metadata = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "docs",
                "scenarios",
                "evidence",
                "E2-001-golden-skill-atlas-metadata.json"));

        using var document = JsonDocument.Parse(metadata);
        var repository = document.RootElement.GetProperty("repository");

        Assert.DoesNotContain(@"C:\", metadata, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AppData", metadata, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Program Files", metadata, StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.GetProperty("containsSaveContent").GetBoolean());
        Assert.False(repository.GetProperty("containsSavePath").GetBoolean());
        Assert.False(repository.GetProperty("containsGameBinaryContent").GetBoolean());
        Assert.False(repository.GetProperty("containsGameBinaryHash").GetBoolean());
        Assert.False(repository.GetProperty("containsCompleteLanguageResource").GetBoolean());
        Assert.False(repository.GetProperty("containsScreenshot").GetBoolean());
        Assert.False(repository.GetProperty("containsCharacterIdentifier").GetBoolean());
    }

    [Fact]
    public void Repository_has_no_tracked_docker_configuration()
    {
        var repositoryRoot = FindRepositoryRoot();
        Assert.False(
            File.Exists(Path.Combine(repositoryRoot, ".dockerignore")));

        var apiProject = File.ReadAllText(
            Path.Combine(repositoryRoot, "TaiWuAPI", "TaiWuAPI.csproj"));
        var launchSettings = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "TaiWuAPI",
                "Properties",
                "launchSettings.json"));

        Assert.DoesNotContain("Docker", apiProject);
        Assert.DoesNotContain("Docker", launchSettings);
    }

    [Fact]
    public void Archive_loader_clears_handlers_before_each_load()
    {
        var repositoryRoot = FindRepositoryRoot();
        var source = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "src",
                "TaiWu.Infrastructure",
                "SaveGames",
                "TaiwuArchiveLoader.cs"));

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
    public void Domain_rule_tests_do_not_require_the_installed_game()
    {
        var repositoryRoot = FindRepositoryRoot();
        var testRoot = Path.Combine(
            repositoryRoot,
            "tests",
            "TaiWu.Domain.UnitTests");
        var project = File.ReadAllText(
            Path.Combine(testRoot, "TaiWu.Domain.UnitTests.csproj"));
        var source = string.Join(
            Environment.NewLine,
            Directory
                .EnumerateFiles(testRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsBuildOutput(path))
                .Select(File.ReadAllText));

        Assert.DoesNotContain("TaiWu.Infrastructure", project);
        Assert.DoesNotContain("GameData", project);
        Assert.DoesNotContain("Program Files", source);
        Assert.DoesNotContain("Steam\\steamapps", source);
        Assert.DoesNotContain("using GameData", source);
    }

    [Fact]
    public void Presentation_tests_do_not_require_the_installed_game()
    {
        var repositoryRoot = FindRepositoryRoot();
        var testRoot = Path.Combine(
            repositoryRoot,
            "tests",
            "TaiWu.API.UnitTests");
        var source = string.Join(
            Environment.NewLine,
            Directory
                .EnumerateFiles(testRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsBuildOutput(path))
                .Select(File.ReadAllText));

        Assert.DoesNotContain("Program Files", source);
        Assert.DoesNotContain("Steam\\steamapps", source);
        Assert.DoesNotContain("TAIWU_INTEGRATION_SAVE_PATH", source);
        Assert.DoesNotContain("using GameData", source);
        Assert.DoesNotContain("File.Open", source);
        Assert.DoesNotContain("File.Read", source);
        Assert.DoesNotContain("File.Write", source);
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
        @"\bFile\s*\.\s*(?:WriteAllBytes|WriteAllBytesAsync|WriteAllLines|WriteAllLinesAsync|WriteAllText|WriteAllTextAsync|AppendAllLines|AppendAllLinesAsync|AppendAllText|AppendAllTextAsync|OpenWrite)\b|\bDirectory\s*\.\s*CreateDirectory\b",
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
        @"\b(?:SqliteConnection|SQLiteConnection|SqliteConnectionStringBuilder)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex SqlitePersistencePattern();

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

    [GeneratedRegex(
        @"\b(?:(?:Catalogue|Catalog)\w*(?:Path|Directory)|(?:Path|Directory)\w*(?:Catalogue|Catalog))\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex CataloguePathContractPattern();

    [GeneratedRegex(
        @"@on(?:click|change)\s*=\s*""([^""]+)""",
        RegexOptions.IgnoreCase)]
    private static partial Regex UiEventHandlerPattern();

    [GeneratedRegex(
        @">\s*@(?<expression>(?:(?:[A-Za-z_]\w*\.)*(?:(?:[A-Za-z_]\w*)?Id|(?:[A-Za-z_]\w*)?Reference)|(?:[A-Za-z_]\w*\.)+Code))\b|>\s*@(?:reference|evidence)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex PlayerVisibleIdentifierPattern();

    [GeneratedRegex(
        @"\b(?:localStorage|sessionStorage|indexedDB|showSaveFilePicker|createObjectURL)\b|\bdownload\s*=",
        RegexOptions.IgnoreCase)]
    private static partial Regex ClientPersistencePattern();

    [GeneratedRegex(
        @"\b(?:fetch|XMLHttpRequest|WebSocket|EventSource)\s*(?:\(|\.)",
        RegexOptions.IgnoreCase)]
    private static partial Regex ClientNetworkPattern();
}
