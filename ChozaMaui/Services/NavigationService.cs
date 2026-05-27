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

        w.Page?.Unfocus();

        if (string.Equals(_session.Rol, "CAJERO", StringComparison.OrdinalIgnoreCase))
        {
            w.Page = _services.GetRequiredService<AppShellCajero>();
        }
        else
        {
            var shell = _services.GetRequiredService<AppShell>();
            shell.AplicarVisibilidadRol(_session.Rol);   // oculta Turnos a CAMARERO/COCINA
            w.Page = shell;
        }
    }

    public void IrAlLogin()
    {
        if (Application.Current?.Windows.FirstOrDefault() is not Window w) return;

        w.Page?.Unfocus();
        w.Page = new NavigationPage(_services.GetRequiredService<LoginPage>());
    }
}
