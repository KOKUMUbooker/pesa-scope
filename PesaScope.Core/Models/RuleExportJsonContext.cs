using System.Text.Json.Serialization;

namespace PesaScope.Core.Models;

[JsonSerializable(typeof(RuleExportEnvelope))]
[JsonSerializable(typeof(RuleExportDto))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class RuleExportJsonContext : JsonSerializerContext
{
}