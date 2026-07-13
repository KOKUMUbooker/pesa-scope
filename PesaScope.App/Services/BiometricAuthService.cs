using Plugin.Maui.Biometric;
using PesaLens.App.Services.Interfaces;

namespace PesaLens.App.Services;

public class BiometricAuthService(IBiometric biometricAuthentication) : IBiometricAuthService
{
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                var status = await biometricAuthentication.GetAuthenticationStatusAsync();
                return status is BiometricHwStatus.Success;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<BiometricCheckResult> AuthenticateAsync(string reason = "Unlock PesaLens")
    {
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                using CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken token = source.Token;
                var result = await biometricAuthentication.AuthenticateAsync(
                    new AuthenticationRequest () {  Title = "PesaLens",  AllowPasswordAuth = true ,Description = reason },
                    token
                );

                return result.Status == BiometricResponseStatus.Success
                    ? BiometricCheckResult.Success
                    : BiometricCheckResult.Failed;
            }

            return BiometricCheckResult.Success;
        }
        catch (OperationCanceledException)
        {
            return BiometricCheckResult.Cancelled;
        }
        catch
        {
            return BiometricCheckResult.Error;
        }
    }
}