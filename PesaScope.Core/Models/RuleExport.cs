using System.Text.Json.Serialization;

namespace PesaScope.Core.Models;

/// <summary>
/// Top-level envelope written to / read from a rules export file.
/// CategoryId is intentionally omitted — categories are matched by name
/// at import time since IDs aren't stable across devices/reseeds.
/// </summary>
public class RuleExportEnvelope
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("exportedAtUtc")]
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("appVersion")]
    public string AppVersion { get; set; } = string.Empty;

    [JsonPropertyName("rules")]
    public List<RuleExportDto> Rules { get; set; } = [];
}

/// <summary>
/// A single rule as it appears in an export file.
/// </summary>
public class RuleExportDto
{
    [JsonPropertyName("ruleType")]
    public RuleType RuleType { get; set; }

    [JsonPropertyName("matchValue")]
    public string MatchValue { get; set; } = string.Empty;

    /// <summary>
    /// Category resolved by name (case-insensitive) at import time.
    /// </summary>
    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;
}