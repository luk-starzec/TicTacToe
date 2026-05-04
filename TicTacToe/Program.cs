using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicTacToe.Logic;
using TicTacToe.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TicTacToe.Models.GameSettings>(builder.Configuration.GetSection("GameSettings"));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IGameStateService, GameStateService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<TicTacToe.App>()
    .AddInteractiveServerRenderMode();

app.MapHub<GameHub>(GameHub.HubUrl);

app.Run();
