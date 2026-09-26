using PesaScope.App.Services.Interfaces;
using PesaScope.Core.Models;

namespace PesaScope.App.Services;

public class PendingRuleImportSession : IPendingRuleImportSession
{
    private RuleImportPreview? _preview;

    public void SetPreview(RuleImportPreview preview) => _preview = preview;

    public RuleImportPreview? TakePreview()
    {
        var p = _preview;
        _preview = null;
        return p;
    }
}