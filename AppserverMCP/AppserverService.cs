using AppserverMCP.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
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
            return new List<(string, string)>(); ;
        }
        catch (HttpRequestException ex)
        {
            var errorResponse = await response.Content.ReadAsStringAsync();
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
            var currentSettings = await GetUserSettings(uri);
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

    public async Task<UserSettingsResponse?> GetUserSettings(string uri)
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

public class ModifyUserSettingsView
{
    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string default_language { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string default_currency { get; set; }

    [DataMember(EmitDefaultValue = false)]
    //[Required]      
    public string client_settings { get; set; }

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
    public IList<string> default_business_processes { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public bool? auto_execute_items_on_login { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public bool? auto_execute_last_search { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_locale { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_numbers { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_currencies { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_percentages { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_enum { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_date { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_period { get; set; }

    [DataMember(EmitDefaultValue = false)]
    [Required]
    public string format_time { get; set; }

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
[JsonSerializable(typeof(List<ModelInfo>))]
[JsonSerializable(typeof(ModelInfo))]
internal sealed partial class AppserverContext : JsonSerializerContext
{
}