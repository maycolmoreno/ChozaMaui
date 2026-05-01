using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class PedidosViewModel : ObservableObject
{
    private readonly ApiService _api;
    private List<PedidoResponse> _todos = [];
    private CancellationTokenSource? _pollingCts;
    private const int PollingIntervalSeconds = 30;

    public ObservableCollection<PedidoResponse> Pedidos { get; } = [];

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string filtroEstado = "TODOS";
    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;

    // ── KPIs ──────────────────────────────────────────────────────────
    [ObservableProperty] private int pedidosActivos;
    [ObservableProperty] private int totalCompletados;
    [ObservableProperty] private string tiempoPromedioTexto = "0m";
    [ObservableProperty] private string cargaCocinaLabel = "Normal";
    [ObservableProperty] private string cargaCocinaColor = "#28b779";

    // ── Stats para barra inferior ──────────────────────────────────────
    [ObservableProperty] private int totalEnPreparacion;
    [ObservableProperty] private int totalListos;
    [ObservableProperty] private int totalEntregadosHoy;
    [ObservableProperty] private int totalCancelados;

    public PedidosViewModel(ApiService api) => _api = api;

    [RelayCommand]
    public async Task CargarAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            _todos = await _api.GetPedidosAsync();
            AplicarFiltro();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
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
        var hoy = DateTime.Today;
        var resultado = _todos.Where(p => p.Fecha.Date == hoy).AsEnumerable();

        resultado = FiltroEstado switch
        {
            "EN_PREPARACION" => resultado.Where(p =>
                p.Estado is "EN_COCINA" or "EN_BAR" or "EN_PROCESO" or "PENDIENTE"),
            "LISTOS"         => resultado.Where(p =>
                p.Estado is "LISTO_PARA_ENTREGA" or "LISTO"),
            "ENTREGADOS"     => resultado.Where(p =>
                p.Estado is "COMPLETADO" or "ENTREGADO"),
            "CANCELADOS"     => resultado.Where(p => p.Estado == "CANCELADO"),
            _                => resultado
        };

        if (!string.IsNullOrWhiteSpace(Busqueda))
        {
            var q = Busqueda.Trim().ToLower();
            resultado = resultado.Where(p =>
                p.Idpedido.ToString().Contains(q) ||
                (p.Mesa?.Etiqueta.ToLower().Contains(q) ?? false) ||
                (p.Cliente?.Nombre.ToLower().Contains(q) ?? false));
        }

        Pedidos.Clear();
        foreach (var p in resultado.OrderByDescending(p => p.Fecha))
            Pedidos.Add(p);

        RecalcularKpis();
    }

    private void RecalcularKpis()
    {
        var hoy = DateTime.Today;
        var todosHoy = _todos.Where(p => p.Fecha.Date == hoy).ToList();

        TotalEnPreparacion = todosHoy.Count(p =>
            p.Estado is "EN_COCINA" or "EN_BAR" or "EN_PROCESO" or "PENDIENTE");
        TotalListos         = todosHoy.Count(p => p.Estado is "LISTO_PARA_ENTREGA" or "LISTO");
        TotalEntregadosHoy  = todosHoy.Count(p => p.Estado is "COMPLETADO" or "ENTREGADO");
        TotalCancelados     = todosHoy.Count(p => p.Estado == "CANCELADO");

        PedidosActivos   = TotalEnPreparacion + TotalListos;
        TotalCompletados = TotalEntregadosHoy;

        var activos = todosHoy.Where(p => p.EsActivo).ToList();
        if (activos.Count == 0)
            TiempoPromedioTexto = "0m";
        else
        {
            var avgMin = activos.Average(p => (DateTime.Now - p.Fecha).TotalMinutes);
            TiempoPromedioTexto = avgMin < 60
                ? $"{(int)avgMin}m"
                : $"{(int)avgMin / 60}h {(int)avgMin % 60}m";
        }

        if (PedidosActivos >= 9)
        { CargaCocinaLabel = "Alta intensidad"; CargaCocinaColor = "#ef4444"; }
        else if (PedidosActivos >= 4)
        { CargaCocinaLabel = "Moderada";        CargaCocinaColor = "#f59e0b"; }
        else
        { CargaCocinaLabel = "Normal";          CargaCocinaColor = "#28b779"; }
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
        if (pedido.Mesa is null) return;
        await Shell.Current.GoToAsync("//pos",
            new Dictionary<string, object> { { "Mesa", pedido.Mesa } });
    }

    /// Marca el pedido como COMPLETADO directamente desde la lista.
    [RelayCommand]
    public async Task EntregarRapidoAsync(PedidoResponse pedido)
    {
        IsBusy = true;
        try
        {
            await _api.CambiarEstadoPedidoAsync(pedido.Idpedido, "COMPLETADO");
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
        _pollingCts?.Cancel();
        _pollingCts = new CancellationTokenSource();
        var token = _pollingCts.Token;

        await CargarAsync();

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(PollingIntervalSeconds));
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                    await MainThread.InvokeOnMainThreadAsync(CargarAsync);
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    public void DetenerPolling()
    {
        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingCts = null;
    }
}

