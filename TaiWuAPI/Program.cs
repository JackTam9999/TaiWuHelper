using Microsoft.AspNetCore.DataProtection;
using System.Text.Json.Serialization;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.RegionStories;
using TaiWu.Application.SaveGames;
using TaiWu.Application.TargetObservations;
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
builder.Services.AddScoped<ReadRegionStoryProgress>();
builder.Services.AddScoped<
    IRecommendCombatLoadout,
    RecommendCombatLoadout>();
builder.Services.AddScoped<
    ITargetObservationRecommendationWorkflow,
    TargetObservationRecommendationWorkflow>();
builder.Services.AddScoped<
    IResolveTargetSkillSelection,
    ResolveTargetSkillSelection>();
builder.Services.AddScoped<IFindTargets, FindTargets>();
builder.Services.AddScoped<IFindCompanionCandidates, FindCompanionCandidates>();
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
