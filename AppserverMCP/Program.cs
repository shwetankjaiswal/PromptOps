using AppserverMCP;
using AppserverMCP.Utils;
using AppserverMCP.Interfaces;
using AppserverMCP.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<AppserverTools>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<AppserverService>();
builder.Services.AddSingleton<IPlatformService, PlatformService>();
builder.Services.AddSingleton<AngleService>();

var app = builder.Build();

app.MapMcp();

app.Run();