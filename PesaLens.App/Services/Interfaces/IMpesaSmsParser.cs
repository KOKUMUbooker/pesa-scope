using PesaLens.App.Models;

namespace PesaLens.App.Services.Interfaces;

public interface IMpesaSmsParser
{
    Transaction? Parse(string smsBody, long smsId, long smsTimestamp);
}