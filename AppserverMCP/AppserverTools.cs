using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using AppserverMCP.Services;
using AppserverMCP.Models;

namespace AppserverMCP;

[McpServerToolType]
public sealed class AppserverTools(AppserverService appserverService, AngleService angleService)
{
    private AppserverService? _appserverService = appserverService;
    private AngleService? _angleService = angleService;

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
    [McpServerTool, Description("Get comprehensive information about the business processes information.")]
    public async Task<string> GetBusinessProcesses()
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        try
        {
            var businessProcesses = await _appserverService.GetBusinessProcessesAsync();
            if (businessProcesses == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve business processes information. The server may be unavailable." });
            }

            return JsonSerializer.Serialize(businessProcesses, AppserverContext.Default.BusinessProcessResponse);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving business processes information: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get details of a specific model by model ID")]
    public async Task<string> GetModelDetails(string modelId)
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        if (string.IsNullOrWhiteSpace(modelId))
            return JsonSerializer.Serialize(new { error = "Model ID is required" });

        try
        {
            var aboutInfo = await _appserverService.GetAboutAsync();
            if (aboutInfo == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve Appserver information. The server may be unavailable." });
            }

            var model = aboutInfo.Models.FirstOrDefault(m =>
                string.Equals(m.ModelId, modelId, StringComparison.OrdinalIgnoreCase));

            if (model == null)
            {
                return JsonSerializer.Serialize(new
                {
                    error = $"Model with ID '{modelId}' not found",
                    availableModels = aboutInfo.Models.Select(m => m.ModelId).ToList()
                });
            }

            return JsonSerializer.Serialize(new
            {
                model_id = model.ModelId,
                version = model.Version,
                status = model.Status,
                modeldata_timestamp = model.ModeldataTimestamp,
                modeldata_datetime = DateTimeOffset.FromUnixTimeSeconds(model.ModeldataTimestamp).ToString("yyyy-MM-dd HH:mm:ss UTC"),
                model_definition_version = model.ModelDefinitionVersion,
                is_real_time = model.IsRealTime
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving model details: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get all available model IDs and their status")]
    public async Task<string> GetAllModels()
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

            var models = aboutInfo.Models.Select(m => new
            {
                model_id = m.ModelId,
                status = m.Status,
                version = m.Version,
                is_real_time = m.IsRealTime
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                total_models = models.Count,
                app_server_version = aboutInfo.AppServerVersion,
                models = models
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving models: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get models filtered by status (Up, Down, etc.)")]
    public async Task<string> GetModelsByStatus(string status)
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        if (string.IsNullOrWhiteSpace(status))
            return JsonSerializer.Serialize(new { error = "Status parameter is required (e.g., 'Up', 'Down')" });

        try
        {
            var aboutInfo = await _appserverService.GetAboutAsync();
            if (aboutInfo == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve Appserver information. The server may be unavailable." });
            }

            var filteredModels = aboutInfo.Models
                .Where(m => string.Equals(m.Status, status, StringComparison.OrdinalIgnoreCase))
                .Select(m => new
                {
                    model_id = m.ModelId,
                    version = m.Version,
                    status = m.Status,
                    is_real_time = m.IsRealTime
                }).ToList();

            return JsonSerializer.Serialize(new
            {
                filter_status = status,
                matching_models = filteredModels.Count,
                total_models = aboutInfo.Models.Count,
                models = filteredModels
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error filtering models by status: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get server health status and basic information")]
    public async Task<string> GetServerHealth()
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        try
        {
            var aboutInfo = await _appserverService.GetAboutAsync();
            if (aboutInfo == null)
            {
                return JsonSerializer.Serialize(new
                {
                    health_status = "Unhealthy",
                    error = "Cannot connect to Appserver",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                });
            }

            var upModels = aboutInfo.Models.Count(m => string.Equals(m.Status, "Up", StringComparison.OrdinalIgnoreCase));
            var totalModels = aboutInfo.Models.Count;

            return JsonSerializer.Serialize(new
            {
                health_status = "Healthy",
                app_server_version = aboutInfo.AppServerVersion,
                total_models = totalModels,
                models_up = upModels,
                models_down = totalModels - upModels,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                health_status = "Error",
                error = $"Health check failed: {ex.Message}",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            });
        }
    }

    [McpServerTool, Description("Search for business processes by name or abbreviation")]
    public async Task<string> SearchBusinessProcesses(string searchTerm)
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        if (string.IsNullOrWhiteSpace(searchTerm))
            return JsonSerializer.Serialize(new { error = "Search term is required" });

        try
        {
            var businessProcesses = await _appserverService.GetBusinessProcessesAsync();
            if (businessProcesses == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve business processes. The server may be unavailable." });
            }

            var matchingProcesses = businessProcesses.BusinessProcesses
                .Where(bp =>
                    bp.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    bp.Abbreviation.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    bp.Id.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Select(bp => new
                {
                    id = bp.Id,
                    name = bp.Name,
                    abbreviation = bp.Abbreviation,
                    enabled = bp.Enabled,
                    is_allowed = bp.IsAllowed,
                    system = bp.System,
                    order = bp.Order
                }).ToList();

            return JsonSerializer.Serialize(new
            {
                search_term = searchTerm,
                total_found = matchingProcesses.Count,
                total_processes = businessProcesses.BusinessProcesses.Count,
                matching_processes = matchingProcesses
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error searching business processes: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get summary statistics for all models and business processes")]
    public async Task<string> GetSystemSummary()
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        try
        {
            var aboutInfo = await _appserverService.GetAboutAsync();
            var businessProcesses = await _appserverService.GetBusinessProcessesAsync();

            var summary = new
            {
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                appserver = aboutInfo != null ? new
                {
                    version = aboutInfo.AppServerVersion,
                    status = "Connected",
                    models = new
                    {
                        total = aboutInfo.Models.Count,
                        up = aboutInfo.Models.Count(m => string.Equals(m.Status, "Up", StringComparison.OrdinalIgnoreCase)),
                        down = aboutInfo.Models.Count(m => !string.Equals(m.Status, "Up", StringComparison.OrdinalIgnoreCase)),
                        real_time = aboutInfo.Models.Count(m => m.IsRealTime)
                    }
                } : new
                {
                    version = "Unknown",
                    status = "Disconnected",
                    models = new { total = 0, up = 0, down = 0, real_time = 0 }
                },
                business_processes = businessProcesses != null ? new
                {
                    total = businessProcesses.BusinessProcesses.Count,
                    enabled = businessProcesses.BusinessProcesses.Count(bp => bp.Enabled),
                    disabled = businessProcesses.BusinessProcesses.Count(bp => !bp.Enabled),
                    allowed = businessProcesses.BusinessProcesses.Count(bp => bp.IsAllowed),
                    system = businessProcesses.BusinessProcesses.Count(bp => bp.System)
                } : new
                {
                    total = 0,
                    enabled = 0,
                    disabled = 0,
                    allowed = 0,
                    system = 0
                }
            };

            return JsonSerializer.Serialize(summary);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error generating system summary: {ex.Message}" });
        }
    }    // AngleController MCP Tools    [McpServerTool, Description("Search for angles using Solr with flexible query parameters including filters, sorting, and pagination")]
    public async Task<string> SearchAngles(
        string? query = "*:*",
        string? fields = "*",
        string? sort = "",
        int start = 0,
        int rows = 10,
        bool facet = false,
        string? facetFields = "",
        bool highlight = false,
        string? highlightFields = "",
        string? filterQueries = "")
    {
        if (_angleService == null)
            return JsonSerializer.Serialize(new { error = "AngleService not initialized" });

        try
        {
            var searchRequest = new AngleSearchRequest
            {
                Query = query ?? "*:*",
                Fields = fields ?? "*",
                Sort = sort ?? "",
                Start = start,
                Rows = rows,
                Facet = facet,
                Highlight = highlight,
                HighlightFields = highlightFields ?? ""
            };

            if (facet && !string.IsNullOrEmpty(facetFields))
            {
                searchRequest.FacetFields = facetFields.Split(',').Select(f => f.Trim()).ToList();
            }

            if (!string.IsNullOrEmpty(filterQueries))
            {
                searchRequest.FilterQueries = filterQueries.Split(',').Select(f => f.Trim()).ToList();
            }

            var result = await _angleService.SearchAnglesAsync(searchRequest);
            return JsonSerializer.Serialize(result, AppserverContext.Default.AngleSearchResponse);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error searching angles: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get a specific angle by its unique identifier")]
    public async Task<string> GetAngleById(string angleId)
    {
        if (_angleService == null)
            return JsonSerializer.Serialize(new { error = "AngleService not initialized" });

        if (string.IsNullOrWhiteSpace(angleId))
            return JsonSerializer.Serialize(new { error = "Angle ID is required" });

        try
        {
            var angle = await _angleService.GetAngleByIdAsync(angleId);
            if (angle == null)
            {
                return JsonSerializer.Serialize(new { error = $"Angle with ID '{angleId}' not found" });
            }

            return JsonSerializer.Serialize(angle, AppserverContext.Default.AngleDocument);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving angle: {ex.Message}" });
        }
    }
    [McpServerTool, Description("Filter angles with advanced criteria including multiple field filters and date ranges")]
    public async Task<string> FilterAngles(
        string? field = "",
        string? value = "",
        string? operator_ = "equals",
        string? from = null,
        string? to = null,
        int start = 0,
        int rows = 10,
        string? sort = "")
    {
        if (_angleService == null)
            return JsonSerializer.Serialize(new { error = "AngleService not initialized" });

        try
        {
            var filters = new List<AngleFilterRequest>();

            if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(value))
            {
                filters.Add(new AngleFilterRequest
                {
                    Field = field,
                    Value = value,
                    Operator = operator_ ?? "equals",
                    From = from,
                    To = to
                });
            }

            var result = await _angleService.FilterAnglesAsync(filters, start, rows, sort ?? "");
            return JsonSerializer.Serialize(result, AppserverContext.Default.AngleSearchResponse);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error filtering angles: {ex.Message}" });
        }
    }
    [McpServerTool, Description("Get comprehensive angle statistics including counts, facets, and analytics data")]
    public async Task<string> GetAngleStatistics()
    {
        if (_angleService == null)
            return JsonSerializer.Serialize(new { error = "AngleService not initialized" });

        try
        {
            var result = await _angleService.GetAngleStatisticsAsync();
            return JsonSerializer.Serialize(result, AppserverContext.Default.AngleStatisticsResponse);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving angle statistics: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get system license information")]
    public async Task<string> GetSystemLicense()
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        try
        {
            var licenseInfo = await _appserverService.GetLicenseAsync();
            if (licenseInfo == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve license information. The server may be unavailable." });
            }

            return JsonSerializer.Serialize(licenseInfo, typeof(JsonElement), AppserverContext.Default);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving license information: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get list of all users")]
    public async Task<string> GetUsers()
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        try
        {
            var usersList = await _appserverService.GetUsersAsync();
            if (usersList == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve users list. The server may be unavailable." });
            }

            return JsonSerializer.Serialize(usersList, typeof(List<UserView>), AppserverContext.Default);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving users list: {ex.Message}" });
        }
    }
}