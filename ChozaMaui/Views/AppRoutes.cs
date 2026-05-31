namespace ChozaMaui.Views;

/// <summary>
/// Centraliza el registro de rutas de navegación Shell.
/// El guardia estático garantiza que las rutas se registren exactamente una vez
/// independientemente de cuántos Shells se instancien durante la sesión.
/// </summary>
public static class AppRoutes
{
    private static bool _registered;
    private static readonly object _lock = new();

    public static void Register()
    {
        lock (_lock)
        {
            if (_registered) return;

            Routing.RegisterRoute("pedidodetalle", typeof(PedidoDetallePage));
            Routing.RegisterRoute("mesadetalle",   typeof(MesaDetallePage));
            Routing.RegisterRoute("pago",          typeof(PagoPage));
            Routing.RegisterRoute("pos",           typeof(PosPage));
            Routing.RegisterRoute("turnos",        typeof(TurnoPage));
            Routing.RegisterRoute("notificacionesPage", typeof(NotificacionesPage));

            _registered = true;
        }
    }
}
