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

    public async Task<UserSettingsResponse?> GetUserSettings()
    {
        HttpResponseMessage? response = null;
        try
        {
            _logger.LogInformation("Fetching User Settings from {BaseUrl}/users/1/settings", _baseUrl);
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users/{1}/settings");
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
          
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = await response.Content.ReadAsStringAsync();
            _logger.LogError(ex, $"Failed to fetch user settings from Appserver: {errorResponse}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to Appserver for user settings timed out");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse user settings response from Appserver");
            return null;
        }
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

[JsonSerializable(typeof(UserSettingsResponse))]
[JsonSerializable(typeof(AboutOutputView))]
[JsonSerializable(typeof(List<ModelInfo>))]
[JsonSerializable(typeof(ModelInfo))]
internal sealed partial class AppserverContext : JsonSerializerContext
{
}