namespace PesaScope.Core.Models;

public enum RuleImportStatus
{
    /// <summary>No existing rule with this (RuleType, MatchValue). Selectable, default checked.</summary>
    New,

    /// <summary>Same (RuleType, MatchValue) exists with identical Category/Priority/IsEnabled. Not selectable.</summary>
    Duplicate,

    /// <summary>Same (RuleType, MatchValue) exists but Category/Priority/IsEnabled differs — the "tampered" case.
    /// Selectable, default checked; committing overwrites the existing rule with the imported values.</summary>
    Modified,

    /// <summary>CategoryName in the import file doesn't match any existing category. Not selectable.</summary>
    CategoryNotFound
}

/// <summary>
/// One row in the import preview list — pairs an incoming DTO with whatever
/// currently exists in the DB (if anything) so the UI can render a diff.
/// </summary>
public class RuleImportPreviewItem
{
    public required RuleExportDto Incoming { get; set; }

    /// <summary>The current DB row this collides with, if Status is Duplicate or Modified.</summary>
    public AutoCategorizationRule? Existing { get; set; }

    public RuleImportStatus Status { get; set; }

    /// <summary>Resolved category id for Incoming.CategoryName, if it exists.</summary>
    public int? ResolvedCategoryId { get; set; }

    /// <summary>Resolved category name for Existing.CategoryId, for rendering the "old" side of a Modified diff.</summary>
    public string? ExistingCategoryName { get; set; }

    /// <summary>Whether the user has this row checked for commit. Defaults true for New/Modified, false otherwise.</summary>
    public bool IsSelected { get; set; }
}

/// <summary>
/// Result of parsing an import file against the current DB state, before anything is written.
/// </summary>
public class RuleImportPreview
{
    public required RuleExportEnvelope Envelope { get; set; }
    public List<RuleImportPreviewItem> Items { get; set; } = [];

    public int NewCount => Items.Count(i => i.Status == RuleImportStatus.New);
    public int ModifiedCount => Items.Count(i => i.Status == RuleImportStatus.Modified);
    public int DuplicateCount => Items.Count(i => i.Status == RuleImportStatus.Duplicate);
    public int UnresolvedCount => Items.Count(i => i.Status == RuleImportStatus.CategoryNotFound);
}

/// <summary>
/// Result of committing a subset of a RuleImportPreview to the DB.
/// </summary>
public class RuleImportResult
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
}