using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class AdminViewModel : ObservableObject
{
    private readonly ApiService _api;

    // ── Reporte de ventas del día ─────────────────────────────────
    [ObservableProperty] private double totalVentas;
    [ObservableProperty] private int numeroPedidos;
    [ObservableProperty] private double ticketPromedio;
    [ObservableProperty] private int totalProductos;
    [ObservableProperty] private ObservableCollection<ResumenProductoVenta> productosTop = new();

    // ── Turno activo ──────────────────────────────────────────────
    [ObservableProperty] private CajaTurnoResponse? turnoActivo;
    [ObservableProperty] private bool tieneTurnoAbierto;

    // ── Mesas ─────────────────────────────────────────────────────
    [ObservableProperty] private int mesasDisponibles;
    [ObservableProperty] private int mesasOcupadas;
    [ObservableProperty] private int totalMesas;

    // ── Pedidos del día ───────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<PedidoResponse> pedidosHoy = new();

    // ── Estado ────────────────────────────────────────────────────
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private DateTime fechaReporte = DateTime.Today;

    public AdminViewModel(ApiService api) => _api = api;

    [RelayCommand]
    public async Task CargarAsync()
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            // Paralelizar las 3 llamadas independientes
            var tareaReporte = _api.GetReporteVentasDiaAsync(FechaReporte);
            var tareaTurno   = _api.ObtenerCajaAbiertaAsync();
            var tareaMesas   = _api.ObtenerMesasAsync();

            await Task.WhenAll(tareaReporte, tareaTurno, tareaMesas);

            // Reporte de ventas
            var reporte = tareaReporte.Result;
            TotalVentas    = reporte.TotalVentas;
            NumeroPedidos  = reporte.NumeroPedidos;
            TicketPromedio = reporte.TicketPromedio;
            TotalProductos = reporte.TotalProductos;

            var top = reporte.Productos
                .OrderByDescending(p => p.CantidadVendida)
                .Take(5);
            ProductosTop = new ObservableCollection<ResumenProductoVenta>(top);

            // Turno
            TurnoActivo       = tareaTurno.Result;
            TieneTurnoAbierto = TurnoActivo is not null;

            // Mesas
            var mesas   = tareaMesas.Result;
            TotalMesas       = mesas.Count;
            MesasDisponibles = mesas.Count(m => m.Estado);
            MesasOcupadas    = mesas.Count(m => !m.Estado);
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al cargar panel: {ex.Message}";
        }
        finally { IsBusy = false; }
    }
}
