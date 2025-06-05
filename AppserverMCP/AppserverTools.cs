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

    [McpServerTool, Description("Get user settings of the user from app server and takes user id as input")]
    public async Task<string> GetUserSettings(int userid)
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        try
        {
          var userSettings = await _appserverService.GetUserSettings(userid);
            if (userSettings == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve Appserver information. The server may be unavailable." });
            }

            return JsonSerializer.Serialize(userSettings, AppserverContext.Default.UserSettingsResponse);

        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving Appserver information: {ex.Message}" });
        }
    }

    [McpServerTool, Description("updates user settings such as default Currency and takes user id and default Currency as input to be updated")]
    public async Task<string> UpdateUserCurrency(int userid, string defaultCurrency)
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        try
        {
            var userSettings = await _appserverService.PutUserCurrencyAsync(userid, defaultCurrency);
            if (!userSettings)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve Appserver information. The server may be unavailable." });
            }

            return $"{defaultCurrency} updated for {userid}";

        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving Appserver information: {ex.Message}" });
        }
    }
} 