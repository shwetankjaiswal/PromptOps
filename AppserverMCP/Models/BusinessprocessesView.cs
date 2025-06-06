using System.Text.Json.Serialization;

namespace AppserverMCP.Models
{

    public class BusinessProcessResponse
    {
        [JsonPropertyName("header")]
        public Header Header { get; set; } = new();

        [JsonPropertyName("business_processes")]
        public List<BusinessProcess> BusinessProcesses { get; set; } = new();        [JsonPropertyName("sort_options")]
        public List<BusinessProcessSortOption> SortOptions { get; set; } = new();
    }

    public class Header
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }
    }

    public class BusinessProcess
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("abbreviation")]
        public string Abbreviation { get; set; } = string.Empty;

        [JsonPropertyName("system")]
        public bool System { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("is_allowed")]
        public bool IsAllowed { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;    }

    public class BusinessProcessSortOption
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}
