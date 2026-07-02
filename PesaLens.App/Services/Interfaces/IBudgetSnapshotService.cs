namespace PesaLens.App.Services.Interfaces;

public interface IBudgetSnapshotService
{
    /// <summary>
    /// Takes a full snapshot for the given month — one overall row
    /// plus one row per category that had either a budget set or spending > 0.
    /// Safe to call multiple times; upserts rather than duplicates.
    /// </summary>
    Task TakeSnapshotAsync(int year, int month);

    /// <summary>
    /// Checks if the previous month needs snapshotting and does so.
    /// Call this on app foreground/startup.
    /// </summary>
    Task SnapshotPreviousMonthIfNeededAsync();
}
