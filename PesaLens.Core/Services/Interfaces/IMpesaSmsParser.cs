using PesaLens.Core.Models;

namespace PesaLens.Core.Services.Interfaces;

public interface IMpesaSmsParser
{
    Transaction? Parse(string smsBody, long smsId, long smsTimestamp);
}