using AppserverMCP;
using AppserverMCP.Utils;
using AppserverMCP.Interfaces;
using AppserverMCP.Services;
using AppserverMCP.Middleware;
using AppserverMCP.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure the application to listen on port 3001
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(3001);
});

// Add controllers support
builder.Services.AddControllers();

// Add request logging
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    logging.RequestHeaders.Add("User-Agent");
    logging.RequestHeaders.Add("A4SAuthorization");
    logging.RequestHeaders.Add("ROPC");
    logging.MediaTypeOptions.AddText("application/json");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

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

// Add request logging middleware
app.UseHttpLogging();

// Add custom detailed request logging middleware (optional - use this OR the built-in logging above)
// app.UseDetailedRequestLogging();

// Map controllers
app.MapControllers();

// Map MCP endpoints
app.MapMcp();

app.Run();