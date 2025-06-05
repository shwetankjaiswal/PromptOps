using AppserverMCP.Interfaces;
using AppserverMCP.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppserverMCP;

public class AppserverService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AppserverService> _logger;
    private readonly IPlatformService _platformService;
    private readonly string _baseUrl;

    public AppserverService(IHttpClientFactory httpClientFactory, ILogger<AppserverService> logger, IConfiguration configuration, IPlatformService platformService)
    {
        _httpClient = httpClientFactory.CreateClient();
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
            var errorResponse = response?.Content.ReadAsStringAsync().Result ?? "No response content";
            _logger.LogError(ex, $"Failed to fetch business processes from Appserver: {errorResponse}");
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

[JsonSerializable(typeof(AboutOutputView))]
[JsonSerializable(typeof(BusinessProcessResponse))]
[JsonSerializable(typeof(AngleSearchRequest))]
[JsonSerializable(typeof(AngleSearchResponse))]
[JsonSerializable(typeof(AngleResponseHeader))]
[JsonSerializable(typeof(AngleDocument))]
[JsonSerializable(typeof(AngleFilterRequest))]
[JsonSerializable(typeof(AngleStatisticsResponse))]
[JsonSerializable(typeof(FacetCategory))]
[JsonSerializable(typeof(FacetFilter))]
[JsonSerializable(typeof(AngleSortOption))]
[JsonSerializable(typeof(SortOption))]
[JsonSerializable(typeof(List<FacetCategory>))]
[JsonSerializable(typeof(List<FacetFilter>))]
[JsonSerializable(typeof(List<AngleSortOption>))]
[JsonSerializable(typeof(List<ModelInfo>))]
[JsonSerializable(typeof(ModelInfo))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(object))]
internal sealed partial class AppserverContext : JsonSerializerContext
{
}