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
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("is_validated")]
        public bool IsValidated { get; set; }

        [JsonPropertyName("is_parameterized")]
        public bool IsParameterized { get; set; }

        [JsonPropertyName("is_published")]
        public bool IsPublished { get; set; }

        [JsonPropertyName("is_template")]
        public bool IsTemplate { get; set; }

        [JsonPropertyName("has_warnings")]
        public bool HasWarnings { get; set; }

        [JsonPropertyName("created")]
        public AngleCreatedInfo Created { get; set; } = new();

        [JsonPropertyName("lastExecutedOn")]
        public string LastExecutedOn { get; set; } = string.Empty;

        [JsonPropertyName("displays")]
        public List<AngleDisplay> Displays { get; set; } = new();

        [JsonPropertyName("user_specific")]
        public AngleUserSpecific UserSpecific { get; set; } = new();

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();
    }

    public class AngleCreatedInfo
    {
        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("datetime")]
        public long DateTime { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;
    }

    public class AngleDisplay
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("display_type")]
        public string DisplayType { get; set; } = string.Empty;

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("created_on")]
        public string CreatedOn { get; set; } = string.Empty;

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("is_angle_default")]
        public bool IsAngleDefault { get; set; }

        [JsonPropertyName("has_warnings")]
        public bool HasWarnings { get; set; }

        [JsonPropertyName("has_filters")]
        public bool HasFilters { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("has_followups")]
        public bool HasFollowups { get; set; }

        [JsonPropertyName("is_parameterized")]
        public bool IsParameterized { get; set; }

        [JsonPropertyName("used_in_task")]
        public bool UsedInTask { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    public class AngleUserSpecific
    {
        [JsonPropertyName("is_starred")]
        public bool IsStarred { get; set; }
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

        [JsonPropertyName("dir")]
        public string? Dir { get; set; }
    }
}
