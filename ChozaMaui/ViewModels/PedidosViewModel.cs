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

    // ── KPIs ──────────────────────────────────────────────────────────
    [ObservableProperty] private int pedidosActivos;

    // ── Stats para barra inferior ──────────────────────────────────────
    [ObservableProperty] private int totalEnPreparacion;
    [ObservableProperty] private int totalListos;
    [ObservableProperty] private int totalEntregadosHoy;
    [ObservableProperty] private int totalCancelados;

    public bool PuedeAbrirPedidos => _capabilities.PuedeCrearPedido(_session.Rol);
    public bool PuedeEntregarPedidos => _capabilities.PuedeEntregarPedido(_session.Rol);

    public PedidosViewModel(RoleCapabilityService capabilities, PedidoApiService pedidosApi, NotificationService notifications, PosOrderWorkflowService pedidoWorkflow, PedidoPresentationService presentation, SessionService session, LiveRefreshCoordinator refreshCoordinator)
    {
        _capabilities = capabilities;
        _pedidosApi = pedidosApi;
        _notifications = notifications;
        _pedidoWorkflow = pedidoWorkflow;
        _presentation = presentation;
        _session = session;
        _refreshCoordinator = refreshCoordinator;
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (!await _refreshLock.WaitAsync(0))
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            _todos = await _pedidosApi.GetPedidosAsync();
            AplicarFiltro();
            await _notifications.VerificarPedidosListosAsync(_todos);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _refreshLock.Release();
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
    }

    // ── Acciones de tarjeta ───────────────────────────────────────────

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
            await _pedidoWorkflow.CambiarEstadoPedidoAsync(pedido, "COMPLETADO");
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
            "LISTO"     => "🔔 ¡Pedido listo para entregar!",
            "CONFIRMAR" => "🍳 Nuevo pedido en cocina",
            "CANCELADO" => "❌ Pedido cancelado",
            _           => "📋 Cambio en pedido"
        };
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlertAsync(titulo, notif.Mensaje, "OK");
        });
    }

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
}

