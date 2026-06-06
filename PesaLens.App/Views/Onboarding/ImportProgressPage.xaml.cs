using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.Core.Models;
using PesaLens.App.Services.Interfaces;
using PesaLens.Core.Services.Interfaces;

namespace PesaLens.App.Views.Onboarding;

public partial class ImportProgressPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly IAppSettingsRepository _appSettingsRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ISyncMetadataRepository _syncMetadataRepo;
    private readonly ISmsReaderService _smsReader;
    private readonly IMpesaSmsParser _mpesaSmsParser;

    public ImportProgressPage(
        IAppSettingsRepository appSettingsRepo,
        ITransactionRepository transactionRepo,
        ISyncMetadataRepository syncMetadataRepo,
        ISmsReaderService smsReader,
        IMpesaSmsParser mpesaSmsParser)
    {
        InitializeComponent();

        _appSettingsRepo = appSettingsRepo;
        _transactionRepo = transactionRepo;
        _syncMetadataRepo = syncMetadataRepo;
        _mpesaSmsParser = mpesaSmsParser;
        _smsReader = smsReader;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = RunImportAsync();
    }

    // ── Import ────────────────────────────────────────────────────────────────

    private async Task RunImportAsync()
    {
        try
        {
            await SetStatusAsync("Reading messages from MPESA...");

            var messages = await _smsReader.GetAllMpesaMessagesAsync();

            if (messages == null || messages.Count == 0)
            {
                await FinishWithResultAsync(0, noMessages: true);
                return;
            }

            await SetStatusAsync($"Found {messages.Count} M-Pesa messages. Parsing...");

            int imported = await ParseAndImportAsync(messages);

            await FinishWithResultAsync(imported);
        }
        catch (Exception ex)
        {
            await SetStatusAsync($"Something went wrong: {ex.Message}");
            ShowDoneButton("Continue Anyway");
        }
    }

    private async Task<int> ParseAndImportAsync(List<PesaLens.App.Services.Interfaces.SmsMessage> messages)
    {
        var transactions = new List<Transaction>();
        int total = messages.Count;

        for (int i = 0; i < total; i++)
        {
            var msg = messages[i];
            var tx = _mpesaSmsParser.Parse(msg.Body, msg.SmsId, msg.Timestamp);

            if (tx is not null)
                transactions.Add(tx);

            // Batch UI updates every 10 messages to avoid flooding the main thread
            if (i % 10 == 0 || i == total - 1)
            {
                double progress = (double)(i + 1) / total;
                int found = transactions.Count;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ImportProgressBar.Progress = progress;
                    CountLabel.Text = $"{found} transaction{(found == 1 ? "" : "s")} found";
                    StatusLabel.Text = $"Parsing message {i + 1} of {total}...";
                });
            }
        }

        await SetStatusAsync("Saving to your device...");

        int inserted = await _transactionRepo.InsertManyAsync(transactions);

        if (messages.Count > 0)
        {
            var last = messages[^1];
            await _syncMetadataRepo.UpdateAfterSyncAsync(last.SmsId, last.Timestamp, inserted);
        }

        return inserted;
    }

    // ── Completion ────────────────────────────────────────────────────────────

    private async Task FinishWithResultAsync(int importedCount, bool noMessages = false)
    {
        var settings = await _appSettingsRepo.GetAsync();
        settings.OnboardingComplete = true;
        await _appSettingsRepo.UpdateAsync(settings);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ImportProgressBar.Progress = 1.0;

            if (noMessages)
            {
                StatusIcon.Text = "🤷";
                TitleLabel.Text = "No M-Pesa Messages Found";
                SubtitleLabel.Text = "We couldn't find any MPESA messages in your inbox. You can still explore the app — transactions will appear after your next M-Pesa activity.";
                StatusLabel.Text = "You can sync manually from the Settings page at any time.";
                CountLabel.IsVisible = false;
            }
            else
            {
                StatusIcon.Text = "✅";
                TitleLabel.Text = "Import Complete!";
                SubtitleLabel.Text = "Your M-Pesa history is ready.";
                StatusLabel.Text = $"Successfully imported {importedCount} transaction{(importedCount == 1 ? "" : "s")}.";
                CountLabel.Text = $"{importedCount} transaction{(importedCount == 1 ? "" : "s")} imported";
            }

            ShowDoneButton("Go to Dashboard");
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task SetStatusAsync(string message) =>
        MainThread.InvokeOnMainThreadAsync(() => StatusLabel.Text = message);

    private void ShowDoneButton(string label)
    {
        DoneButton.Text = label;
        DoneButton.IsVisible = true;
    }

    private void OnDoneClicked(object? sender, EventArgs e)
    {
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
            window.Page = new AppShell();
    }
}