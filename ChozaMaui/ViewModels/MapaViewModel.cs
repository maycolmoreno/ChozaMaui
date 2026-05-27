using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class MapaViewModel : ObservableObject
{
    private readonly PedidoApiService _pedidosApi;
    private readonly MesaStateService _mesas;
    private readonly MapaPresentationService _presentation;
    private readonly NotificationService _notifications;
    private readonly SessionService _session;
    private readonly LiveRefreshCoordinator _refreshCoordinator;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private const int PollingIntervalSeconds = 30;
    private const string TopicCamarero = "/topic/camarero";
    private const string TopicCocina = "/topic/cocina";

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private ObservableCollection<GrupoMesaVisual> grupos = new();

    // Header: datos del usuario actual
    [ObservableProperty] private string nombreUsuario = string.Empty;
    [ObservableProperty] private string rolUsuario = string.Empty;
    [ObservableProperty] private string inicialUsuario = string.Empty;

    // Contadores para la leyenda
    [ObservableProperty] private int totalDisponibles;
    [ObservableProperty] private int totalOcupadas;
    [ObservableProperty] private int totalEnPreparacion;
    [ObservableProperty] private int totalPendientePago;

    // ── Bottom sheet ───────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SheetTitulo))]
    [NotifyPropertyChangedFor(nameof(SheetComedorTexto))]
    [NotifyPropertyChangedFor(nameof(SheetCapacidadTexto))]
    [NotifyPropertyChangedFor(nameof(SheetEstadoTexto))]
    [NotifyPropertyChangedFor(nameof(SheetEstadoColor))]
    [NotifyPropertyChangedFor(nameof(SheetEsPendientePago))]
    [NotifyPropertyChangedFor(nameof(PuedeCobrar))]
    [NotifyPropertyChangedFor(nameof(SheetTienePedido))]
    private MesaVisual? mesaSheet;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SheetTienePedido))]
    [NotifyPropertyChangedFor(nameof(SheetPedidoTitulo))]
    [NotifyPropertyChangedFor(nameof(SheetPedidoResumen))]
    [NotifyPropertyChangedFor(nameof(SheetPedidoObservaciones))]
    [NotifyPropertyChangedFor(nameof(SheetPedidoTotalTexto))]
    [NotifyPropertyChangedFor(nameof(SheetPuedeEnviarACocina))]
    private PedidoResponse? pedidoSheet;

    [ObservableProperty] private bool mostrarSheet;

    public string SheetTitulo        => MesaSheet is null ? "" : $"Mesa #{MesaSheet.Numero}";
    public string SheetComedorTexto  => MesaSheet?.NombreComedor ?? "";
    public string SheetCapacidadTexto => MesaSheet is null ? "" : $"Capacidad: {MesaSheet.Capacidad} personas";
    public string SheetEstadoTexto   => MesaSheet?.EstadoVisual ?? "";
    public string SheetEstadoColor   => MesaSheet?.EstadoColor ?? "#6b7280";
    public bool   SheetEsPendientePago => MesaSheet?.EstadoVisual == "Pendiente de pago";
    public bool   PuedeCobrar          => SheetEsPendientePago && _session.Rol != "CAMARERO";
    public bool   SheetTienePedido     => PedidoSheet is not null;
    public string SheetPedidoTitulo    => PedidoSheet is null ? string.Empty : $"Pedido #{PedidoSheet.Idpedido}";
    public string SheetPedidoResumen   => _presentation.BuildSheetPedidoResumen(PedidoSheet);
    public string SheetPedidoObservaciones => string.IsNullOrWhiteSpace(PedidoSheet?.Observaciones)
        ? "Sin observaciones"
        : PedidoSheet!.Observaciones!;
    public string SheetPedidoTotalTexto => PedidoSheet is null ? string.Empty : $"${PedidoSheet.Total:0.00}";
    public bool SheetPuedeEnviarACocina =>
        PedidoSheet?.Estado == "PENDIENTE" &&
        !string.Equals(_session.Rol, "COCINA", StringComparison.OrdinalIgnoreCase);

    public MapaViewModel(PedidoApiService pedidosApi, MesaStateService mesas, MapaPresentationService presentation, NotificationService notifications, SessionService session, LiveRefreshCoordinator refreshCoordinator)
    {
        _pedidosApi = pedidosApi;
        _mesas = mesas;
        _presentation = presentation;
        _notifications = notifications;
        _session = session;
        _refreshCoordinator = refreshCoordinator;
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (!await _refreshLock.WaitAsync(0))
            return;

        // Cargar datos del usuario en el header
        NombreUsuario = _session.NombreCompleto ?? "Usuario";
        RolUsuario = (_session.Rol ?? "CAMARERO").ToUpper();
        InicialUsuario = NombreUsuario.Length > 0
            ? string.Concat(NombreUsuario.Split(' ').Take(2).Select(p => p.Length > 0 ? p[0].ToString() : ""))
            : "U";

        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var mesasTask = _mesas.ObtenerMesasAsync();
            var pedidosTask = _pedidosApi.GetPedidosAsync();

            await Task.WhenAll(mesasTask, pedidosTask);

            var snapshot = _presentation.Build(mesasTask.Result, pedidosTask.Result);
            TotalDisponibles = snapshot.TotalDisponibles;
            TotalOcupadas = snapshot.TotalOcupadas;
            TotalEnPreparacion = snapshot.TotalEnPreparacion;
            TotalPendientePago = snapshot.TotalPendientePago;
            ReemplazarItems(Grupos, snapshot.Grupos);

            if (!Grupos.Any()) Mensaje = "No hay mesas registradas.";

            // Notificar si hay pedidos recién listos
            await _notifications.VerificarPedidosListosAsync(pedidosTask.Result);
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al cargar mesas: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _refreshLock.Release();
        }
    }

    // Abre el bottom sheet al tocar una mesa
    [RelayCommand]
    public async Task VerDetalleMesa(MesaVisual mesa)
    {
        MesaSheet = mesa;
        PedidoSheet = null;

        var pedidoBase = mesa.PedidosActivos
            .OrderByDescending(p => p.Fecha)
            .FirstOrDefault();

        if (pedidoBase is not null)
        {
            try
            {
                PedidoSheet = await _pedidosApi.GetPedidoPorIdAsync(pedidoBase.Idpedido);
            }
            catch
            {
                PedidoSheet = pedidoBase;
            }
        }

        MostrarSheet = true;
    }

    [RelayCommand]
    public void CerrarSheet()
    {
        MostrarSheet = false;
        PedidoSheet = null;
    }

    // "Ver / Continuar pedido" → navega a POS con la mesa
    [RelayCommand]
    public async Task IrAlPosAsync()
    {
        if (MesaSheet is null) return;
        MostrarSheet = false;
        await Shell.Current.GoToAsync("pos",
            new Dictionary<string, object> { { "Mesa", MesaSheet.Mesa } });
    }

    // "Solicitar cuenta / Cobrar" → navega al pedido entregado para pago
    [RelayCommand]
    public async Task IrAPagoAsync()
    {
        if (MesaSheet is null) return;
        var pedidoEntregado = MesaSheet.PedidosActivos
            .FirstOrDefault(p => p.Estado == "ENTREGADO");
        MostrarSheet = false;
        if (pedidoEntregado is not null)
        {
            await Shell.Current.GoToAsync("pago",
                new Dictionary<string, object> { { "Pedido", pedidoEntregado } });
        }
        else
        {
            await Shell.Current.GoToAsync("mesadetalle",
                new Dictionary<string, object> { { "Mesa", MesaSheet.Mesa } });
        }
    }

    // "Detalles de la mesa" → navega a MesaDetalle
    [RelayCommand]
    public async Task IrADetalleAsync()
    {
        if (MesaSheet is null) return;
        MostrarSheet = false;
        await Shell.Current.GoToAsync("mesadetalle",
            new Dictionary<string, object> { { "Mesa", MesaSheet.Mesa } });
    }

    [RelayCommand]
    public async Task EnviarPedidoACocinaAsync()
    {
        if (!SheetPuedeEnviarACocina || PedidoSheet is null)
            return;

        try
        {
            PedidoSheet = await _pedidosApi.CambiarEstadoPedidoAsync(PedidoSheet.Idpedido, "EN_COCINA");
            await CargarAsync();
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al enviar a cocina: {ex.Message}";
        }
    }

    private static void ReemplazarItems<T>(ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }

    // ── Polling automático ────────────────────────────────────────────

    public async Task IniciarPollingAsync()
    {
        await _refreshCoordinator.StartAsync(
            CargarAsync,
            ObtenerTopicsActivos(),
            OnNotificacionRecibida,
            PollingIntervalSeconds);
    }

    public void DetenerPolling()
    {
        _refreshCoordinator.Stop();
    }

    private void OnNotificacionRecibida(NotificacionPedidoWs notif)
    {
    }

    private IEnumerable<string> ObtenerTopicsActivos()
    {
        var rol = _session.Rol ?? string.Empty;
        if (rol is "CAMARERO" or "ADMIN" or "CAJERO")
            yield return TopicCamarero;
        if (rol is "COCINA" or "ADMIN")
            yield return TopicCocina;
    }

}
