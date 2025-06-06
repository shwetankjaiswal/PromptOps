using Microsoft.AspNetCore.Mvc;
using AppserverMCP.Services;

namespace AppserverMCP.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(AppserverService appserverService, ILogger<HealthController> logger) : ControllerBase
{
    private readonly AppserverService _appserverService = appserverService;
    private readonly ILogger<HealthController> _logger = logger;

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    /// <returns>Health status information</returns>
    [HttpGet]
    public IActionResult GetHealth()
    {
        try
        {
            var healthInfo = new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime,
                version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            };

            _logger.LogInformation("Health check requested - Status: Healthy");
            return Ok(healthInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Detailed health check that includes Appserver backend connectivity
    /// </summary>
    /// <returns>Detailed health status including backend connectivity</returns>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedHealth()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Check backend connectivity
            var appserverStatus = await CheckAppserverHealth();
            
            var responseTime = DateTime.UtcNow - startTime;

            var healthInfo = new
            {
                status = appserverStatus.IsHealthy ? "Healthy" : "Degraded",
                timestamp = DateTime.UtcNow,
                uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime,
                version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                response_time_ms = responseTime.TotalMilliseconds,
                backends = new
                {
                    appserver = new
                    {
                        status = appserverStatus.IsHealthy ? "Healthy" : "Unhealthy",
                        version = appserverStatus.Version,
                        models_count = appserverStatus.ModelsCount,
                        models_up = appserverStatus.ModelsUp,
                        error = appserverStatus.Error
                    }
                }
            };

            var httpStatusCode = appserverStatus.IsHealthy ? 200 : 503;
            _logger.LogInformation("Detailed health check completed - Status: {Status}, Backend: {BackendStatus}", 
                healthInfo.status, appserverStatus.IsHealthy ? "Healthy" : "Unhealthy");
            
            return StatusCode(httpStatusCode, healthInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed health check failed");
            return StatusCode(500, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Readiness probe endpoint
    /// </summary>
    /// <returns>Readiness status</returns>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            // Check if the service can handle requests
            var appserverStatus = await CheckAppserverHealth();
            
            if (appserverStatus.IsHealthy)
            {
                return Ok(new { status = "Ready", timestamp = DateTime.UtcNow });
            }
            else
            {
                return StatusCode(503, new 
                { 
                    status = "Not Ready", 
                    timestamp = DateTime.UtcNow,
                    reason = "Backend Appserver not accessible"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new
            {
                status = "Not Ready",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Liveness probe endpoint
    /// </summary>
    /// <returns>Liveness status</returns>
    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        try
        {
            // Simple check that the application is running
            return Ok(new { status = "Alive", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Liveness check failed");
            return StatusCode(500, new
            {
                status = "Dead",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    private async Task<AppserverHealthStatus> CheckAppserverHealth()
    {
        try
        {
            var aboutInfo = await _appserverService.GetAboutAsync();
            
            if (aboutInfo != null)
            {
                var modelsUp = aboutInfo.Models.Count(m => string.Equals(m.Status, "Up", StringComparison.OrdinalIgnoreCase));
                
                return new AppserverHealthStatus
                {
                    IsHealthy = true,
                    Version = aboutInfo.AppServerVersion,
                    ModelsCount = aboutInfo.Models.Count,
                    ModelsUp = modelsUp
                };
            }
            else
            {
                return new AppserverHealthStatus
                {
                    IsHealthy = false,
                    Error = "Unable to connect to Appserver"
                };
            }
        }
        catch (Exception ex)
        {
            return new AppserverHealthStatus
            {
                IsHealthy = false,
                Error = ex.Message
            };
        }
    }

    private class AppserverHealthStatus
    {
        public bool IsHealthy { get; set; }
        public string? Version { get; set; }
        public int ModelsCount { get; set; }
        public int ModelsUp { get; set; }
        public string? Error { get; set; }
    }
} 