using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Servicio singleton que detecta pedidos recién listos para entregar,
/// dispara vibración y muestra una alerta al mesero.
/// Comparte estado entre MapaViewModel y PedidosViewModel para evitar
/// notificaciones duplicadas del mismo pedido.
/// También acumula el historial de notificaciones de sesión para la pantalla
/// de notificaciones (NotificacionesViewModel).
/// </summary>
public class NotificationService
{
    // IDs de pedidos que ya fueron notificados como "listo"
    private readonly HashSet<int> _notificados = new();
    private readonly HashSet<string> _eventosWebSocketProcesados = new();
    private readonly object _eventosLock = new();

    // Evita apilar múltiples alertas simultáneas
    private bool _alertaActiva;

    // ── Historial de notificaciones (sesión) ──────────────────────────────
    private readonly List<Notificacion> _historial = new();
    private int _nextId = 1;

    /// <summary>Historial de notificaciones del turno actual (más reciente primero).</summary>
    public IReadOnlyList<Notificacion> Historial => _historial;

    /// <summary>Se dispara en el hilo principal cada vez que se agrega una nueva notificación.</summary>
    public event Action? Cambiaron;

    /// <summary>
    /// Agrega una notificación al historial y dispara el evento <see cref="Cambiaron"/>.
    /// Puede llamarse desde cualquier hilo; internamente se marshalea al hilo principal.
    /// </summary>
    public void AgregarNotificacion(Notificacion notif)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            notif.Id = _nextId++;
            _historial.Insert(0, notif);
            if (_historial.Count > 100)
                _historial.RemoveAt(_historial.Count - 1);
            Cambiaron?.Invoke();
        });
    }

    /// <summary>
    /// Convierte un mensaje WebSocket STOMP (<see cref="NotificacionPedidoWs"/>) en una
    /// <see cref="Notificacion"/> visible en el historial.
    /// </summary>
    public bool RegistrarDesdeWebSocket(NotificacionPedidoWs ws)
    {
        if (!MarcarEventoWebSocketSiEsNuevo(ws))
            return false;

        if (ws.PedidoId > 0)
        {
            if (EsEventoListo(ws))
                _notificados.Add(ws.PedidoId);

            if (EsEventoFinalizado(ws))
                _notificados.Remove(ws.PedidoId);
        }

        var (titulo, tipo) = ws.Evento switch
        {
            _ when EsEventoListo(ws) => ("¡Pedido listo para entregar!", "PEDIDO"),
            "CONFIRMAR" => ("Nuevo pedido en cocina",       "PEDIDO"),
            "PREPARANDO" => ("Pedido en preparacion",       "PEDIDO"),
            _ when EsEventoEntregado(ws) => ("Pedido entregado", "PEDIDO"),
            PedidoEstados.Cancelado => ("Pedido cancelado", "PEDIDO"),
            _           => ("Cambio en pedido",             "PEDIDO"),
        };

        AgregarNotificacion(new Notificacion
        {
            Titulo      = titulo,
            Descripcion = string.IsNullOrWhiteSpace(ws.Mensaje) ? $"Pedido #{ws.PedidoId}" : ws.Mensaje,
            Tipo        = tipo,
            Fecha       = ws.Fecha == default ? DateTime.Now : ws.Fecha,
            Leida       = false,
            Accion      = ConstruirAccionPedido(ws.PedidoId)
        });

        return true;
    }

    /// <summary>
    /// Compara la lista actual de pedidos contra el estado interno.
    /// Si hay pedidos nuevos en estado LISTO, vibra y muestra alerta.
    /// Llamar después de cada carga/poll.
    /// </summary>
    public async Task VerificarPedidosListosAsync(IEnumerable<PedidoResponse> pedidosActuales)
    {
        var lista = pedidosActuales.ToList();

        // 1. Detectar pedidos recién listos que aún no fueron notificados
        var nuevosListos = lista
            .Where(p => p.EstaListoParaEntrega && !_notificados.Contains(p.Idpedido))
            .ToList();

        // 2. Limpiar del set los pedidos ya terminados / cancelados
        var idsTerminados = lista
            .Where(p => p.Estado is PedidoEstados.Completado or PedidoEstados.Entregado or PedidoEstados.Cancelado)
            .Select(p => p.Idpedido)
            .ToHashSet();
        _notificados.RemoveWhere(id => idsTerminados.Contains(id));

        if (nuevosListos.Count == 0) return;

        // 3. Marcar como notificados antes de mostrar alerta
        foreach (var p in nuevosListos)
            _notificados.Add(p.Idpedido);

        // 3b. Registrar en el historial de sesión
        foreach (var p in nuevosListos)
        {
            var mesa   = p.Mesa?.Numero is int n ? $"Mesa #{n}" : "Sin mesa";
            var items  = string.IsNullOrWhiteSpace(p.ResumenItems) ? "" : $": {p.ResumenItems}";
            AgregarNotificacion(new Notificacion
            {
                Titulo      = "¡Pedido listo para entregar!",
                Descripcion = $"{mesa} — Pedido #{p.Idpedido}{items}",
                Tipo        = "PEDIDO",
                Fecha       = DateTime.Now,
                Leida       = false,
                Accion      = ConstruirAccionPedido(p.Idpedido)
            });
        }

        // 4-5. Evitar apilar alertas y vibraciones duplicadas
        if (_alertaActiva) return;
        _alertaActiva = true;

        await VibrarAsync();

        try
        {
            var titulo = nuevosListos.Count == 1
                ? "🔔 ¡Pedido listo para entregar!"
                : $"🔔 {nuevosListos.Count} pedidos listos para entregar";

            var lineas = nuevosListos.Select(p =>
            {
                var mesa = p.Mesa?.Numero is int n ? $"Mesa #{n}" : "Sin mesa";
                var items = string.IsNullOrWhiteSpace(p.ResumenItems)
                    ? "" : $" — {p.ResumenItems}";
                return $"• {mesa}{items}";
            });

            var cuerpo = string.Join("\n", lineas);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current is not null)
                    await Shell.Current.DisplayAlertAsync(titulo, cuerpo, "Entendido");
            });
        }
        finally
        {
            _alertaActiva = false;
        }
    }

    // ── Vibración ──────────────────────────────────────────────────────

    private static async Task VibrarAsync()
    {
        try
        {
            // 3 pulsos: 250ms vibra, 200ms pausa
            for (int i = 0; i < 3; i++)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(250));
                await Task.Delay(450);
            }
        }
        catch
        {
            // El dispositivo puede no tener vibrador — silenciar error
        }
    }

    private bool MarcarEventoWebSocketSiEsNuevo(NotificacionPedidoWs ws)
    {
        var key = $"{ws.PedidoId}:{ws.Evento}:{ws.EstadoNuevo}";

        lock (_eventosLock)
        {
            if (!_eventosWebSocketProcesados.Add(key))
                return false;

            if (ws.PedidoId > 0 && EsEventoFinalizado(ws))
            {
                var prefijo = $"{ws.PedidoId}:";
                _eventosWebSocketProcesados.RemoveWhere(k => k.StartsWith(prefijo, StringComparison.Ordinal));
                _eventosWebSocketProcesados.Add(key);
            }

            return true;
        }
    }

    private static string ConstruirAccionPedido(int pedidoId)
        => pedidoId > 0 ? $"pedidodetalle?id={pedidoId}" : "//pedidos";

    private static bool EsEventoListo(NotificacionPedidoWs ws)
        => string.Equals(ws.Evento, PedidoEstados.Listo, StringComparison.OrdinalIgnoreCase)
           || string.Equals(ws.EstadoNuevo, PedidoEstados.ListoParaEntrega, StringComparison.OrdinalIgnoreCase);

    private static bool EsEventoEntregado(NotificacionPedidoWs ws)
        => string.Equals(ws.Evento, PedidoEstados.Entregado, StringComparison.OrdinalIgnoreCase)
           || string.Equals(ws.EstadoNuevo, PedidoEstados.Completado, StringComparison.OrdinalIgnoreCase)
           || string.Equals(ws.EstadoNuevo, PedidoEstados.Entregado, StringComparison.OrdinalIgnoreCase);

    private static bool EsEventoFinalizado(NotificacionPedidoWs ws)
        => EsEventoEntregado(ws)
           || string.Equals(ws.Evento, PedidoEstados.Cancelado, StringComparison.OrdinalIgnoreCase)
           || string.Equals(ws.EstadoNuevo, PedidoEstados.Cancelado, StringComparison.OrdinalIgnoreCase);
}
