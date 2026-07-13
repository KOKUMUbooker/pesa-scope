using PesaScope.Core.Models;

namespace PesaScope.Core.Services.Interfaces;

public interface IMpesaSmsParser
{
    Transaction? Parse(string smsBody, long smsId, long smsTimestamp);
}