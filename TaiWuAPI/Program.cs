using TaiWu.Application.SaveGames;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.Targets;
using TaiWu.Infrastructure;
using TaiWuAPI.Components;
using TaiWuAPI.Configuration;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(
    options => options.ListenLocalhost(5056));
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<ReadSaveGame>();
builder.Services.AddScoped<
    IRecommendCombatLoadout,
    RecommendCombatLoadout>();
builder.Services.AddScoped<IFindTargets, FindTargets>();
builder.Services.AddTaiwuInfrastructure();
builder.Services
    .AddOptions<SaveGameOptions>()
    .Bind(builder.Configuration.GetSection(SaveGameOptions.SectionName))
    .Validate(
        options =>
            Path.IsPathFullyQualified(options.DefaultSaveFilePath)
            && string.Equals(
                Path.GetExtension(options.DefaultSaveFilePath),
                ".sav",
                StringComparison.OrdinalIgnoreCase),
        "SaveGames:DefaultSaveFilePath must be an absolute .sav path.")
    .ValidateOnStart();

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
