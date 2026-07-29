using TaiWu.Application.SaveGames;
using TaiWu.Infrastructure;
using TaiWuAPI.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(
    options => options.ListenLocalhost(5056));
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllers();
builder.Services.AddScoped<ReadSaveGame>();
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

app.MapControllers();

app.Run();
