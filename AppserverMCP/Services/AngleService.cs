using AppserverMCP.Interfaces;
using AppserverMCP.Models;
using System.Text.Json;
using System.Text;
using System.Web;

namespace AppserverMCP.Services
{
    public class AngleService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AngleService> _logger;
        private readonly IPlatformService _platformService;
        private readonly string _baseUrl;

        public AngleService(IHttpClientFactory httpClientFactory, ILogger<AngleService> logger, IConfiguration configuration, IPlatformService platformService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _baseUrl = configuration.GetValue<string>("AppserverBaseUrl") ?? "http://localhost:8080";
            _platformService = platformService;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<AngleSearchResponse?> SearchAnglesAsync(AngleSearchRequest searchRequest)
        {
            HttpResponseMessage? response = null;
            try
            {
                var queryParams = BuildQueryParameters(searchRequest);
                var url = $"{_baseUrl}/items?{queryParams}";

                _logger.LogInformation("Searching items with URL: {Url}", url);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                var accessToken = await _platformService.GetAccessTokenAsync();
                request.Headers.Add("A4SAuthorization", accessToken);
                request.Headers.Add("ROPC", "true"); response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Log the raw JSON response for debugging
                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Raw API Response: {JsonContent}", jsonContent);

                var searchResponse = await response.Content.ReadFromJsonAsync<AngleSearchResponse>(); if (searchResponse != null)
                {
                    _logger.LogInformation("Successfully searched items. Found {Count} items", searchResponse.Header.Total);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize JSON response to ItemSearchResponse");
                }

                return searchResponse;
            }
            catch (HttpRequestException ex)
            {
                var errorResponse = response?.Content != null ? await response.Content.ReadAsStringAsync() : "No response content";
                _logger.LogError(ex, "Failed to search items from Appserver: {ErrorResponse}", errorResponse);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request to search items timed out");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse item search response from Appserver");
                return null;
            }
        }

