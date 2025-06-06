using AppserverMCP.Interfaces;
using AppserverMCP.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppserverMCP;

public class AppserverService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AppserverService> _logger;
    private readonly IPlatformService _platformService;
    private readonly string _baseUrl;

    public AppserverService(IHttpClientFactory httpClientFactory, ILogger<AppserverService> logger, IConfiguration configuration, IPlatformService platformService)
    {
        // Create HttpClient with custom handler to disable SSL certificate validation
        var handler = new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
        };

        _httpClient = new HttpClient(handler);
        _logger = logger;
        _baseUrl = configuration.GetValue<string>("AppserverBaseUrl") ?? "http://localhost:8080";
        _platformService = platformService;

        // Set a reasonable timeout for API calls
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<AboutOutputView?> GetAboutAsync()
    {
        HttpResponseMessage? response = null;
        try
        {
            _logger.LogInformation("Fetching about information from {BaseUrl}/about", _baseUrl);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/about");
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var aboutResponse = await response.Content.ReadFromJsonAsync(AppserverContext.Default.AboutOutputView);

            if (aboutResponse != null)
            {
                _logger.LogInformation("Successfully fetched about information for app server version {Version}", aboutResponse.AppServerVersion);
            }

            return aboutResponse;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = response?.Content != null ? await response.Content.ReadAsStringAsync() : "No response content";
            _logger.LogError(ex, $"Failed to fetch about information from Appserver: {errorResponse}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to Appserver timed out");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse about response from Appserver");
            return null;
        }
    }



    public async Task<BusinessProcessResponse?> GetBusinessProcessesAsync()
    {
        HttpResponseMessage? response = null;
        try
        {
            _logger.LogInformation("Fetching business processes from {BaseUrl}/businessprocesses", _baseUrl);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/businessprocesses");
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var businessProcesses = await response.Content.ReadFromJsonAsync<BusinessProcessResponse>();

            if (businessProcesses != null)
            {
                _logger.LogInformation("Successfully fetched business processes.");
            }

            return businessProcesses;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = response?.Content != null ? await response.Content.ReadAsStringAsync() : "No response content";
            _logger.LogError(ex, "Failed to fetch business processes from Appserver: {ErrorResponse}", errorResponse);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to Appserver timed out");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse business processes response from Appserver");
            return null;
        }
    }

    public async Task<string?> GetServerStatusAsync()
    {
        try
        {
            _logger.LogInformation("Checking server status at {BaseUrl}/health", _baseUrl);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/health");
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return "Healthy";
            }
            else
            {
                return $"Unhealthy - Status Code: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check server status");
            return $"Error - {ex.Message}";
        }
    }

    public async Task<Dictionary<string, object>?> GetModelStatisticsAsync()
    {
        try
        {
            var aboutInfo = await GetAboutAsync();
            if (aboutInfo == null) return null;

            var stats = new Dictionary<string, object>
            {
                ["total_models"] = aboutInfo.Models.Count,
                ["models_up"] = aboutInfo.Models.Count(m => string.Equals(m.Status, "Up", StringComparison.OrdinalIgnoreCase)),
                ["models_down"] = aboutInfo.Models.Count(m => !string.Equals(m.Status, "Up", StringComparison.OrdinalIgnoreCase)),
                ["real_time_models"] = aboutInfo.Models.Count(m => m.IsRealTime),
                ["batch_models"] = aboutInfo.Models.Count(m => !m.IsRealTime),
                ["latest_model_timestamp"] = aboutInfo.Models.Any() ? aboutInfo.Models.Max(m => m.ModeldataTimestamp) : 0
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model statistics");
            return null;
        }
    }

    public async Task<TaskExecutionResponse?> ExecuteTaskAsync(string taskId, string? reason = null)
    {
        HttpResponseMessage? response = null;
        try
        {
            _logger.LogInformation("Executing task {TaskId} with reason: {Reason}", taskId, reason ?? "No reason provided");

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/tasks/{taskId}/execution");
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            var requestBody = new TaskExecutionRequest
            {
                Start = true,
                Reason = reason ?? "Automated execution"
            };

            request.Content = JsonContent.Create(requestBody, AppserverContext.Default.TaskExecutionRequest);

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var executionResponse = await response.Content.ReadFromJsonAsync(AppserverContext.Default.TaskExecutionResponse);

            if (executionResponse != null)
            {
                _logger.LogInformation("Successfully executed task {TaskId}", taskId);
            }

            return executionResponse;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = response != null ? await response.Content.ReadAsStringAsync() : "Unknown error";
            _logger.LogError(ex, "Failed to execute task {TaskId}: {ErrorMessage}", taskId, errorResponse);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to execute task {TaskId} timed out", taskId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse task execution response for task {TaskId}", taskId);
            return null;
        }
    }

    public async Task<TaskStatusResponse?> GetTaskStatusAsync(string taskId)
    {
        HttpResponseMessage? response = null;
        try
        {
            _logger.LogInformation("Fetching status for task {TaskId}", taskId);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/tasks/{taskId}");
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var statusResponse = await response.Content.ReadFromJsonAsync(AppserverContext.Default.TaskStatusResponse);

            if (statusResponse != null)
            {
                _logger.LogInformation("Successfully fetched status for task {TaskId}: {Status}", taskId, statusResponse.Status);
            }

            return statusResponse;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = response != null ? await response.Content.ReadAsStringAsync() : "Unknown error";
            _logger.LogError(ex, "Failed to fetch status for task {TaskId}: {ErrorMessage}", taskId, errorResponse);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to fetch task status for {TaskId} timed out", taskId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse task status response for task {TaskId}", taskId);
            return null;
        }
    }

    public async Task<List<TaskItemView>?> GetTasksAsync()
    {
        HttpResponseMessage? response = null;
        try
        {
            _logger.LogInformation("Fetching tasks list from {BaseUrl}/tasks", _baseUrl);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/tasks?types=export_angle_to_datastore");
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var tasksResponse = await response.Content.ReadFromJsonAsync(AppserverContext.Default.TasksListResponse);

            if (tasksResponse != null)
            {
                _logger.LogInformation("Successfully fetched {Count} tasks (total: {Total})", tasksResponse.Tasks.Count, tasksResponse.Header.Total);
                return tasksResponse.Tasks;
            }

            return null;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = response != null ? await response.Content.ReadAsStringAsync() : "Unknown error";
            _logger.LogError(ex, "Failed to fetch tasks list: {ErrorMessage}", errorResponse);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to fetch tasks list timed out");
            return null;
        }
        catch (JsonException ex)
        {
            var errorResponse = response != null ? await response.Content.ReadAsStringAsync() : "Unknown error";
            _logger.LogError(ex, $"Failed to parse tasks list response: {errorResponse}");
            return null;
        }
    }

    public async Task<JsonElement?> GetLicenseAsync()
    {
        HttpResponseMessage? response = null;
        try
        {
            _logger.LogInformation("Fetching license information from {BaseUrl}/system/license", _baseUrl);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/system/license");
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var licenseResponse = await response.Content.ReadFromJsonAsync<JsonElement>(AppserverContext.Default.Options);
            
            _logger.LogInformation("Successfully fetched license information");
            return licenseResponse;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = response != null ? await response.Content.ReadAsStringAsync() : "Unknown error";
            _logger.LogError(ex, "Failed to fetch license information: {ErrorMessage}", errorResponse);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to fetch license information timed out");
            return null;
        }        catch (JsonException ex)
        {
            var errorResponse = response != null ? await response.Content.ReadAsStringAsync() : "Unknown error";
            _logger.LogError(ex, "Failed to parse license information response: {ErrorResponse}", errorResponse);
            return null;
        }
    }

    public async Task<List<UserView>?> GetUsersAsync()
    {
        HttpResponseMessage? response = null;
        try
        {
            _logger.LogInformation("Fetching users list from {BaseUrl}/users", _baseUrl);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users");
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var usersResponse = await response.Content.ReadFromJsonAsync(AppserverContext.Default.UsersListResponse);

            if (usersResponse != null)
            {
                _logger.LogInformation("Successfully fetched {Count} users (total: {Total})", usersResponse.Users.Count, usersResponse.Header.Total);
                return usersResponse.Users;
            }

            return null;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = response != null ? await response.Content.ReadAsStringAsync() : "Unknown error";
            _logger.LogError(ex, "Failed to fetch users list: {ErrorMessage}", errorResponse);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to fetch users list timed out");
            return null;
        }
        catch (JsonException ex)
        {
            var errorResponse = response != null ? await response.Content.ReadAsStringAsync() : "Unknown error";
            _logger.LogError(ex, "Failed to parse users list response: {ErrorResponse}", errorResponse);
            return null;
        }
    }

    public async Task<List<(string FullName, string Uri)>?> GetAllUser()
    {
        HttpResponseMessage? response = null;
        try
        {
            _logger.LogInformation("Fetching User {BaseUrl}/users", _baseUrl);
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users");
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var userListResponse = await response.Content.ReadFromJsonAsync(AppserverContext.Default.UserListResponse);
            if (userListResponse != null)
            {
                _logger.LogInformation("Successfully fetched All user");
                return userListResponse.Users
               .Select(u => (u.FullName, u.Uri))
               .ToList();
            }
            return new List<(string, string)>();
        }        catch (HttpRequestException ex)
        {
            var errorResponse = response?.Content != null ? await response.Content.ReadAsStringAsync() : "No response content";
            _logger.LogError(ex, $"Failed to fetch users from Appserver: {errorResponse}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to Appserver for users timed out");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse users response from Appserver");
            return null;
        }
    }

    public async Task<bool> PutUserCurrencyAsync(string uri, string defaultCurrency)
    {
        HttpResponseMessage? response = null;
        try
        {
            // 1. Get current user settings
            var currentSettings = await GetUserSettingsAsync(uri);
            if (currentSettings == null)
            {
                _logger.LogError("Cannot update currency: failed to fetch current user settings for user");
                return false;
            }

            // 2. Map UserSettingsResponse to ModifyUserSettingsView
            var modifyView = new ModifyUserSettingsView
            {
                default_language = currentSettings.DefaultLanguage,
                default_currency = defaultCurrency, // update only currency
                client_settings = currentSettings.ClientSettings,
                default_export_lines = currentSettings.DefaultExportLines,
                sap_fields_in_chooser = currentSettings.SapFieldsInChooser,
                sap_fields_in_header = currentSettings.SapFieldsInHeader,
                manual_insert_column = currentSettings.ManualInsertColumn,
                compressed_list_header = currentSettings.CompressedListHeader,
                compressed_bp_bar = currentSettings.CompressedBpBar,
                default_business_processes = currentSettings.DefaultBusinessProcesses,
                auto_execute_items_on_login = currentSettings.AutoExecuteItemsOnLogin,
                // The following property is not present in UserSettingsResponse, set to null or as needed
                auto_execute_last_search = null,
                format_locale = currentSettings.FormatLocale,
                format_numbers = currentSettings.FormatNumbers,
                format_currencies = currentSettings.FormatCurrencies,
                format_percentages = currentSettings.FormatPercentages,
                format_enum = currentSettings.FormatEnum,
                format_date = currentSettings.FormatDate,
                format_period = currentSettings.FormatPeriod,
                format_time = currentSettings.FormatTime,
                hide_other_users_private_display = currentSettings.HideOtherUsersPrivateDisplay
            };

            // 3. Send PUT request
            using var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}{uri}/settings")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(modifyView),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully updated user currency for user");
            return true;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = response != null ? await response.Content.ReadAsStringAsync() : string.Empty;
            _logger.LogError(ex, $"Failed to update user currency: {errorResponse}");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to Appserver for updating user currency timed out");
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize user currency payload");
            return false;
        }
    }

    public async Task<UserSettingsResponse?> GetUserSettingsAsync(string uri)
    {
        HttpResponseMessage? response = null;
        try
        {
            _logger.LogInformation("Fetching User Settings from {BaseUrl}/users/1/settings", _baseUrl);
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}{uri}/settings");
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var userSettings = await response.Content.ReadFromJsonAsync(AppserverContext.Default.UserSettingsResponse);
            if (userSettings != null)
            {
                _logger.LogInformation("Successfully fetched user settings");
            }
            return userSettings;
        }        catch (HttpRequestException ex)
        {
            var errorResponse = response?.Content != null ? await response.Content.ReadAsStringAsync() : "No response content";
            _logger.LogError(ex, $"Failed to fetch user settings from Appserver: {errorResponse}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to Appserver for user settings timed out");
            return null;
        }        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse user settings response from Appserver");
            return null;
        }
    }

    public async Task<ModelClassesResponse?> GetModelClassesAsync(string modelId, int offset = 0, int limit = 100, string? ids = null)
    {
        HttpResponseMessage? response = null;
        try
        {
            var queryParams = new List<string> { $"offset={offset}", $"limit={limit}" };
            if (!string.IsNullOrWhiteSpace(ids))
            {
                queryParams.Add($"ids={Uri.EscapeDataString(ids)}");
            }

            var queryString = string.Join("&", queryParams);
            var url = $"{_baseUrl}/models/{modelId}/classes?{queryString}";

            _logger.LogInformation("Fetching model classes from {Url}", url);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var classesResponse = await response.Content.ReadFromJsonAsync(AppserverContext.Default.ModelClassesResponse);
            if (classesResponse != null)
            {
                _logger.LogInformation("Successfully fetched {Count} classes for model {ModelId} (total: {Total})", 
                    classesResponse.Classes.Count, modelId, classesResponse.Header.Total);
            }
            return classesResponse;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = response != null ? await response.Content.ReadAsStringAsync() : "No response content";
            _logger.LogError(ex, "Failed to fetch model classes from Appserver: {ErrorMessage}", errorResponse);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to Appserver for model classes timed out");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse model classes response from Appserver");
            return null;
        }
    }

    public async Task<ModelClassesResponse?> GetModelClassesByUriAsync(string modelUri, int offset = 0, int limit = 100, string? ids = null)
    {
        HttpResponseMessage? response = null;
        try
        {
            var queryParams = new List<string> { $"offset={offset}", $"limit={limit}" };
            if (!string.IsNullOrWhiteSpace(ids))
            {
                queryParams.Add($"ids={Uri.EscapeDataString(ids)}");
            }

            var queryString = string.Join("&", queryParams);
            // Use the model URI directly and append /classes endpoint
            var url = $"{_baseUrl}{modelUri}/classes?{queryString}";

            _logger.LogInformation("Fetching model classes from {Url}", url);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var classesResponse = await response.Content.ReadFromJsonAsync(AppserverContext.Default.ModelClassesResponse);
            if (classesResponse != null)
            {
                _logger.LogInformation("Successfully fetched {Count} classes for model URI {ModelUri} (total: {Total})", 
                    classesResponse.Classes.Count, modelUri, classesResponse.Header.Total);
            }
            return classesResponse;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = response != null ? await response.Content.ReadAsStringAsync() : "No response content";
            _logger.LogError(ex, "Failed to fetch model classes from Appserver using URI {ModelUri}: {ErrorMessage}", modelUri, errorResponse);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to Appserver for model classes timed out for URI {ModelUri}", modelUri);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse model classes response from Appserver for URI {ModelUri}", modelUri);
            return null;
        }
    }

    public async Task<List<ComprehensiveModelInfo>?> GetAllModelsComprehensiveAsync()
    {
        HttpResponseMessage? response = null;
        try
        {
            _logger.LogInformation("Fetching comprehensive model information from {BaseUrl}/model/getallmodels", _baseUrl);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/model/getallmodels");
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();            // Since the endpoint returns HTML, we need to parse it manually
            var htmlContent = await response.Content.ReadAsStringAsync();
            var models = ParseHtmlModelsResponse(htmlContent);

            // If HTML parsing failed, fall back to basic model information
            if (models.Count == 0)
            {
                _logger.LogWarning("HTML parsing returned no models. Falling back to basic model information.");
                var aboutInfo = await GetAboutAsync();
                if (aboutInfo?.Models != null)
                {
                    foreach (var basicModel in aboutInfo.Models)
                    {
                        var fallbackModel = new ComprehensiveModelInfo
                        {
                            ModelId = basicModel.ModelId,
                            Status = basicModel.Status,
                            Version = basicModel.Version,
                            ModeldataTimestamp = basicModel.ModeldataTimestamp,
                            ModelDefinitionVersion = basicModel.ModelDefinitionVersion,
                            IsRealTime = basicModel.IsRealTime,
                            ShortName = basicModel.ModelId, // Use ModelId as fallback for display
                            LongName = basicModel.ModelId    // Use ModelId as fallback for display
                        };
                        models.Add(fallbackModel);
                    }
                    _logger.LogInformation("Added {Count} fallback models from basic model info", models.Count);
                }
            }

            _logger.LogInformation("Successfully parsed {Count} comprehensive models", models.Count);
            return models;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = response?.Content != null ? await response.Content.ReadAsStringAsync() : "No response content";
            _logger.LogError(ex, "Failed to fetch comprehensive model information: {ErrorMessage}", errorResponse);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to fetch comprehensive model information timed out");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse comprehensive model information");
            return null;
        }
    }    private List<ComprehensiveModelInfo> ParseHtmlModelsResponse(string htmlContent)
    {
        var models = new List<ComprehensiveModelInfo>();
        
        try
        {
            _logger.LogInformation("Parsing HTML response for models, content length: {Length}", htmlContent.Length);
            
            // Improved HTML parsing to extract model information
            var lines = htmlContent.Split('\n');
            ComprehensiveModelInfo? currentModel = null;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Look for model ID pattern - try multiple patterns
                if (line.Contains("model_id") || (line.Contains("Model ID") && currentModel == null))
                {
                    currentModel = new ComprehensiveModelInfo();
                    
                    // Try different extraction patterns for Model ID
                    var spanMatch = System.Text.RegularExpressions.Regex.Match(line, @"<span[^>]*>([^<]+)</span>");
                    var tdMatch = System.Text.RegularExpressions.Regex.Match(line, @"<td[^>]*>([^<]+)</td>");
                    var valueMatch = System.Text.RegularExpressions.Regex.Match(line, @">\s*(\w+)\s*<");
                    
                    if (spanMatch.Success && !string.IsNullOrWhiteSpace(spanMatch.Groups[1].Value))
                    {
                        currentModel.ModelId = spanMatch.Groups[1].Value.Trim();
                        _logger.LogDebug("Found Model ID (span): {ModelId}", currentModel.ModelId);
                    }
                    else if (tdMatch.Success && !string.IsNullOrWhiteSpace(tdMatch.Groups[1].Value))
                    {
                        currentModel.ModelId = tdMatch.Groups[1].Value.Trim();
                        _logger.LogDebug("Found Model ID (td): {ModelId}", currentModel.ModelId);
                    }
                    else if (valueMatch.Success && !string.IsNullOrWhiteSpace(valueMatch.Groups[1].Value))
                    {
                        currentModel.ModelId = valueMatch.Groups[1].Value.Trim();
                        _logger.LogDebug("Found Model ID (value): {ModelId}", currentModel.ModelId);
                    }
                    else
                    {
                        // Look at the next few lines for the actual model ID value
                        for (int j = i + 1; j < Math.Min(i + 5, lines.Length); j++)
                        {
                            var nextLine = lines[j].Trim();
                            var nextSpanMatch = System.Text.RegularExpressions.Regex.Match(nextLine, @"<span[^>]*>([^<]+)</span>");
                            var nextTdMatch = System.Text.RegularExpressions.Regex.Match(nextLine, @"<td[^>]*>([^<]+)</td>");
                            var nextValueMatch = System.Text.RegularExpressions.Regex.Match(nextLine, @">\s*(\w+)\s*<");
                            
                            if (nextSpanMatch.Success && !string.IsNullOrWhiteSpace(nextSpanMatch.Groups[1].Value))
                            {
                                currentModel.ModelId = nextSpanMatch.Groups[1].Value.Trim();
                                _logger.LogDebug("Found Model ID in next line (span): {ModelId}", currentModel.ModelId);
                                break;
                            }
                            else if (nextTdMatch.Success && !string.IsNullOrWhiteSpace(nextTdMatch.Groups[1].Value))
                            {
                                currentModel.ModelId = nextTdMatch.Groups[1].Value.Trim();
                                _logger.LogDebug("Found Model ID in next line (td): {ModelId}", currentModel.ModelId);
                                break;
                            }
                            else if (nextValueMatch.Success && !string.IsNullOrWhiteSpace(nextValueMatch.Groups[1].Value))
                            {
                                currentModel.ModelId = nextValueMatch.Groups[1].Value.Trim();
                                _logger.LogDebug("Found Model ID in next line (value): {ModelId}", currentModel.ModelId);
                                break;
                            }
                        }
                    }
                }                
                // Extract other fields with improved parsing
                if (currentModel != null)
                {
                    if (line.Contains("short_name"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"<(?:span|td)[^>]*>([^<]+)</(?:span|td)>");
                        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value)) 
                        {
                            currentModel.ShortName = match.Groups[1].Value.Trim();
                            _logger.LogDebug("Found Short Name: {ShortName}", currentModel.ShortName);
                        }
                    }
                    else if (line.Contains("long_name"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"<(?:span|td)[^>]*>([^<]+)</(?:span|td)>");
                        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value)) 
                        {
                            currentModel.LongName = match.Groups[1].Value.Trim();
                            _logger.LogDebug("Found Long Name: {LongName}", currentModel.LongName);
                        }
                    }
                    else if (line.Contains("status") && !line.Contains("modeldata_timestamp"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"<(?:span|td)[^>]*>([^<]+)</(?:span|td)>");
                        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value)) 
                        {
                            currentModel.Status = match.Groups[1].Value.Trim();
                            _logger.LogDebug("Found Status: {Status}", currentModel.Status);
                        }
                    }
                    else if (line.Contains("environment"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"<(?:span|td)[^>]*>([^<]+)</(?:span|td)>");
                        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value)) 
                        {
                            currentModel.Environment = match.Groups[1].Value.Trim();
                            _logger.LogDebug("Found Environment: {Environment}", currentModel.Environment);
                        }
                    }
                    else if (line.Contains("license_type"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"<(?:span|td)[^>]*>([^<]+)</(?:span|td)>");
                        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value)) 
                        {
                            currentModel.LicenseType = match.Groups[1].Value.Trim();
                            _logger.LogDebug("Found License Type: {LicenseType}", currentModel.LicenseType);
                        }
                    }
                    else if (line.Contains("version") && !line.Contains("model_definition"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"<(?:span|td)[^>]*>([^<]+)</(?:span|td)>");
                        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value)) 
                        {
                            currentModel.Version = match.Groups[1].Value.Trim();
                            _logger.LogDebug("Found Version: {Version}", currentModel.Version);
                        }
                    }
                    else if (line.Contains("created") && line.Contains("user"))
                    {
                        // Extract created information
                        var userMatch = System.Text.RegularExpressions.Regex.Match(line, @"user[^>]*>([^<]+)");
                        var fullNameMatch = System.Text.RegularExpressions.Regex.Match(line, @"full_name[^>]*>([^<]+)");
                        var datetimeMatch = System.Text.RegularExpressions.Regex.Match(line, @"datetime[^>]*>(\d+)");
                        
                        if (userMatch.Success) currentModel.Created.User = userMatch.Groups[1].Value.Trim();
                        if (fullNameMatch.Success) currentModel.Created.FullName = fullNameMatch.Groups[1].Value.Trim();
                        if (datetimeMatch.Success && long.TryParse(datetimeMatch.Groups[1].Value, out var datetime))
                            currentModel.Created.DateTime = datetime;
                    }
                    else if (line.Contains("base_model") && line.Contains("href"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"href=[""']([^""']+)[""']");
                        if (match.Success) currentModel.Uris.BaseModel = match.Groups[1].Value.Trim();
                    }
                    else if (line.Contains("base_classes") && line.Contains("href"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"href=[""']([^""']+)[""']");
                        if (match.Success) currentModel.Uris.BaseClasses = match.Groups[1].Value.Trim();
                    }
                    else if (line.Contains("base_fields") && line.Contains("href"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"href=[""']([^""']+)[""']");
                        if (match.Success) currentModel.Uris.BaseFields = match.Groups[1].Value.Trim();
                    }
                    else if (line.Contains("base_angles") && line.Contains("href"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"href=[""']([^""']+)[""']");
                        if (match.Success) currentModel.Uris.BaseAngles = match.Groups[1].Value.Trim();
                    }
                    else if (line.Contains("base_languages") && line.Contains("href"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"href=[""']([^""']+)[""']");
                        if (match.Success) currentModel.Uris.BaseLanguages = match.Groups[1].Value.Trim();
                    }                    else if (line.Contains("</tr>") && currentModel != null)
                    {
                        // End of model row, add to list if we have at least a model ID
                        if (!string.IsNullOrEmpty(currentModel.ModelId))
                        {
                            models.Add(currentModel);
                            _logger.LogDebug("Added model to list: {ModelId}, Status: {Status}", currentModel.ModelId, currentModel.Status);
                        }
                        else
                        {
                            _logger.LogWarning("Skipping model with empty Model ID");
                        }
                        currentModel = null;
                    }
                }
            }
            
            // If we have a model that wasn't closed properly, add it
            if (currentModel != null && !string.IsNullOrEmpty(currentModel.ModelId))
            {
                models.Add(currentModel);
                _logger.LogDebug("Added final model to list: {ModelId}", currentModel.ModelId);
            }
            
            _logger.LogInformation("Successfully parsed {Count} models from HTML response", models.Count);
              // If we didn't get any comprehensive models, log a warning
            if (models.Count == 0)
            {
                _logger.LogWarning("No models parsed from HTML response. The HTML structure may have changed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing HTML models response");
        }
        
        return models;
    }

    public async Task<ModelsResponse?> GetModelsAsync(int offset = 0, int limit = 100)
    {
        HttpResponseMessage? response = null;
        try
        {
            var queryParams = new List<string> { $"offset={offset}", $"limit={limit}" };
            var queryString = string.Join("&", queryParams);
            var url = $"{_baseUrl}/models?{queryString}";

            _logger.LogInformation("Fetching models from {Url}", url);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var accessToken = await _platformService.GetAccessTokenAsync();
            request.Headers.Add("A4SAuthorization", accessToken);
            request.Headers.Add("ROPC", "true");

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var modelsResponse = await response.Content.ReadFromJsonAsync(AppserverContext.Default.ModelsResponse);
            if (modelsResponse != null)
            {
                _logger.LogInformation("Successfully fetched {Count} models (total: {Total})", 
                    modelsResponse.Models.Count, modelsResponse.Header.Total);
            }
            return modelsResponse;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = response != null ? await response.Content.ReadAsStringAsync() : "No response content";
            _logger.LogError(ex, "Failed to fetch models from Appserver: {ErrorMessage}", errorResponse);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to Appserver for models timed out");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse models response from Appserver");
            return null;
        }
    }
}



