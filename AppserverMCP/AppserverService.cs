using AppserverMCP.Interfaces;
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
            var errorResponse = await response.Content.ReadAsStringAsync();
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
[JsonSerializable(typeof(List<ModelInfo>))]
[JsonSerializable(typeof(ModelInfo))]
internal sealed partial class AppserverContext : JsonSerializerContext
{
}