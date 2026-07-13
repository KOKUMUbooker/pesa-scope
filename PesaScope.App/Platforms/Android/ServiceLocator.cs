namespace PesaScope.App;

/// <summary>
/// Thin bridge that lets platform components (BroadcastReceiver, etc.)
/// resolve services from the MAUI DI container without constructor injection.
/// Set once in MauiProgram.cs after the app is built.
/// </summary>
public static class ServiceLocator
{
    private static IServiceProvider? _provider;

    public static void Initialize(IServiceProvider provider) =>
        _provider = provider;

    public static T? GetService<T>() where T : class =>
        _provider?.GetService<T>();
}