public class AboutOutputView
{
    [JsonPropertyName("app_server_version")]
    public string AppServerVersion { get; set; } = string.Empty;

    [JsonPropertyName("models")]
    public List<ModelInfo> Models { get; set; } = new();
}

public class ModelInfo
{
    [JsonPropertyName("model_id")]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("modeldata_timestamp")]
    public long ModeldataTimestamp { get; set; }

    [JsonPropertyName("model_definition_version")]
    public int ModelDefinitionVersion { get; set; }

    [JsonPropertyName("is_real_time")]
    public bool IsRealTime { get; set; }
}

public class ComprehensiveModelInfo
{
    [JsonPropertyName("model_id")]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("short_name")]
    public string ShortName { get; set; } = string.Empty;

    [JsonPropertyName("long_name")]
    public string LongName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("environment")]
    public string Environment { get; set; } = string.Empty;

    [JsonPropertyName("license_type")]
    public string LicenseType { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public ModelCreatedInfo Created { get; set; } = new();

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("modeldata_timestamp")]
    public long ModeldataTimestamp { get; set; }

    [JsonPropertyName("model_definition_version")]
    public int ModelDefinitionVersion { get; set; }

    [JsonPropertyName("is_real_time")]
    public bool IsRealTime { get; set; }

    [JsonPropertyName("uris")]
    public ModelUris Uris { get; set; } = new();
}

