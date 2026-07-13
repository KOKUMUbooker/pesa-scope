namespace PesaScope.App.Platforms.Android;

public static class SmsRoleActivityResultHelper
{
    public const int RequestCode = 1001;

    // PermissionPage subscribes to this; MainActivity raises it
    public static event Action<bool>? RoleRequestCompleted;

    public static void NotifyResult(bool granted) =>
        RoleRequestCompleted?.Invoke(granted);
}