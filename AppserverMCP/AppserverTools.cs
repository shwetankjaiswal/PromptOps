using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using AppserverMCP.Services;
using AppserverMCP.Models;

namespace AppserverMCP;

[McpServerToolType]
public sealed class AppserverTools(AppserverService appserverService, AngleService angleService)
{
    private const int DelayBetweenRequestsMs = 100; // Delay in milliseconds between requests
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
    }
    // AngleController MCP Tools
    //[McpServerTool, Description("Search for angles using Solr with flexible query parameters including filters, sorting, and pagination")]
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

    //[McpServerTool, Description("Get a specific angle by its unique identifier")]
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
    }    [McpServerTool, Description("Get all angles or Get angle by search query if specified")]
    public async Task<string> GetAngles(string? query = null)
    {
        if (_angleService == null)
            return JsonSerializer.Serialize(new { error = "AngleService not initialized" });        try
        {
            var angle = await _angleService.GetAngles(query ?? "*:*");

            return JsonSerializer.Serialize(angle, AppserverContext.Default.AngleSearchResponse);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving angle: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get user settings of the user from app server and takes user's name as input")]
    public async Task<string> GetUserSettings(string username)
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        try
        {
            var allUsers = await _appserverService.GetAllUser();
            string? uri = allUsers?.FirstOrDefault(u => u.FullName.Equals(username, StringComparison.OrdinalIgnoreCase)).Uri;
            if (uri == null)
            {
                return JsonSerializer.Serialize(new { error = "user doesn't exist. please specify exact name" });
            }

            var userSettings = await _appserverService.GetUserSettingsAsync(uri);
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

    [McpServerTool, Description("updates user settings such as default Currency and takes user name and default Currency as input to be updated")]
    public async Task<string> UpdateUserCurrency(string username, string defaultCurrency)
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        try
        {
            var allUsers = await _appserverService.GetAllUser();
            string? uri = allUsers?.FirstOrDefault(u => u.FullName.Equals(username, StringComparison.OrdinalIgnoreCase)).Uri;
            if (uri == null)
            {
                return JsonSerializer.Serialize(new { error = "user doesn't exist. please specify exact name" });
            }

            var userSettings = await _appserverService.PutUserCurrencyAsync(uri, defaultCurrency);
            if (!userSettings)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve Appserver information. The server may be unavailable." });
            }

            return $"{defaultCurrency} updated for {username}";

        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving Appserver information: {ex.Message}" });
        }
    }

    //[McpServerTool, Description("Filter angles with advanced criteria including multiple field filters and date ranges")]
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

    [McpServerTool, Description("Execute an angle display to get results data")]
    public async Task<string> ExecuteAngleDisplay(
        [Description("ID(integer) of the model containing the angle")] int modelId,
        [Description("ID(integer) of the angle to execute")] int angleId,
        [Description("ID(integer) of the display within the angle to execute")] int displayId)
    {
        if (_angleService == null)
            return JsonSerializer.Serialize(new { error = "AngleService not initialized" });

        if (modelId <= 0)
            return JsonSerializer.Serialize(new { error = "Model ID must be a positive integer" });

        if (angleId <= 0)
            return JsonSerializer.Serialize(new { error = "Angle ID must be a positive integer" });

        if (displayId <= 0)
            return JsonSerializer.Serialize(new { error = "Display ID must be a positive integer" });

        try
        {
            var result = await _angleService.ExecuteAngleDisplay(modelId, angleId, displayId);
            if (result == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to execute angle display. The server may be unavailable or the model/angle/display IDs may be invalid." });
            }

            return JsonSerializer.Serialize(result, AppserverContext.Default.ExecuteAngleDisplayResponse);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error executing angle display: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get the execution status of an angle display result using the URI from ExecuteAngleDisplay response. This tool can be polled until the angle execution status is finished")]
    public async Task<string> GetAngleDisplayExecutionStatus(
        [Description("URI of the result from ExecuteAngleDisplay response (e.g., '/results/15')")] string resultUri)
    {
        if (_angleService == null)
            return JsonSerializer.Serialize(new { error = "AngleService not initialized" });

        if (string.IsNullOrWhiteSpace(resultUri))
            return JsonSerializer.Serialize(new { error = "Result URI is required (e.g., 'results/15' from ExecuteAngleDisplay response)" });

        try
        {
            var result = await _angleService.GetAngleDisplayExecutionStatus(resultUri);
            if (result == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve angle display execution status. The result URI may be invalid or the server may be unavailable." });
            }

            return JsonSerializer.Serialize(result, AppserverContext.Default.GetAngleDisplayExecutionStatusResponse);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving angle display execution status: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get a single page of data rows from an angle display execution result with manual pagination control")]
    public async Task<string> GetAngleDisplayDataRowsPage(
        [Description("Data rows URI from the execution status response (e.g., '/results/15/datarows')")] string dataRowsUri,
        [Description("Starting offset for pagination (default: 0)")] int offset = 0,
        [Description("Number of rows to fetch in this page (default: 300, max: 1000)")] int limit = 300,
        [Description("Comma-separated list of field names to retrieve. This list comes from default_fields property in AngleDisplayExecutionStatus response. If not provided, will use all available fields")] string? fields = null)
    {
        if (_angleService == null)
            return JsonSerializer.Serialize(new { error = "AngleService not initialized" });

        if (string.IsNullOrWhiteSpace(dataRowsUri))
            return JsonSerializer.Serialize(new { error = "Data rows URI is required" });

        if (offset < 0)
            return JsonSerializer.Serialize(new { error = "Offset must be 0 or greater" });

        if (limit <= 0 || limit > 1000)
            return JsonSerializer.Serialize(new { error = "Limit must be between 1 and 1000" });

        try
        {
            var fieldsList = string.IsNullOrWhiteSpace(fields) 
                ? null 
                : fields.Split(',').Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f)).ToList();

            var result = await _angleService.GetDataRows(dataRowsUri, offset, limit, fieldsList);
            if (result == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve data rows. The URI may be invalid or the server may be unavailable." });
            }

            return JsonSerializer.Serialize(result, AppserverContext.Default.DataRowsResponse);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving data rows page: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get data rows from an angle display execution result with automatic pagination. Fetches all available rows based on total count and default fields from execution status")]
    public async Task<string> GetAngleDisplayDataRows(
        [Description("Data rows URI from the execution status response (e.g., '/results/15/datarows' from the data_rows property)")] string dataRowsUri,
        [Description("Comma-separated list of field names to retrieve. If not provided, will use all available fields")] string? fields = null,
        [Description("Maximum number of rows to fetch per request (default: 300, max recommended: 500)")] int batchSize = 300,
        [Description("Maximum total number of rows to fetch across all pages (default: 10000). Set to -1 for unlimited")] int maxTotalRows = 10000)
    {
        if (_angleService == null)
            return JsonSerializer.Serialize(new { error = "AngleService not initialized" });

        if (string.IsNullOrWhiteSpace(dataRowsUri))
            return JsonSerializer.Serialize(new { error = "Data rows URI is required (e.g., '/results/15/datarows' from execution status response)" });

        if (batchSize <= 0 || batchSize > 1000)
            return JsonSerializer.Serialize(new { error = "Batch size must be between 1 and 1000" });

        try
        {
            var fieldsList = string.IsNullOrWhiteSpace(fields) 
                ? null 
                : fields.Split(',').Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f)).ToList();

            // First request to get total count and determine pagination
            var firstBatch = await _angleService.GetDataRows(dataRowsUri, 0, Math.Min(batchSize, maxTotalRows > 0 ? maxTotalRows : batchSize), fieldsList);
            
            if (firstBatch == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve data rows. The URI may be invalid or the server may be unavailable." });
            }

            var allRows = new List<DataRow>(firstBatch.Rows);
            var totalRows = firstBatch.Header.Total;
            var fieldsReturned = firstBatch.Fields;
            
            // If we have more rows to fetch and haven't hit our limit
            if (totalRows > batchSize && (maxTotalRows == -1 || allRows.Count < maxTotalRows))
            {
                var remaining = maxTotalRows == -1 ? totalRows - batchSize : Math.Min(maxTotalRows - batchSize, totalRows - batchSize);
                var offset = batchSize;

                while (remaining > 0 && offset < totalRows)
                {
                    var currentBatchSize = Math.Min(batchSize, remaining);
                    var nextBatch = await _angleService.GetDataRows(dataRowsUri, offset, currentBatchSize, fieldsList);
                    
                    if (nextBatch == null || nextBatch.Rows.Count == 0)
                        break;

                    allRows.AddRange(nextBatch.Rows);
                    offset += nextBatch.Rows.Count;
                    remaining -= nextBatch.Rows.Count;

                    // Add a small delay between requests to be gentle on the server
                    await Task.Delay(DelayBetweenRequestsMs);
                }
            }

            var result = new
            {
                summary = new
                {
                    total_rows_available = totalRows,
                    rows_fetched = allRows.Count,
                    fields_count = fieldsReturned.Count,
                    batch_size_used = batchSize,
                    pages_fetched = (int)Math.Ceiling((double)allRows.Count / batchSize),
                    execution_time = firstBatch.ExecutionTime
                },
                fields = fieldsReturned,
                rows = allRows
            };

            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving data rows: {ex.Message}" });
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
        }        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving users list: {ex.Message}" });
        }
    }    [McpServerTool, Description("Get all classes for a specific model by model ID. Uses the URI from GetModels response to access model classes.")]
    public async Task<string> GetModelClasses(
        [Description("The model ID to get classes for")] string modelId,
        [Description("Starting offset for pagination (default: 0)")] int offset = 0,
        [Description("Number of classes to retrieve (default: 100, max: 1000)")] int limit = 100,
        [Description("Comma-separated list of specific class IDs to retrieve (optional). If not provided, all classes will be returned.")] string? ids = null)
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        if (string.IsNullOrWhiteSpace(modelId))
            return JsonSerializer.Serialize(new { error = "Model ID is required" });

        if (offset < 0)
            return JsonSerializer.Serialize(new { error = "Offset must be non-negative" });

        if (limit <= 0 || limit > 1000)
            return JsonSerializer.Serialize(new { error = "Limit must be between 1 and 1000" });

        try
        {
            // First, get the model information to retrieve the URI
            var modelsResponse = await _appserverService.GetModelsAsync(0, 1000); // Get all models to find the right one
            if (modelsResponse == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve models information. The server may be unavailable." });
            }

            // Find the model by ID
            var targetModel = modelsResponse.Models.FirstOrDefault(m => 
                string.Equals(m.Id, modelId, StringComparison.OrdinalIgnoreCase));

            if (targetModel == null)
            {
                return JsonSerializer.Serialize(new { 
                    error = $"Model with ID '{modelId}' not found",
                    available_models = modelsResponse.Models.Select(m => new { id = m.Id, short_name = m.ShortName }).ToList()
                });
            }

            // Use the GetModelClassesByUri method which should use the model's URI
            var modelClasses = await _appserverService.GetModelClassesByUriAsync(targetModel.Uri, offset, limit, ids);
            if (modelClasses == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve model classes. The server may be unavailable or the model URI may be invalid." });
            }            // Create beautiful tabular structure for model classes
            var classesTable = modelClasses.Classes?.Select(c => new
            {
                // Basic Class Information
                class_info = new
                {
                    class_id = c.Id,
                    short_name = c.ShortName,
                    long_name = c.LongName,
                    uri = c.Uri,
                    help_id = c.HelpId
                },
                
                // Business Information
                business_info = new
                {
                    main_businessprocess = c.MainBusinessprocess ?? "Not specified",
                    help_text = !string.IsNullOrEmpty(c.HelpText) ? c.HelpText : "No help text available"
                }
            }).ToArray();

            return JsonSerializer.Serialize(new
            {
                // Summary Header
                summary = new
                {
                    title = $"📋 Classes for Model: {targetModel.ShortName}",
                    timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    request_details = new
                    {
                        model_id = modelId,
                        offset = offset,
                        limit = limit,
                        specific_ids = ids
                    }
                },
                
                // Model Context Table
                model_context = new
                {
                    basic_info = new
                    {
                        id = targetModel.Id,
                        short_name = targetModel.ShortName,
                        full_name = targetModel.LongName,
                        environment = targetModel.Environment,
                        type = targetModel.Type,
                        uri = targetModel.Uri
                    },
                    status = new
                    {
                        refresh_enabled = targetModel.UseRefresh ? "✓ Yes" : "✗ No",
                        postprocessing = targetModel.IsPostprocessing ? "🔄 Active" : "⭕ Inactive"
                    }
                },
                
                // Classes Summary Statistics
                classes_statistics = new
                {
                    overview = new
                    {
                        total_available = modelClasses.Header.Total,
                        returned_count = modelClasses.Classes?.Count ?? 0,
                        offset = modelClasses.Header.Offset,
                        limit = modelClasses.Header.Limit
                    },
                    breakdown = new
                    {
                        classes_with_help = modelClasses.Classes?.Count(c => !string.IsNullOrEmpty(c.HelpText)) ?? 0,
                        classes_with_business_process = modelClasses.Classes?.Count(c => !string.IsNullOrEmpty(c.MainBusinessprocess)) ?? 0,
                        classes_with_help_id = modelClasses.Classes?.Count(c => !string.IsNullOrEmpty(c.HelpId)) ?? 0
                    }
                },
                
                // Main Classes Table
                classes_table = classesTable
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving model classes: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get all available models with detailed information including authorizations, environment, active languages, and created information")]
    public async Task<string> GetModels(
        [Description("Starting offset for pagination (default: 0)")] int offset = 0,
        [Description("Number of models to retrieve (default: 100, max: 1000)")] int limit = 100)
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        if (offset < 0)
            return JsonSerializer.Serialize(new { error = "Offset must be non-negative" });

        if (limit <= 0 || limit > 1000)
            return JsonSerializer.Serialize(new { error = "Limit must be between 1 and 1000" });

        try
        {
            var modelsResponse = await _appserverService.GetModelsAsync(offset, limit);
            if (modelsResponse == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve models. The server may be unavailable." });
            }            // Create a beautiful tabular structure for model data
            var modelsTable = modelsResponse.Models.Select(m => new
            {
                // Basic Information Table
                basic_info = new
                {
                    model_id = m.Id,
                    short_name = m.ShortName,
                    full_name = m.LongName,
                    abbreviation = m.Abbreviation,
                    environment = m.Environment,
                    type = m.Type,
                    uri = m.Uri
                },
                
                // Status & Configuration Table
                status_config = new
                {
                    refresh_enabled = m.UseRefresh ? "✓ Yes" : "✗ No",
                    postprocessing_active = m.IsPostprocessing ? "🔄 Active" : "⭕ Inactive",
                    switch_when_postprocessing = m.SwitchWhenPostprocessing ? "✓ Enabled" : "✗ Disabled",
                    active_languages = m.ActiveLanguages?.Count > 0 ? string.Join(", ", m.ActiveLanguages) : "None",
                    language_count = m.ActiveLanguages?.Count ?? 0
                },
                
                // Creation Details Table
                creation_info = new
                {
                    created_by = m.Created.FullName,
                    creator_username = m.Created.User,
                    created_date = DateTimeOffset.FromUnixTimeSeconds(m.Created.DateTime).ToString("yyyy-MM-dd"),
                    created_time = DateTimeOffset.FromUnixTimeSeconds(m.Created.DateTime).ToString("HH:mm:ss UTC"),
                    created_timestamp = m.Created.DateTime
                },
                
                // Permissions Table
                permissions = new
                {
                    data_access = m.Authorizations.AccessData ? "✓" : "✗",
                    update_model = m.Authorizations.Update ? "✓" : "✗",
                    delete_model = m.Authorizations.Delete ? "✓" : "✗",
                    create_angles = m.Authorizations.CreateAngle ? "✓" : "✗",
                    publish_dashboards = m.Authorizations.PublishDashboard ? "✓" : "✗",
                    manage_settings = m.Authorizations.ManageSettings ? "✓" : "✗",
                    manage_roles = m.Authorizations.ManageRoles ? "✓" : "✗",
                    assign_roles = m.Authorizations.AssignRoles ? "✓" : "✗"
                },
                  // Technical Details Table
                technical_info = new
                {
                    packages_info = !string.IsNullOrEmpty(m.Packages) ? m.Packages : "No packages specified",
                    modelserver_settings = !string.IsNullOrEmpty(m.ModelserverSettings) ? m.ModelserverSettings : "Default settings"
                }
            }).ToList();// Enhanced statistics with tabular presentation
            var enhancedStatistics = new
            {
                overview = new
                {
                    total_models = modelsResponse.Header.Total,
                    models_returned = modelsResponse.Models.Count,
                    offset = modelsResponse.Header.Offset,
                    limit = modelsResponse.Header.Limit
                },
                
                feature_breakdown = new
                {
                    refresh_enabled = modelsResponse.Models.Count(m => m.UseRefresh),
                    refresh_disabled = modelsResponse.Models.Count(m => !m.UseRefresh),
                    postprocessing_active = modelsResponse.Models.Count(m => m.IsPostprocessing),
                    postprocessing_inactive = modelsResponse.Models.Count(m => !m.IsPostprocessing)
                },
                
                distribution_tables = new
                {
                    by_type = modelsResponse.Models
                        .GroupBy(m => m.Type ?? "Unknown")
                        .Select(g => new { type = g.Key, count = g.Count() })
                        .OrderByDescending(x => x.count)
                        .ToList(),
                        
                    by_environment = modelsResponse.Models
                        .GroupBy(m => m.Environment ?? "Unknown")
                        .Select(g => new { environment = g.Key, count = g.Count() })
                        .OrderByDescending(x => x.count)
                        .ToList(),
                        
                    language_usage = modelsResponse.Models
                        .Where(m => m.ActiveLanguages != null && m.ActiveLanguages.Any())
                        .SelectMany(m => m.ActiveLanguages)
                        .GroupBy(lang => lang)
                        .Select(g => new { language = g.Key, model_count = g.Count() })
                        .OrderByDescending(x => x.model_count)
                        .ToList()
                }
            };

            return JsonSerializer.Serialize(new
            {
                // Summary Header
                summary = new
                {
                    title = "📊 Models Overview",
                    timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    pagination = enhancedStatistics.overview
                },
                
                // Enhanced Statistics Tables
                statistics = enhancedStatistics,
                
                // Main Models Table
                models_table = modelsTable
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving models: {ex.Message}" });
        }
    }
}