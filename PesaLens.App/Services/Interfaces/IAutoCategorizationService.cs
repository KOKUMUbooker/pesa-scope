using PesaLens.Core.Models;

namespace PesaLens.App.Services.Interfaces;

public interface IAutoCategorizationService
{
    public Task CategorizeAsync(IList<Transaction> transactions);
}
