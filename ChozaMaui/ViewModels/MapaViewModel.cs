using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class MapaViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private ObservableCollection<GrupoMesaVisual> grupos = new();

    // Contadores para la leyenda
    [ObservableProperty] private int totalDisponibles;
    [ObservableProperty] private int totalOcupadas;
    [ObservableProperty] private int totalEnPreparacion;
    [ObservableProperty] private int totalListas;

    // ── Bottom sheet ───────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SheetTitulo))]
    [NotifyPropertyChangedFor(nameof(SheetComedorTexto))]
    [NotifyPropertyChangedFor(nameof(SheetCapacidadTexto))]
    [NotifyPropertyChangedFor(nameof(SheetEstadoTexto))]
    [NotifyPropertyChangedFor(nameof(SheetEstadoColor))]
    private MesaVisual? mesaSheet;

    [ObservableProperty] private bool mostrarSheet;

    public string SheetTitulo        => MesaSheet is null ? "" : $"Mesa #{MesaSheet.Numero}";
    public string SheetComedorTexto  => MesaSheet?.NombreComedor ?? "";
    public string SheetCapacidadTexto => MesaSheet is null ? "" : $"Capacidad: {MesaSheet.Capacidad} personas";
    public string SheetEstadoTexto   => MesaSheet?.EstadoVisual ?? "";
    public string SheetEstadoColor   => MesaSheet?.EstadoColor ?? "#6b7280";

    public MapaViewModel(ApiService api) => _api = api;

    [RelayCommand]
    public async Task CargarAsync()
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var mesasTask = _api.ObtenerMesasAsync();
            var pedidosTask = _api.GetPedidosAsync();

            await Task.WhenAll(mesasTask, pedidosTask);

            var mesas = mesasTask.Result;
            var pedidosActivos = pedidosTask.Result
                .Where(p => p.EsActivo && p.Mesa is not null)
                .ToList();

            var mesasVisuales = mesas
                .Select(m => new MesaVisual
                {
                    Mesa = m,
                    PedidosActivos = pedidosActivos
                        .Where(p => p.Mesa?.Idmesa == m.Idmesa)
                        .OrderByDescending(p => p.Fecha)
                        .ToList()
                })
                .ToList();

            TotalDisponibles    = mesasVisuales.Count(m => m.EstadoVisual == "Disponible");
            TotalOcupadas       = mesasVisuales.Count(m => m.EstadoVisual == "Ocupada");
            TotalEnPreparacion  = mesasVisuales.Count(m => m.EstadoVisual == "En preparacion");
            TotalListas         = mesasVisuales.Count(m => m.EstadoVisual == "Lista para entregar");

            var agrupadas = mesasVisuales
                .GroupBy(m => m.NombreComedor)
                .Select(g => new GrupoMesaVisual(g.Key, g.OrderBy(m => m.Numero)))
                .OrderBy(g => g.Nombre);

            Grupos = new ObservableCollection<GrupoMesaVisual>(agrupadas);

            if (!Grupos.Any()) Mensaje = "No hay mesas registradas.";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al cargar mesas: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // Abre el bottom sheet al tocar una mesa
    [RelayCommand]
    public void VerDetalleMesa(MesaVisual mesa)
    {
        MesaSheet = mesa;
        MostrarSheet = true;
    }

    [RelayCommand]
    public void CerrarSheet() => MostrarSheet = false;

    // "Ver / Continuar pedido" → navega a POS con la mesa
    [RelayCommand]
    public async Task IrAlPosAsync()
    {
        if (MesaSheet is null) return;
        MostrarSheet = false;
        await Shell.Current.GoToAsync("//pos",
            new Dictionary<string, object> { { "Mesa", MesaSheet.Mesa } });
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
}
