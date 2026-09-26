using PesaScope.Core.Models;

namespace PesaScope.App.Services.Interfaces;

/// <summary>
/// Holds a single in-flight RuleImportPreview between the moment Settings parses
/// a picked file and the moment RuleImportPage reads it in OnAppearing.
/// Registered as a singleton — deliberately single-slot, not a queue.
/// </summary>
public interface IPendingRuleImportSession
{
    void SetPreview(RuleImportPreview preview);
    RuleImportPreview? TakePreview(); // returns and clears
}