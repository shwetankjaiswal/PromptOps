using AppserverMCP.Interfaces;
using System.Text.Json;

namespace AppserverMCP.Utils;

public class PlatformService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IPlatformService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public async Task<string> GetAccessTokenAsync()
    {
        var url = configuration["PlatformUrl"];
        var formData = new Dictionary<string, string>
        {
            ["username"] = "eaadmlocal@gmail.com",
            ["password"] = "pjVKITs3!Nll",
            ["grant_type"] = "password",
            ["scope"] = "openid offline_access 7c471a28-159f-472c-bc0a-f7bfb0b60e9f",
            ["client_id"] = "7c471a28-159f-472c-bc0a-f7bfb0b60e9f",
            ["response_type"] = "token id_token"
        };

        using var content = new FormUrlEncodedContent(formData);
        var response = await _httpClient.PostAsync(url, content);

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

        return tokenResponse == null
            ? throw new InvalidOperationException("Failed to deserialize token response.")
            : tokenResponse.access_token;
    }
}

public class TokenResponse
{
    /// <summary>
    /// access_token
    /// </summary>
    public required string access_token { get; set; }

    /// <summary>
    /// token_type
    /// </summary>
    public required string token_type { get; set; }

    /// <summary>
    /// expires_in
    /// </summary>
    public required string expires_in { get; set; }

    /// <summary>
    /// refresh_token
    /// </summary>
    public required string refresh_token { get; set; }

    /// <summary>
    /// id_token
    /// </summary>
    public required string id_token { get; set; }
}
