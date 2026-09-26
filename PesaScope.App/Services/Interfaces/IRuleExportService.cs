using PesaScope.Core.Models;

namespace PesaScope.App.Services.Interfaces;

public interface IRuleExportService
{
    /// <summary>Serializes the given rules to a JSON export string.</summary>
    Task<string> ExportAsync(IEnumerable<(AutoCategorizationRule Rule, string CategoryName)> rules);

    /// <summary>
    /// Parses an export file's JSON against the current DB state.
    /// Performs no writes — classifies each incoming rule as New/Duplicate/Modified/CategoryNotFound.
    /// </summary>
    Task<RuleImportPreview> ParseAsync(string json);

    /// <summary>
    /// Commits the selected items from a previously-parsed preview.
    /// Inserts New, updates Modified, ignores everything else (or anything deselected).
    /// </summary>
    Task<RuleImportResult> CommitAsync(RuleImportPreview preview, IEnumerable<RuleImportPreviewItem> acceptedItems);
}