public class ModelCreatedInfo
{
    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("datetime")]
    public long DateTime { get; set; }
}

public class ModelUris
{
    [JsonPropertyName("base_model")]
    public string BaseModel { get; set; } = string.Empty;

    [JsonPropertyName("base_classes")]
    public string BaseClasses { get; set; } = string.Empty;

    [JsonPropertyName("base_fields")]
    public string BaseFields { get; set; } = string.Empty;

    [JsonPropertyName("base_angles")]
    public string BaseAngles { get; set; } = string.Empty;

    [JsonPropertyName("base_languages")]
    public string BaseLanguages { get; set; } = string.Empty;
}

public class TaskExecutionRequest
{
    [JsonPropertyName("start")]
    public bool Start { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class TaskExecutionResponse
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class TaskStatusResponse
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class TasksListResponse
{
    [JsonPropertyName("header")]
    public TasksListHeader Header { get; set; } = new();

    [JsonPropertyName("tasks")]
    public List<TaskItemView> Tasks { get; set; } = new();
}

public class TasksListHeader
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}

public class TaskItemView : TaskItemBasicView
{
    [JsonPropertyName("changed")]
    public WhoWhenView Changed { get; set; } = new();

    [JsonPropertyName("delete_after_completion")]
    public bool DeleteAfterCompletion { get; set; }

