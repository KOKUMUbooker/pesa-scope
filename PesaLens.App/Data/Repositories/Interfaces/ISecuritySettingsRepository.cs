using PesaLens.App.Models;

namespace PesaLens.App.Data.Repositories.Interfaces;

public interface ISecuritySettingsRepository
{
    /// <summary>
    /// Returns the single security settings row.
    /// Creates and returns defaults if the row doesn't exist yet.
    /// </summary>
    Task<SecuritySettings> GetAsync();

    /// <summary>
    /// Saves a new hashed PIN. Pass null to remove PIN lock.
    /// Never call this with a raw unhashed PIN.
    /// </summary>
    Task<int> UpdatePinHashAsync(string? pinHash);

    /// <summary>
    /// Enables or disables biometric authentication.
    /// </summary>
    Task<int> SetBiometricsEnabledAsync(bool enabled);

    /// <summary>
    /// Clears both PIN hash and disables biometrics.
    /// Called when the user turns off all app lock options.
    /// </summary>
    Task ResetAsync();
}