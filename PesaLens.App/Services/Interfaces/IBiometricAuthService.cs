namespace PesaLens.App.Services.Interfaces;

public enum BiometricCheckResult
{
    Success,
    Failed,
    Cancelled,
    NotAvailable,
    Error
}

public interface IBiometricAuthService
{
    Task<bool> IsAvailableAsync();
    Task<BiometricCheckResult> AuthenticateAsync(string reason = "Unlock PesaLens");
}