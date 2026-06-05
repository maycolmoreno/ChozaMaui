using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class PedidosViewModel : ObservableObject
{
    private readonly RoleCapabilityService _capabilities;
    private readonly PedidoApiService _pedidosApi;
    private readonly NotificationService _notifications;
    private readonly PosOrderWorkflowService _pedidoWorkflow;
    private readonly PedidoPresentationService _presentation;
    private readonly SessionService _session;
    private readonly LiveRefreshCoordinator _refreshCoordinator;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private List<PedidoResponse> _todos = [];
    private const int PollingIntervalSeconds = 30;
    private const string TopicCamarero = "/topic/camarero";
    private const string TopicCocina = "/topic/cocina";

    public ObservableCollection<PedidoResponse> Pedidos { get; } = [];

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string filtroEstado = "TODOS";
    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string inicialesUsuario = "U";
    [ObservableProperty] private string nombreUsuarioHeader = "Usuario";
    [ObservableProperty] private string rolUsuarioHeader = "Usuario";
    [ObservableProperty] private string headerKpi1Titulo = "Activos";
    [ObservableProperty] private string headerKpi1Valor = "0";
    [ObservableProperty] private string headerKpi2Titulo = "Cocina";
    [ObservableProperty] private string headerKpi2Valor = "0";
    [ObservableProperty] private string headerKpi3Titulo = "Listos";
    [ObservableProperty] private string headerKpi3Valor = "0";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneAlertasHeader))]
    private int totalAlertasHeader;

    // ── KPIs ──────────────────────────────────────────────────────────
    [ObservableProperty] private int pedidosActivos;

    // ── Stats para barra inferior ──────────────────────────────────────
    [ObservableProperty] private int totalEnPreparacion;
    [ObservableProperty] private int totalListos;
    [ObservableProperty] private int totalEntregadosHoy;
    [ObservableProperty] private int totalCancelados;

    public bool PuedeAbrirPedidos => _capabilities.PuedeCrearPedido(_session.Rol);
    public bool PuedeEntregarPedidos => _capabilities.PuedeEntregarPedido(_session.Rol);
    public bool TieneAlertasHeader => TotalAlertasHeader > 0;

    public PedidosViewModel(RoleCapabilityService capabilities, PedidoApiService pedidosApi, NotificationService notifications, PosOrderWorkflowService pedidoWorkflow, PedidoPresentationService presentation, SessionService session, LiveRefreshCoordinator refreshCoordinator)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _capabilities = capabilities;
        _pedidosApi = pedidosApi;
        _notifications = notifications;
        _pedidoWorkflow = pedidoWorkflow;
        _presentation = presentation;
        _session = session;
        _refreshCoordinator = refreshCoordinator;
        System.Diagnostics.Debug.WriteLine($"[PERF][PedidosViewModel] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        if (!await _refreshLock.WaitAsync(0))
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            _todos = await _pedidosApi.GetPedidosAsync();
            AplicarFiltro();
            await _notifications.VerificarPedidosListosAsync(_todos);
            ActualizarHeaderOperativo();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            ActualizarHeaderOperativo();
        }
        finally
        {
            IsBusy = false;
            _refreshLock.Release();
            System.Diagnostics.Debug.WriteLine($"[PERF][PedidosViewModel] CargarAsync: {sw.ElapsedMilliseconds} ms");
        }
    }

    // ── Filtros chip ──────────────────────────────────────────────────

    [RelayCommand]
    public void SeleccionarFiltro(string filtro)
    {
        FiltroEstado = filtro;
    }

    partial void OnFiltroEstadoChanged(string value) => AplicarFiltro();
    partial void OnBusquedaChanged(string value) => AplicarFiltro();

    private void AplicarFiltro()
    {
        var snapshot = _presentation.BuildList(_todos, FiltroEstado, Busqueda);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Pedidos.Clear();
            foreach (var p in snapshot.Pedidos)
                Pedidos.Add(p);
            AplicarKpis(snapshot);
        });
    }

    private void AplicarKpis(PedidosPresentationSnapshot snapshot)
    {
        TotalEnPreparacion = snapshot.TotalEnPreparacion;
        TotalListos = snapshot.TotalListos;
        TotalEntregadosHoy = snapshot.TotalEntregadosHoy;
        TotalCancelados = snapshot.TotalCancelados;
        PedidosActivos = snapshot.PedidosActivos;
        ActualizarHeaderOperativo();
    }

    // ── Acciones de tarjeta ───────────────────────────────────────────

    [RelayCommand]
    public async Task NuevoPedidoAsync()
    {
        if (!PuedeAbrirPedidos)
        {
            ErrorMessage = "Tu perfil no tiene autorizacion para crear pedidos.";
            return;
        }

        await Shell.Current.GoToAsync("//mapa");
    }

    [RelayCommand]
    public async Task IrNotificacionesAsync()
    {
        await Shell.Current.GoToAsync("notificacionesPage");
    }

    [RelayCommand]
    public async Task VerDetalleAsync(PedidoResponse pedido)
    {
        await Shell.Current.GoToAsync($"pedidodetalle?id={pedido.Idpedido}");
    }

    [RelayCommand]
    public async Task IrAPagarAsync(PedidoResponse pedido)
    {
        await Shell.Current.GoToAsync("pago",
            new Dictionary<string, object> { { "Pedido", pedido } });
    }

    /// Navega a POS con la mesa del pedido seleccionado.
    [RelayCommand]
    public async Task AbrirEnPosAsync(PedidoResponse pedido)
    {
        if (!PuedeAbrirPedidos)
        {
            ErrorMessage = "Tu perfil no tiene autorizacion para abrir pedidos en POS.";
            return;
        }

        if (pedido.Mesa is null) return;
        await Shell.Current.GoToAsync("pos",
            new Dictionary<string, object>
            {
                { "Mesa", pedido.Mesa },
                { "PedidoId", pedido.Idpedido }
            });
    }

    /// Marca el pedido como COMPLETADO directamente desde la lista.
    [RelayCommand]
    public async Task EntregarRapidoAsync(PedidoResponse pedido)
    {
        if (!PuedeEntregarPedidos)
        {
            ErrorMessage = "Tu perfil no tiene autorizacion para entregar pedidos.";
            return;
        }

        if (!pedido.PuedeEntregarse)
        {
            ErrorMessage = "Solo se pueden entregar pedidos en estado LISTO_PARA_ENTREGA.";
            return;
        }

        IsBusy = true;
        try
        {
            await _pedidoWorkflow.CambiarEstadoPedidoAsync(pedido, PedidoEstados.Completado);
            await CargarAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al entregar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Polling automático ────────────────────────────────────────────

    public async Task IniciarPollingAsync()
    {
        await CargarAsync();

        await _refreshCoordinator.StartAsync(
            CargarAsync,
            ObtenerTopicsActivos(),
            OnNotificacionRecibida,
            PollingIntervalSeconds,
            minInitialRefreshIntervalSeconds: 0);
    }

    private void OnNotificacionRecibida(NotificacionPedidoWs notif)
    {
        // 1. Registrar en el historial compartido de notificaciones
        var esNuevoEvento = _notifications.RegistrarDesdeWebSocket(notif);
        if (!esNuevoEvento)
            return;

        // 2. El refresh lo ejecuta LiveRefreshCoordinator para no duplicar cargas.
        // 3. Mostrar alerta visual según evento
        var titulo = notif.Evento switch
        {
            _ when EsNotificacionLista(notif) => "🔔 ¡Pedido listo para entregar!",
            "CONFIRMAR" => "🍳 Nuevo pedido en cocina",
            "PREPARANDO" => "🍳 Pedido en preparacion",
            PedidoEstados.Cancelado => "❌ Pedido cancelado",
            _           => "📋 Cambio en pedido"
        };
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlertAsync(titulo, notif.Mensaje, "OK");
        });
    }

    private static bool EsNotificacionLista(NotificacionPedidoWs notif)
        => string.Equals(notif.Evento, PedidoEstados.Listo, StringComparison.OrdinalIgnoreCase)
           || string.Equals(notif.EstadoNuevo, PedidoEstados.ListoParaEntrega, StringComparison.OrdinalIgnoreCase);

    public void DetenerPolling()
    {
        _refreshCoordinator.Stop();
    }

    private IEnumerable<string> ObtenerTopicsActivos()
    {
        var rol = _session.Rol ?? string.Empty;
        if (_capabilities.PuedeConfirmarPedido(rol) || _capabilities.PuedeEntregarPedido(rol))
            yield return TopicCamarero;
        if (_capabilities.PuedeMarcarPedidoListo(rol))
            yield return TopicCocina;
    }

    private void ActualizarHeaderOperativo()
    {
        NombreUsuarioHeader = _session.NombreCompleto ?? _session.Username ?? "Usuario";
        RolUsuarioHeader = FormatearRol(_session.Rol);
        InicialesUsuario = CrearIniciales(NombreUsuarioHeader);
        TotalAlertasHeader = _notifications.Historial.Count(n => !n.Leida);

        var pendientes = _todos.Count(p => string.Equals(p.Estado, PedidoEstados.Pendiente, StringComparison.OrdinalIgnoreCase));
        switch ((_session.Rol ?? string.Empty).ToUpperInvariant())
        {
            case "COCINA":
                HeaderKpi1Titulo = "Pendientes";
                HeaderKpi1Valor = pendientes.ToString();
                HeaderKpi2Titulo = "Preparando";
                HeaderKpi2Valor = TotalEnPreparacion.ToString();
                HeaderKpi3Titulo = "Listos";
                HeaderKpi3Valor = TotalListos.ToString();
                break;
            case "ADMIN":
                HeaderKpi1Titulo = "Pedidos";
                HeaderKpi1Valor = _todos.Count.ToString();
                HeaderKpi2Titulo = "Activos";
                HeaderKpi2Valor = PedidosActivos.ToString();
                HeaderKpi3Titulo = "Alertas";
                HeaderKpi3Valor = TotalAlertasHeader.ToString();
                break;
            default:
                HeaderKpi1Titulo = "Activos";
                HeaderKpi1Valor = PedidosActivos.ToString();
                HeaderKpi2Titulo = "Cocina";
                HeaderKpi2Valor = TotalEnPreparacion.ToString();
                HeaderKpi3Titulo = "Listos";
                HeaderKpi3Valor = TotalListos.ToString();
                break;
        }
    }

    private static string CrearIniciales(string nombre)
    {
        var iniciales = string.Concat(nombre
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(p => p[0].ToString().ToUpperInvariant()));
        return string.IsNullOrWhiteSpace(iniciales) ? "U" : iniciales;
    }

    private static string FormatearRol(string? rol)
        => (rol ?? "USUARIO").ToUpperInvariant() switch
        {
            "CAJERO" => "Cajero",
            "CAMARERO" => "Camarero",
            "COCINA" => "Cocina",
            "ADMIN" => "Administrador",
            _ => "Usuario"
        };
}

