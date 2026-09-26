using System.Text.Json;
using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Services.Interfaces;
using PesaScope.Core.Models;

namespace PesaScope.App.Services;

public class RuleExportService(
    IAutoCategorizationRuleRepository ruleRepo,
    ICategoryRepository categoryRepo) : IRuleExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    // ── Export ───────────────────────────────────────────────────────────────

    public async Task<string> ExportAsync(IEnumerable<(AutoCategorizationRule Rule, string CategoryName)> rules)
    {
        var envelope = new RuleExportEnvelope
        {
            SchemaVersion = 1,
            ExportedAtUtc = DateTime.UtcNow,
            AppVersion = AppInfo.VersionString is { } v ? $"{v} ({AppInfo.BuildString})" : "unknown",
            Rules = rules.Select(x => new RuleExportDto
            {
                RuleType = x.Rule.RuleType,
                MatchValue = x.Rule.MatchValue,
                CategoryName = x.CategoryName,
                Priority = x.Rule.Priority,
                IsEnabled = x.Rule.IsEnabled
            }).ToList()
        };

        // return JsonSerializer.Serialize(envelope, JsonOptions); // Crashes in release mode since this uses reflection
        return JsonSerializer.Serialize(envelope, RuleExportJsonContext.Default.RuleExportEnvelope);
    }

    // ── Parse (dry run, no writes) ──────────────────────────────────────────

    public async Task<RuleImportPreview> ParseAsync(string json)
    {
        RuleExportEnvelope? envelope;
        try
        {
            // envelope = JsonSerializer.Deserialize<RuleExportEnvelope>(json); // Crashes in release mode since this uses reflection
            envelope = JsonSerializer.Deserialize(json, RuleExportJsonContext.Default.RuleExportEnvelope);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("The selected file isn't a valid PesaScope rules export.", ex);
        }

        if (envelope is null || envelope.Rules.Count == 0)
            throw new InvalidDataException("No rules found in this file.");

        if (envelope.SchemaVersion > 1)
            throw new InvalidDataException("This file was exported from a newer version of PesaScope and can't be read here.");

        var allCategories = await categoryRepo.GetAllAsync();
        var categoryByName = allCategories.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

        var preview = new RuleImportPreview { Envelope = envelope };

        foreach (var dto in envelope.Rules)
        {
            var item = new RuleImportPreviewItem { Incoming = dto };

            if (!categoryByName.TryGetValue(dto.CategoryName, out var category))
            {
                item.Status = RuleImportStatus.CategoryNotFound;
                item.IsSelected = false;
                preview.Items.Add(item);
                continue;
            }

            item.ResolvedCategoryId = category.Id;

            var existing = await ruleRepo.GetByTypeAndValueAsync(dto.RuleType, dto.MatchValue);
            if (existing is null)
            {
                item.Status = RuleImportStatus.New;
                item.IsSelected = true;
            }
            else
            {
                item.Existing = existing;
                item.ExistingCategoryName = allCategories.FirstOrDefault(c => c.Id == existing.CategoryId)?.Name
                                            ?? "Unknown";

                bool unchanged = existing.CategoryId == category.Id
                                  && existing.Priority == dto.Priority
                                  && existing.IsEnabled == dto.IsEnabled;

                item.Status = unchanged ? RuleImportStatus.Duplicate : RuleImportStatus.Modified;
                item.IsSelected = !unchanged; // Modified defaults checked; Duplicate defaults unchecked
            }

            preview.Items.Add(item);
        }

        return preview;
    }

    // ── Commit ───────────────────────────────────────────────────────────────

    public async Task<RuleImportResult> CommitAsync(
        RuleImportPreview preview,
        IEnumerable<RuleImportPreviewItem> acceptedItems)
    {
        var result = new RuleImportResult();
        var accepted = acceptedItems.ToHashSet();

        foreach (var item in preview.Items)
        {
            if (!accepted.Contains(item) || !item.IsSelected)
            {
                result.Skipped++;
                continue;
            }

            switch (item.Status)
            {
                case RuleImportStatus.New when item.ResolvedCategoryId is { } newCategoryId:
                    {
                        var inserted = await ruleRepo.TryInsertAsync(new AutoCategorizationRule
                        {
                            RuleType = item.Incoming.RuleType,
                            MatchValue = item.Incoming.MatchValue,
                            CategoryId = newCategoryId,
                            Priority = item.Incoming.Priority,
                            IsEnabled = item.Incoming.IsEnabled,
                            CreatedAt = DateTime.UtcNow
                        });
                        if (inserted) result.Added++; else result.Skipped++;
                        break;
                    }

                case RuleImportStatus.Modified when item.Existing is not null && item.ResolvedCategoryId is { } updCategoryId:
                    {
                        item.Existing.CategoryId = updCategoryId;
                        item.Existing.Priority = item.Incoming.Priority;
                        item.Existing.IsEnabled = item.Incoming.IsEnabled;

                        var updated = await ruleRepo.TryUpdateAsync(item.Existing);
                        if (updated) result.Updated++; else result.Skipped++;
                        break;
                    }

                default:
                    result.Skipped++;
                    break;
            }
        }

        return result;
    }
}