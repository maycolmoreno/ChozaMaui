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
    [ObservableProperty] private ObservableCollection<GrupoComedor> grupos = new();

    // Contadores para la leyenda
    [ObservableProperty] private int totalDisponibles;
    [ObservableProperty] private int totalOcupadas;

    public MapaViewModel(ApiService api) => _api = api;

    [RelayCommand]
    public async Task CargarAsync()
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var mesas = await _api.ObtenerMesasAsync();

            TotalDisponibles = mesas.Count(m => m.Estado);
            TotalOcupadas    = mesas.Count(m => !m.Estado);

            var agrupadas = mesas
                .GroupBy(m => string.IsNullOrWhiteSpace(m.NombreComedor) ? "Sin comedor" : m.NombreComedor)
                .Select(g => new GrupoComedor(g.Key, g.OrderBy(m => m.Numero)))
                .OrderBy(g => g.Nombre);

            Grupos = new ObservableCollection<GrupoComedor>(agrupadas);

            if (!Grupos.Any()) Mensaje = "No hay mesas registradas.";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al cargar mesas: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task VerDetalleMesaAsync(MesaResponse mesa)
    {
        await Shell.Current.GoToAsync("mesadetalle",
            new Dictionary<string, object> { { "Mesa", mesa } });
    }
}
