using ChozaMaui.Views;

namespace ChozaMaui.Services;

/// <summary>
/// Implementación de INavigationService que opera sobre la ventana activa de MAUI.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;
    private readonly SessionService _session;

    public NavigationService(IServiceProvider services, SessionService session)
    {
        _services = services;
        _session = session;
    }

    public void IrAlShellSegunRol()
    {
        if (Application.Current?.Windows.FirstOrDefault() is not Window w) return;

        w.Page = string.Equals(_session.Rol, "CAJERO", StringComparison.OrdinalIgnoreCase)
            ? _services.GetRequiredService<AppShellCajero>()
            : _services.GetRequiredService<AppShell>();
    }

    public void IrAlLogin()
    {
        if (Application.Current?.Windows.FirstOrDefault() is not Window w) return;

        w.Page = new NavigationPage(_services.GetRequiredService<LoginPage>());
    }
}