    [JsonPropertyName("last_run_result")]
    public string LastRunResult { get; set; } = string.Empty;

    [JsonPropertyName("last_run_time")]
    public long? LastRunTime { get; set; }

    [JsonPropertyName("next_run_time")]
    public long? NextRunTime { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("history")]
    public string History { get; set; } = string.Empty;

    [JsonPropertyName("actions_uri")]
    public string ActionsUri { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public List<ArgumentView> Arguments { get; set; } = new();

    [JsonPropertyName("recipients")]
    public List<TaskNotificationRecipientItemView> Recipients { get; set; } = new();
}

public class TaskItemBasicView
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("triggers")]
    public List<TriggerView> Triggers { get; set; } = new();

    [JsonPropertyName("max_run_time")]
    public int MaxRunTime { get; set; }

    [JsonPropertyName("expected_run_time")]
    public int ExpectedRunTime { get; set; }

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("action_count")]
    public int ActionCount { get; set; }

    [JsonPropertyName("run_as_user")]
    public string? RunAsUser { get; set; }

    [JsonPropertyName("actions")]
    public List<ActionView> Actions { get; set; } = new();

    [JsonPropertyName("created")]
    public WhoWhenView Created { get; set; } = new();
}

public class TriggerView
{
    [JsonPropertyName("trigger_type")]
    public string TriggerType { get; set; } = string.Empty;

