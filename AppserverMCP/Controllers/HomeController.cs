using Microsoft.AspNetCore.Mvc;
using AppserverMCP.Services;

namespace AppserverMCP.Controllers;

[ApiController]
[Route("")]
public class HomeController(AppserverService appserverService, ILogger<HomeController> logger) : ControllerBase
{
    private readonly AppserverService _appserverService = appserverService;
    private readonly ILogger<HomeController> _logger = logger;

    /// <summary>
    /// Root endpoint - provides welcome message and available endpoints
    /// </summary>
    /// <returns>Welcome message and API information</returns>
    [HttpGet]
    public IActionResult GetRoot()
    {
        try
        {
            var welcomeInfo = new
            {
                message = "Welcome to AppserverMCP API",
                version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                timestamp = DateTime.UtcNow,
            };

            _logger.LogInformation("Root endpoint accessed");
            return Ok(welcomeInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Root endpoint failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
} 