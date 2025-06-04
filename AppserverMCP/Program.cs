using AppserverMCP;
using AppserverMCP.Utils;
using AppserverMCP.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<AppserverTools>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<AppserverService>();
builder.Services.AddSingleton<IPlatformService, PlatformService>();

var app = builder.Build();

app.MapMcp();

app.Run();