    // Common for event triggers
    [JsonPropertyName("arguments")]
    public List<ArgumentView>? Arguments { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }

    // Common for schedule triggers
    [JsonPropertyName("days")]
    public List<DayView>? Days { get; set; }

    [JsonPropertyName("continuous")]
    public bool? Continuous { get; set; }

    [JsonPropertyName("frequency")]
    public string? Frequency { get; set; }

    [JsonPropertyName("start_time")]
    public long? StartTime { get; set; }
}

public class DayView
{
    [JsonPropertyName("day")]
    public int Day { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }
}

public class ActionView
{
    [JsonPropertyName("action_type")]
    public string ActionType { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public List<ArgumentView> Arguments { get; set; } = new();
}

public class WhoWhenView
{
    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;

    [JsonPropertyName("datetime")]
    public long DateTime { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;
}

public class ArgumentView
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }
}

public class TaskNotificationRecipientItemView : NotificationRecipientItemView
{
    [JsonPropertyName("summary")]
    public bool Summary { get; set; }
}

public class NotificationRecipientItemView
{
    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public bool Result { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("failed")]
    public bool Failed { get; set; }
}

public class UsersListResponse
{
    [JsonPropertyName("header")]
    public UsersListHeader Header { get; set; } = new();

    [JsonPropertyName("users")]
    public List<UserView> Users { get; set; } = new();
}

