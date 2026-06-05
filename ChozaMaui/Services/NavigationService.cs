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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _services = services;
        _session = session;
        System.Diagnostics.Debug.WriteLine($"[PERF][NavigationService] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    public void IrAlShellSegunRol()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        if (Application.Current?.Windows.FirstOrDefault() is not Window w) return;

        w.Page?.Unfocus();

        if (string.Equals(_session.Rol, "CAJERO", StringComparison.OrdinalIgnoreCase)
            || string.Equals(_session.Rol, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            w.Page = _services.GetRequiredService<AppShellCajero>();
            System.Diagnostics.Debug.WriteLine($"[PERF][Navigation] Crear/asignar AppShellCajero: {sw.ElapsedMilliseconds} ms");
        }
        else
        {
            var shell = _services.GetRequiredService<AppShell>();
            shell.AplicarVisibilidadRol(_session.Rol);   // oculta Turnos a CAMARERO/COCINA
            w.Page = shell;
            System.Diagnostics.Debug.WriteLine($"[PERF][Navigation] Crear/asignar AppShell: {sw.ElapsedMilliseconds} ms");
        }
    }

    public void IrAlLogin()
    {
        if (Application.Current?.Windows.FirstOrDefault() is not Window w) return;

        w.Page?.Unfocus();
        w.Page = new NavigationPage(_services.GetRequiredService<LoginPage>());
    }

    public Task GoToAsync(string route)
        => Shell.Current.GoToAsync(route);

    public Task GoToAsync(string route, IDictionary<string, object> parameters)
        => Shell.Current.GoToAsync(route, parameters);
}
