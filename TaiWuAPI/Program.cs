using TaiWu.Application.SaveGames;
using TaiWu.Infrastructure;
using TaiWuAPI.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllers();
builder.Services.AddScoped<ReadSaveGame>();
builder.Services.AddTaiwuInfrastructure();
builder.Services.Configure<SaveGameOptions>(
    builder.Configuration.GetSection(SaveGameOptions.SectionName));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