public class UsersListHeader
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}

public class UserView
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("is_admin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("is_external")]
    public bool IsExternal { get; set; }

    [JsonPropertyName("created")]
    public WhoWhenView Created { get; set; } = new();

    [JsonPropertyName("changed")]
    public WhoWhenView Changed { get; set; } = new();

    [JsonPropertyName("last_login")]
    public long? LastLogin { get; set; }

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}

public class UserSettingsResponse
{
    [JsonPropertyName("default_language")]
    public string DefaultLanguage { get; set; } = string.Empty;

    [JsonPropertyName("default_currency")]
    public string DefaultCurrency { get; set; } = string.Empty;

    [JsonPropertyName("client_settings")]
    public string ClientSettings { get; set; } = string.Empty;

    [JsonPropertyName("default_export_lines")]
    public int DefaultExportLines { get; set; }

    [JsonPropertyName("sap_fields_in_chooser")]
    public bool SapFieldsInChooser { get; set; }

    [JsonPropertyName("sap_fields_in_header")]
    public bool SapFieldsInHeader { get; set; }

    [JsonPropertyName("manual_insert_column")]
    public bool ManualInsertColumn { get; set; }

    [JsonPropertyName("compressed_list_header")]
    public bool CompressedListHeader { get; set; }

    [JsonPropertyName("compressed_bp_bar")]
    public bool CompressedBpBar { get; set; }

    [JsonPropertyName("default_business_processes")]
    public List<string> DefaultBusinessProcesses { get; set; } = new();