        public async Task<AngleDocument?> GetAngleByIdAsync(string angleId)
        {
            HttpResponseMessage? response = null; try
            {
                var url = $"{_baseUrl}/api/items/{angleId}";

                _logger.LogInformation("Getting angle by ID: {AngleId}", angleId);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                var accessToken = await _platformService.GetAccessTokenAsync();
                request.Headers.Add("A4SAuthorization", accessToken);
                request.Headers.Add("ROPC", "true");

                response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var angle = await response.Content.ReadFromJsonAsync<AngleDocument>();

                if (angle != null)
                {
                    _logger.LogInformation("Successfully retrieved angle: {AngleId}", angleId);
                }

                return angle;
            }
            catch (HttpRequestException ex)
            {
                var errorResponse = response?.Content != null ? await response.Content.ReadAsStringAsync() : "No response content";
                _logger.LogError(ex, "Failed to get angle {AngleId} from Appserver: {ErrorResponse}", angleId, errorResponse);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request to get angle {AngleId} timed out", angleId);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse angle response from Appserver for angle {AngleId}", angleId);
                return null;
            }
        }

        public async Task<AngleSearchResponse?> FilterAnglesAsync(List<AngleFilterRequest> filters, int start = 0, int rows = 10, string sort = "")
        {
            try
            {
                var searchRequest = new AngleSearchRequest
                {
                    Query = "*:*",
                    FilterQueries = BuildFilterQueries(filters),
                    Start = start,
                    Rows = rows,
                    Sort = sort
                };

                return await SearchAnglesAsync(searchRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to filter items");
                return null;
            }
        }

        public async Task<AngleStatisticsResponse?> GetAngleStatisticsAsync()
        {
            try
            {                // Get overall statistics by searching with facets
                var searchRequest = new AngleSearchRequest
                {
                    Query = "*:*",
                    Rows = 0, // We only want facet data, not documents
                    Facet = true,
                    FacetFields = new List<string> { "category", "status" }
                };

                var searchResponse = await SearchAnglesAsync(searchRequest);
                if (searchResponse?.FacetCounts == null) return null; var statistics = new AngleStatisticsResponse
                {
                    TotalAngles = searchResponse.Header.Total,
                    LastUpdated = DateTime.UtcNow
                };

                // Process category facets
                if (searchResponse.FacetCounts.FacetFields.ContainsKey("category"))
                {
                    var categoryFacets = searchResponse.FacetCounts.FacetFields["category"];
                    statistics.Categories = ProcessFacetList(categoryFacets);
                }

                // Process status facets
                if (searchResponse.FacetCounts.FacetFields.ContainsKey("status"))
                {
                    var statusFacets = searchResponse.FacetCounts.FacetFields["status"];
                    statistics.StatusDistribution = ProcessFacetList(statusFacets);
                }                // Get recent items count (last 30 days)
                var recentFilter = new List<AngleFilterRequest>
                {
                    new AngleFilterRequest
                    {
                        Field = "created_date",
                        Operator = "range",
                        From = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        To = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    }
                }; var recentItemsResponse = await FilterAnglesAsync(recentFilter, 0, 0);
                statistics.RecentAngles = (int)(recentItemsResponse?.Header.Total ?? 0);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get item statistics");
                return null;
            }
        }
        private string BuildQueryParameters(AngleSearchRequest searchRequest)
        {
            var parameters = new List<string>
            {
                $"q={HttpUtility.UrlEncode(searchRequest.Query)}",
                "caching=false",
                "viewmode=basic"
            };

            // Only add start and rows if they are not default values
            if (searchRequest.Start > 0)
                parameters.Add($"start={searchRequest.Start}"); if (searchRequest.Rows != 10) // Don't add if it's the default value
                parameters.Add($"rows={searchRequest.Rows}");

            // Only add fields parameter if it's not the default "*" value
            if (!string.IsNullOrEmpty(searchRequest.Fields) && searchRequest.Fields != "*")
                parameters.Add($"fl={HttpUtility.UrlEncode(searchRequest.Fields)}");

            if (!string.IsNullOrEmpty(searchRequest.Sort))
                parameters.Add($"sort={HttpUtility.UrlEncode(searchRequest.Sort)}");

            foreach (var fq in searchRequest.FilterQueries)
            {
                parameters.Add($"fq={HttpUtility.UrlEncode(fq)}");
            }

            if (searchRequest.Facet)
            {
                parameters.Add("facet=true");
                foreach (var facetField in searchRequest.FacetFields)
                {
                    parameters.Add($"facet.field={HttpUtility.UrlEncode(facetField)}");
                }
            }

            if (searchRequest.Highlight)
            {
                parameters.Add("highlight=true");
                if (!string.IsNullOrEmpty(searchRequest.HighlightFields))
                    parameters.Add($"hl.fl={HttpUtility.UrlEncode(searchRequest.HighlightFields)}");
            }

            return string.Join("&", parameters);
        }

        private List<string> BuildFilterQueries(List<AngleFilterRequest> filters)
        {
            var filterQueries = new List<string>();

            foreach (var filter in filters)
            {
                string filterQuery = filter.Operator.ToLower() switch
                {
                    "equals" => $"{filter.Field}:\"{filter.Value}\"",
                    "contains" => $"{filter.Field}:*{filter.Value}*",
                    "startswith" => $"{filter.Field}:{filter.Value}*",
                    "endswith" => $"{filter.Field}:*{filter.Value}",
                    "range" => $"{filter.Field}:[{filter.From ?? "*"} TO {filter.To ?? "*"}]",
                    "not" => $"-{filter.Field}:\"{filter.Value}\"",
                    _ => $"{filter.Field}:\"{filter.Value}\""
                };

                filterQueries.Add(filterQuery);
            }

            return filterQueries;
        }

        private Dictionary<string, int> ProcessFacetList(List<object> facetList)
        {
            var result = new Dictionary<string, int>();

            for (int i = 0; i < facetList.Count; i += 2)
            {
                if (i + 1 < facetList.Count)
                {
                    var key = facetList[i].ToString() ?? "";
                    if (int.TryParse(facetList[i + 1].ToString(), out int count))
                    {
                        result[key] = count;
                    }
                }
            }

            return result;
        }
    }
}
