using System.Text.Json.Serialization;

namespace AppserverMCP.Models
{
    public class AngleSearchRequest
    {
        [JsonPropertyName("q")]
        public string Query { get; set; } = "*:*";

        [JsonPropertyName("fq")]
        public List<string> FilterQueries { get; set; } = new();

        [JsonPropertyName("sort")]
        public string Sort { get; set; } = "";

        [JsonPropertyName("start")]
        public int Start { get; set; } = 0;

        [JsonPropertyName("rows")]
        public int Rows { get; set; } = 10;

        [JsonPropertyName("fl")]
        public string Fields { get; set; } = "*";

        [JsonPropertyName("facet")]
        public bool Facet { get; set; } = false;

        [JsonPropertyName("facet.field")]
        public List<string> FacetFields { get; set; } = new();

        [JsonPropertyName("highlight")]
        public bool Highlight { get; set; } = false;

        [JsonPropertyName("hl.fl")]
        public string HighlightFields { get; set; } = "";
    }
    public class AngleSearchResponse
    {
        [JsonPropertyName("header")]
        public AngleResponseHeader Header { get; set; } = new();

        [JsonPropertyName("items")]
        public List<AngleDocument> Items { get; set; } = new();

        [JsonPropertyName("facets")]
        public List<FacetCategory> Facets { get; set; } = new(); [JsonPropertyName("sort_options")]
        public List<AngleSortOption> SortOptions { get; set; } = new();

        [JsonPropertyName("advanced_filters")]
        public List<string> AdvancedFilters { get; set; } = new();

        [JsonPropertyName("facet_counts")]
        public FacetCounts? FacetCounts { get; set; }

        [JsonPropertyName("highlighting")]
        public Dictionary<string, object>? Highlighting { get; set; }
    }
    public class AngleResponseHeader
    {
        [JsonPropertyName("total")]
        public long Total { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }
    }

    public class AngleDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("created_date")]
        public DateTime? CreatedDate { get; set; }

        [JsonPropertyName("modified_date")]
        public DateTime? ModifiedDate { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        // Add more fields as needed based on your Solr schema
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalProperties { get; set; } = new();
    }

    public class FacetCounts
    {
        [JsonPropertyName("facet_queries")]
        public Dictionary<string, int> FacetQueries { get; set; } = new();

        [JsonPropertyName("facet_fields")]
        public Dictionary<string, List<object>> FacetFields { get; set; } = new();

        [JsonPropertyName("facet_ranges")]
        public Dictionary<string, object> FacetRanges { get; set; } = new();

        [JsonPropertyName("facet_intervals")]
        public Dictionary<string, object> FacetIntervals { get; set; } = new();

        [JsonPropertyName("facet_heatmaps")]
        public Dictionary<string, object> FacetHeatmaps { get; set; } = new();
    }
    public class AngleFilterRequest
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("operator")]
        public string Operator { get; set; } = "equals"; // equals, contains, startsWith, endsWith, range

        [JsonPropertyName("from")]
        public string? From { get; set; }

        [JsonPropertyName("to")]
        public string? To { get; set; }
    }

    public class AngleStatisticsResponse
    {
        [JsonPropertyName("total_angles")]
        public long TotalAngles { get; set; }

        [JsonPropertyName("categories")]
        public Dictionary<string, int> Categories { get; set; } = new();

        [JsonPropertyName("status_distribution")]
        public Dictionary<string, int> StatusDistribution { get; set; } = new(); [JsonPropertyName("recent_angles")]
        public int RecentAngles { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTime LastUpdated { get; set; }
    }

    // Additional models for the complete API response structure
    public class FacetCategory
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("filters")]
        public List<FacetFilter> Filters { get; set; } = new();
    }

    public class FacetFilter
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int? Count { get; set; }
    }
    public class AngleSortOption
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("is_selected")]
        public bool IsSelected { get; set; }
    }
}