    [JsonPropertyName("auto_execute_items_on_login")]
    public bool AutoExecuteItemsOnLogin { get; set; }

    [JsonPropertyName("format_locale")]
    public string FormatLocale { get; set; } = string.Empty;

    [JsonPropertyName("format_numbers")]
    public string FormatNumbers { get; set; } = string.Empty;

    [JsonPropertyName("format_currencies")]
    public string FormatCurrencies { get; set; } = string.Empty;

    [JsonPropertyName("format_percentages")]
    public string FormatPercentages { get; set; } = string.Empty;

    [JsonPropertyName("format_enum")]
    public string FormatEnum { get; set; } = string.Empty;

    [JsonPropertyName("format_date")]
    public string FormatDate { get; set; } = string.Empty;

    [JsonPropertyName("format_period")]
    public string FormatPeriod { get; set; } = string.Empty;

    [JsonPropertyName("format_time")]
    public string FormatTime { get; set; } = string.Empty;

    [JsonPropertyName("hide_other_users_private_display")]
    public bool HideOtherUsersPrivateDisplay { get; set; }
}

public class ModifyUserSettingsView
{
    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string default_language { get; set; } = string.Empty;

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string default_currency { get; set; } = string.Empty;

    [DataMember(EmitDefaultValue = false)]
    //[Required]      
    public string client_settings { get; set; } = string.Empty;

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public int? default_export_lines { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public bool? sap_fields_in_chooser { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public bool? sap_fields_in_header { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public bool? manual_insert_column { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public bool? compressed_list_header { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public bool? compressed_bp_bar { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public IList<string> default_business_processes { get; set; } = new List<string>();

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public bool? auto_execute_items_on_login { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public bool? auto_execute_last_search { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_locale { get; set; } = string.Empty;

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_numbers { get; set; } = string.Empty;

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_currencies { get; set; } = string.Empty;

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_percentages { get; set; } = string.Empty;

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_enum { get; set; } = string.Empty;

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_date { get; set; } = string.Empty;

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_period { get; set; } = string.Empty;

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_time { get; set; } = string.Empty;

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public bool? hide_other_users_private_display { get; set; }
}

public class UserListResponse
{
    [JsonPropertyName("header")]
    public UserListHeader Header { get; set; } = new();

    [JsonPropertyName("users")]
    public List<UserInfo> Users { get; set; } = new();

    [JsonPropertyName("sort_options")]
    public List<SortOption> SortOptions { get; set; } = new();
}

public class UserListHeader
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}

public class UserInfo
{
    [JsonPropertyName("access_to_models")]
    public List<string> AccessToModels { get; set; } = new();

    [JsonPropertyName("system_privileges")]
    public SystemPrivileges SystemPrivileges { get; set; } = new();

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("registered_on")]
    public long RegisteredOn { get; set; }

    [JsonPropertyName("last_logon")]
    public long? LastLogon { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("assigned_roles")]
    public List<AssignedRole>? AssignedRoles { get; set; }
}

public class SystemPrivileges
{
    [JsonPropertyName("has_management_access")]
    public bool HasManagementAccess { get; set; }

    [JsonPropertyName("manage_system")]
    public bool ManageSystem { get; set; }

    [JsonPropertyName("manage_users")]
    public bool ManageUsers { get; set; }

    [JsonPropertyName("allow_impersonation")]
    public bool AllowImpersonation { get; set; }

    [JsonPropertyName("schedule_angles")]
    public bool ScheduleAngles { get; set; }
}

public class AssignedRole
{
    [JsonPropertyName("role_id")]
    public string RoleId { get; set; } = string.Empty;

    [JsonPropertyName("model_id")]
    public string? ModelId { get; set; }
}

public class SortOption
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

// Models for the /models endpoint
public class ModelsResponse
{
    [JsonPropertyName("header")]
    public ModelsHeader Header { get; set; } = new();

    [JsonPropertyName("models")]
    public List<DetailedModel> Models { get; set; } = new();
}

public class ModelsHeader
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}

public class DetailedModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("short_name")]
    public string ShortName { get; set; } = string.Empty;

    [JsonPropertyName("long_name")]
    public string LongName { get; set; } = string.Empty;

    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; set; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("modelserver_settings")]
    public string ModelserverSettings { get; set; } = string.Empty;

    [JsonPropertyName("environment")]
    public string Environment { get; set; } = string.Empty;

    [JsonPropertyName("use_refresh")]
    public bool UseRefresh { get; set; }

    [JsonPropertyName("switch_when_postprocessing")]
    public bool SwitchWhenPostprocessing { get; set; }

    [JsonPropertyName("is_postprocessing")]
    public bool IsPostprocessing { get; set; }

    [JsonPropertyName("active_languages")]
    public List<string> ActiveLanguages { get; set; } = new();

    [JsonPropertyName("created")]
    public ModelCreatedInfo Created { get; set; } = new();

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("packages")]
    public string Packages { get; set; } = string.Empty;

    [JsonPropertyName("authorizations")]
    public ModelAuthorizations Authorizations { get; set; } = new();
}

public class ModelAuthorizations
{
    [JsonPropertyName("update")]
    public bool Update { get; set; }

    [JsonPropertyName("delete")]
    public bool Delete { get; set; }

    [JsonPropertyName("access_data")]
    public bool AccessData { get; set; }

    [JsonPropertyName("publish_dashboard")]
    public bool PublishDashboard { get; set; }

    [JsonPropertyName("create_angle")]
    public bool CreateAngle { get; set; }

    [JsonPropertyName("manage_settings")]
    public bool ManageSettings { get; set; }

    [JsonPropertyName("manage_roles")]
    public bool ManageRoles { get; set; }

