using System.Text.Json;
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

    // Models for ExecuteAngleDisplay functionality
    public class ExecuteAngleDisplayRequest
    {
        [JsonPropertyName("query_definition")]
        public List<QueryDefinitionItem> QueryDefinition { get; set; } = new();
    }

    public class QueryDefinitionItem
    {
        [JsonPropertyName("base_angle")]
        public string? BaseAngle { get; set; }

        [JsonPropertyName("base_display")]
        public string? BaseDisplay { get; set; }

        [JsonPropertyName("queryblock_type")]
        public string QueryBlockType { get; set; } = string.Empty;
    }

    public class ExecuteAngleDisplayResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("instance")]
        public string Instance { get; set; } = string.Empty;

        [JsonPropertyName("search")]
        public string Search { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("progress")]
        public double Progress { get; set; }

        [JsonPropertyName("queryable")]
        public bool Queryable { get; set; }

        [JsonPropertyName("row_count")]
        public int RowCount { get; set; }

        [JsonPropertyName("object_count")]
        public int ObjectCount { get; set; }

        [JsonPropertyName("executed")]
        public ExecutedInfo Executed { get; set; } = new();

        [JsonPropertyName("query_definition")]
        public string QueryDefinition { get; set; } = string.Empty;

        [JsonPropertyName("data_fields")]
        public string DataFields { get; set; } = string.Empty;

        [JsonPropertyName("query_fields")]
        public string QueryFields { get; set; } = string.Empty;

        [JsonPropertyName("followups")]
        public string Followups { get; set; } = string.Empty;

        [JsonPropertyName("data_rows")]
        public string DataRows { get; set; } = string.Empty;

        [JsonPropertyName("execute_steps")]
        public string ExecuteSteps { get; set; } = string.Empty;

        [JsonPropertyName("sap_transactions")]
        public string SapTransactions { get; set; } = string.Empty;

        [JsonPropertyName("actual_classes")]
        public List<string> ActualClasses { get; set; } = new();

        [JsonPropertyName("potential_classes")]
        public List<string> PotentialClasses { get; set; } = new();

        [JsonPropertyName("default_fields")]
        public List<string> DefaultFields { get; set; } = new();

        [JsonPropertyName("modeldata_timestamp")]
        public long ModeldataTimestamp { get; set; }

        [JsonPropertyName("original_modeldata_timestamp")]
        public string OriginalModeldataTimestamp { get; set; } = string.Empty;

        [JsonPropertyName("authorizations")]
        public AngleAuthorizations Authorizations { get; set; } = new();
    }

    public class ExecutedInfo
    {
        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("datetime")]
        public long DateTime { get; set; }
    }

    public class AngleAuthorizations
    {
        [JsonPropertyName("change_query_filters")]
        public bool ChangeQueryFilters { get; set; }

        [JsonPropertyName("change_query_followups")]
        public bool ChangeQueryFollowups { get; set; }

        [JsonPropertyName("single_item_view")]
        public bool SingleItemView { get; set; }

        [JsonPropertyName("change_field_collection")]
        public bool ChangeFieldCollection { get; set; }

        [JsonPropertyName("sort")]
        public bool Sort { get; set; }

        [JsonPropertyName("add_filter")]
        public bool AddFilter { get; set; }

        [JsonPropertyName("add_followup")]
        public bool AddFollowup { get; set; }

        [JsonPropertyName("add_aggregation")]
        public bool AddAggregation { get; set; }

        [JsonPropertyName("export")]
        public bool Export { get; set; }
    }

    public class GetAngleDisplayExecutionStatusResponse : ExecuteAngleDisplayResponse
    {
        [JsonPropertyName("execution_time")]
        public int ExecutionTime { get; set; }
    }

    // Models for DataRows functionality
    public class DataRowsResponse
    {
        [JsonPropertyName("header")]
        public DataRowsHeader Header { get; set; } = new();

        [JsonPropertyName("fields")]
        public List<string> Fields { get; set; } = new();

        [JsonPropertyName("rows")]
        public List<DataRow> Rows { get; set; } = new();

        [JsonPropertyName("execution_time")]
        public double? ExecutionTime { get; set; }
    }

    public class DataRowsHeader
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class DataRow
    {
        [JsonPropertyName("row_id")]
        public string RowId { get; set; } = string.Empty;

        [JsonPropertyName("field_values")]
        public List<JsonElement> FieldValues { get; set; } = new();    }

    // Model Classes Response Models
    public class ModelClassesResponse
    {
        [JsonPropertyName("header")]
        public AngleResponseHeader Header { get; set; } = new();

        [JsonPropertyName("classes")]
        public List<ModelClass> Classes { get; set; } = new();

        [JsonPropertyName("facets")]
        public List<FacetCategory> Facets { get; set; } = new();

        [JsonPropertyName("sort_options")]
        public List<AngleSortOption> SortOptions { get; set; } = new();
    }

    public class ModelClass
    {
        [JsonPropertyName("short_name")]
        public string ShortName { get; set; } = string.Empty;

        [JsonPropertyName("long_name")]
        public string LongName { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("main_businessprocess")]
        public string MainBusinessprocess { get; set; } = string.Empty;

        [JsonPropertyName("helpid")]
        public string HelpId { get; set; } = string.Empty;

        [JsonPropertyName("helptext")]
        public string HelpText { get; set; } = string.Empty;
    }

    // Models for Dashboard functionality
    public class DashboardResponse
    {
        [JsonPropertyName("multi_lang_name")]
        public List<MultiLangText> MultiLangName { get; set; } = new();

        [JsonPropertyName("multi_lang_description")]
        public List<MultiLangText> MultiLangDescription { get; set; } = new();

        [JsonPropertyName("changed")]
        public AngleCreatedInfo Changed { get; set; } = new();

        [JsonPropertyName("executed")]
        public AngleCreatedInfo Executed { get; set; } = new();

        [JsonPropertyName("user_specific")]
        public DashboardUserSpecific UserSpecific { get; set; } = new();

        [JsonPropertyName("widget_definitions")]
        public List<WidgetDefinition> WidgetDefinitions { get; set; } = new();

        [JsonPropertyName("filters")]
        public List<JsonElement> Filters { get; set; } = new();

        [JsonPropertyName("widgets")]
        public string Widgets { get; set; } = string.Empty;

        [JsonPropertyName("layout")]
        public string Layout { get; set; } = string.Empty;

        [JsonPropertyName("assigned_labels")]
        public List<string> AssignedLabels { get; set; } = new();

        [JsonPropertyName("assigned_tags")]
        public List<string> AssignedTags { get; set; } = new();

        [JsonPropertyName("privilege_labels")]
        public string PrivilegeLabels { get; set; } = string.Empty;

        [JsonPropertyName("grouping_labels")]
        public string GroupingLabels { get; set; } = string.Empty;

        [JsonPropertyName("labels")]
        public string Labels { get; set; } = string.Empty;

        [JsonPropertyName("business_processes")]
        public string BusinessProcesses { get; set; } = string.Empty;

        [JsonPropertyName("angles")]
        public string Angles { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("is_validated")]
        public bool IsValidated { get; set; }

        [JsonPropertyName("is_published")]
        public bool IsPublished { get; set; }

        [JsonPropertyName("created")]
        public AngleCreatedInfo Created { get; set; } = new();

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("has_warnings")]
        public bool HasWarnings { get; set; }

        [JsonPropertyName("authorizations")]
        public DashboardAuthorizations Authorizations { get; set; } = new();

        [JsonPropertyName("is_parameterized")]
        public bool IsParameterized { get; set; }
    }

    public class MultiLangText
    {
        [JsonPropertyName("lang")]
        public string Lang { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class DashboardUserSpecific
    {
        [JsonPropertyName("execute_on_login")]
        public bool ExecuteOnLogin { get; set; }

        [JsonPropertyName("is_starred")]
        public bool IsStarred { get; set; }
    }

    public class WidgetDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("widget_details")]
        public string WidgetDetails { get; set; } = string.Empty;

        [JsonPropertyName("widget_type")]
        public string WidgetType { get; set; } = string.Empty;

        [JsonPropertyName("angle")]
        public string Angle { get; set; } = string.Empty;

        [JsonPropertyName("display")]
        public string Display { get; set; } = string.Empty;

        [JsonPropertyName("multi_lang_name")]
        public List<MultiLangText> MultiLangName { get; set; } = new();

        [JsonPropertyName("multi_lang_description")]
        public List<MultiLangText> MultiLangDescription { get; set; } = new();
    }

    public class DashboardAuthorizations
    {
        [JsonPropertyName("update")]
        public bool Update { get; set; }

        [JsonPropertyName("delete")]
        public bool Delete { get; set; }

        [JsonPropertyName("publish")]
        public bool Publish { get; set; }

        [JsonPropertyName("unpublish")]
        public bool Unpublish { get; set; }

        [JsonPropertyName("validate")]
        public bool Validate { get; set; }

        [JsonPropertyName("unvalidate")]
        public bool Unvalidate { get; set; }
    }

    // Models for Data Fields functionality
    public class DataFieldsResponse
    {
        [JsonPropertyName("header")]
        public DataFieldsHeader Header { get; set; } = new();

        [JsonPropertyName("fields")]
        public List<DataField> Fields { get; set; } = new();
    }

    public class DataFieldsHeader
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }
    }

    public class DataField
    {
        [JsonPropertyName("short_name")]
        public string ShortName { get; set; } = string.Empty;

        [JsonPropertyName("long_name")]
        public string LongName { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("fieldtype")]
        public string FieldType { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("domain")]
        public string? Domain { get; set; }
    }
}
