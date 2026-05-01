using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class TurnoViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SessionService _session;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string montoInicial = string.Empty;
    [ObservableProperty] private string montoFinal = string.Empty;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private CajaTurnoResponse? turnoActivo;
    [ObservableProperty] private bool tieneTurnoAbierto;

    public TurnoViewModel(ApiService api, SessionService session)
    {
        _api = api;
        _session = session;
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            TurnoActivo = await _api.ObtenerCajaAbiertaAsync();
            TieneTurnoAbierto = TurnoActivo != null;
        }
        catch (Exception)
        {
            TieneTurnoAbierto = false;
            Mensaje = "No se pudo verificar el turno activo. Revisa la conexión.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task AbrirTurnoAsync()
    {
        var raw = MontoInicial.Replace(",", ".");
        if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var monto) || monto < 0)
        {
            Mensaje = "Ingrese un monto inicial válido.";
            return;
        }
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var usuario = _session.Username ?? "desconocido";
            TurnoActivo = await _api.AbrirCajaAsync(monto, usuario);
            TieneTurnoAbierto = true;
            MontoInicial = string.Empty;
            Mensaje = $"Turno abierto con ${TurnoActivo.MontoInicial:F2}.";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task CerrarTurnoAsync()
    {
        var raw = MontoFinal.Replace(",", ".");
        if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var monto) || monto < 0)
        {
            Mensaje = "Ingrese el monto final declarado.";
            return;
        }
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var usuario = _session.Username ?? "desconocido";
            TurnoActivo = await _api.CerrarCajaAsync(monto, usuario);
            TieneTurnoAbierto = false;
            MontoFinal = string.Empty;
            Mensaje = "Turno cerrado correctamente.";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }
}
