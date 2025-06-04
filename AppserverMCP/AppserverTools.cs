using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace AppserverMCP;

[McpServerToolType]
public sealed class AppserverTools(AppserverService appserverService)
{
    private AppserverService? _appserverService = appserverService;

    [McpServerTool, Description("Get comprehensive information about the Appserver including version and all available models with their status")]
    public async Task<string> GetAppserverAbout()
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        try
        {
            var aboutInfo = await _appserverService.GetAboutAsync();
            if (aboutInfo == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve Appserver information. The server may be unavailable." });
            }

            return JsonSerializer.Serialize(aboutInfo, AppserverContext.Default.AboutOutputView);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving Appserver information: {ex.Message}" });
        }
    }
} 