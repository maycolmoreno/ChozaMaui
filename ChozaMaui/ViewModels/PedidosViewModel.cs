using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;
using Microsoft.Maui.Graphics;

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

    public List<string> Estados { get; } =
        ["TODOS", "PENDIENTE", "EN_PROCESO", "LISTO", "ENTREGADO", "CANCELADO"];

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

    partial void OnFiltroEstadoChanged(string value) => AplicarFiltro();
    partial void OnBusquedaChanged(string value) => AplicarFiltro();

    private void AplicarFiltro()
    {
        var hoy = DateTime.Today;
        // Filtrar siempre por hoy — no se muestra histórico
        var resultado = _todos.Where(p => p.Fecha.Date == hoy).AsEnumerable();

        if (FiltroEstado != "TODOS")
            resultado = resultado.Where(p => p.Estado == FiltroEstado);

        if (!string.IsNullOrWhiteSpace(Busqueda))
        {
            var q = Busqueda.Trim().ToLower();
            resultado = resultado.Where(p =>
                p.Idpedido.ToString().Contains(q) ||
                (p.Mesa?.Etiqueta.ToLower().Contains(q) ?? false) ||
                (p.Cliente?.NombreCompleto.ToLower().Contains(q) ?? false));
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

        PedidosActivos = todosHoy.Count(p => p.Estado != "ENTREGADO" && p.Estado != "CANCELADO");
        TotalCompletados = todosHoy.Count(p => p.Estado == "ENTREGADO");

        var activos = todosHoy.Where(p => p.Estado != "ENTREGADO" && p.Estado != "CANCELADO").ToList();
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
        { CargaCocinaLabel = "High Intensity"; CargaCocinaColor = "#ef4444"; }
        else if (PedidosActivos >= 4)
        { CargaCocinaLabel = "Moderate";       CargaCocinaColor = "#f59e0b"; }
        else
        { CargaCocinaLabel = "Normal";         CargaCocinaColor = "#28b779"; }
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
