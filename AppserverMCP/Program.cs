using AppserverMCP;
using AppserverMCP.Utils;
using AppserverMCP.Interfaces;
using AppserverMCP.Services;

var builder = WebApplication.CreateBuilder(args);

// Add controllers support
builder.Services.AddControllers();

// Add MCP server configuration
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<AppserverTools>();

// Add HTTP client and services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<AppserverService>();
builder.Services.AddSingleton<IPlatformService, PlatformService>();
builder.Services.AddSingleton<AngleService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Map controllers
app.MapControllers();

// Map MCP endpoints
app.MapMcp();

app.Run();