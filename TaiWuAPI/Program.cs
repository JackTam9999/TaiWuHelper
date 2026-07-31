using Microsoft.AspNetCore.DataProtection;
using System.Text.Json.Serialization;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.SaveGames;
using TaiWu.Application.Targets;
using TaiWu.Infrastructure;
using TaiWuAPI.Components;
using TaiWuAPI.Configuration;
using TaiWuAPI.Localization;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddDataProtection()
        .UseEphemeralDataProtectionProvider();
}

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
builder.Services.AddScoped<TaiwuLanguageState>();
builder.Services.AddTaiwuInfrastructure();
builder.Services
    .AddOptions<SaveGameOptions>()
    .Bind(builder.Configuration.GetSection(SaveGameOptions.SectionName))
    .Validate(
        options => options.HasValidSaveFilePath(),
        SaveGameOptions.ValidationMessage);

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