    [JsonPropertyName("assign_roles")]
    public bool AssignRoles { get; set; }
}

[JsonSerializable(typeof(UserListResponse))]
[JsonSerializable(typeof(UserListHeader))]
[JsonSerializable(typeof(List<UserInfo>))]
[JsonSerializable(typeof(UserInfo))]
[JsonSerializable(typeof(SystemPrivileges))]
[JsonSerializable(typeof(List<AssignedRole>))]
[JsonSerializable(typeof(AssignedRole))]
[JsonSerializable(typeof(List<SortOption>))]
[JsonSerializable(typeof(SortOption))]
[JsonSerializable(typeof(UserSettingsResponse))]
[JsonSerializable(typeof(AboutOutputView))]
[JsonSerializable(typeof(BusinessProcessResponse))]
[JsonSerializable(typeof(BusinessProcessSortOption))]
[JsonSerializable(typeof(List<BusinessProcessSortOption>))]
[JsonSerializable(typeof(AngleSearchRequest))]
[JsonSerializable(typeof(AngleSearchResponse))]
[JsonSerializable(typeof(AngleResponseHeader))]
[JsonSerializable(typeof(AngleDocument))]
[JsonSerializable(typeof(AngleCreatedInfo))]
[JsonSerializable(typeof(AngleDisplay))]
[JsonSerializable(typeof(AngleUserSpecific))]
[JsonSerializable(typeof(List<AngleDocument>))]
[JsonSerializable(typeof(List<AngleDisplay>))]
[JsonSerializable(typeof(ExecuteAngleDisplayRequest))]
[JsonSerializable(typeof(ExecuteAngleDisplayResponse))]
[JsonSerializable(typeof(GetAngleDisplayExecutionStatusResponse))]
[JsonSerializable(typeof(DataRowsResponse))]
[JsonSerializable(typeof(DataRowsHeader))]
[JsonSerializable(typeof(DataRow))]
[JsonSerializable(typeof(List<DataRow>))]
[JsonSerializable(typeof(QueryDefinitionItem))]
[JsonSerializable(typeof(ExecutedInfo))]
[JsonSerializable(typeof(AngleAuthorizations))]
[JsonSerializable(typeof(List<QueryDefinitionItem>))]
[JsonSerializable(typeof(AngleFilterRequest))]
[JsonSerializable(typeof(AngleStatisticsResponse))]
[JsonSerializable(typeof(FacetCategory))]
[JsonSerializable(typeof(FacetFilter))]
[JsonSerializable(typeof(FacetCounts))]
[JsonSerializable(typeof(AngleSortOption))]
[JsonSerializable(typeof(List<FacetCategory>))]
[JsonSerializable(typeof(List<FacetFilter>))]
[JsonSerializable(typeof(List<AngleSortOption>))]
[JsonSerializable(typeof(List<ModelInfo>))]
[JsonSerializable(typeof(ModelInfo))]
[JsonSerializable(typeof(ComprehensiveModelInfo))]
[JsonSerializable(typeof(List<ComprehensiveModelInfo>))]
[JsonSerializable(typeof(ModelCreatedInfo))]
[JsonSerializable(typeof(ModelUris))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, int>))]
[JsonSerializable(typeof(Dictionary<string, List<object>>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(TaskExecutionRequest))]
[JsonSerializable(typeof(TaskExecutionResponse))]
[JsonSerializable(typeof(TaskStatusResponse))]
[JsonSerializable(typeof(List<TaskItemView>))]
[JsonSerializable(typeof(TaskItemView))]
[JsonSerializable(typeof(TaskItemBasicView))]
[JsonSerializable(typeof(WhoWhenView))]
[JsonSerializable(typeof(ArgumentView))]
[JsonSerializable(typeof(TaskNotificationRecipientItemView))]
[JsonSerializable(typeof(NotificationRecipientItemView))]
[JsonSerializable(typeof(TasksListResponse))]
[JsonSerializable(typeof(TasksListHeader))]
[JsonSerializable(typeof(TriggerView))]
[JsonSerializable(typeof(ActionView))]
[JsonSerializable(typeof(List<TriggerView>))]
[JsonSerializable(typeof(List<ActionView>))]
[JsonSerializable(typeof(DayView))]
[JsonSerializable(typeof(List<DayView>))]
[JsonSerializable(typeof(List<UserView>))]
[JsonSerializable(typeof(UserView))]
[JsonSerializable(typeof(UsersListResponse))]
[JsonSerializable(typeof(UsersListHeader))]
[JsonSerializable(typeof(DashboardResponse))]
[JsonSerializable(typeof(MultiLangText))]
[JsonSerializable(typeof(DashboardUserSpecific))]
[JsonSerializable(typeof(WidgetDefinition))]
[JsonSerializable(typeof(DashboardAuthorizations))]
[JsonSerializable(typeof(List<MultiLangText>))]
[JsonSerializable(typeof(List<WidgetDefinition>))]
[JsonSerializable(typeof(DataFieldsResponse))]
[JsonSerializable(typeof(DataFieldsHeader))]
[JsonSerializable(typeof(DataField))]
[JsonSerializable(typeof(List<DataField>))]
[JsonSerializable(typeof(ModelClassesResponse))]
[JsonSerializable(typeof(ModelClass))]
[JsonSerializable(typeof(List<ModelClass>))]
[JsonSerializable(typeof(ModelsResponse))]
[JsonSerializable(typeof(ModelsHeader))]
[JsonSerializable(typeof(DetailedModel))]
[JsonSerializable(typeof(List<DetailedModel>))]
[JsonSerializable(typeof(ModelAuthorizations))]
[JsonSerializable(typeof(List<string>))]
internal sealed partial class AppserverContext : JsonSerializerContext
{
}