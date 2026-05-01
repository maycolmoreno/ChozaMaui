using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class PerfilViewModel : ObservableObject
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private readonly INavigationService _navigation;

    // Datos del perfil
    [ObservableProperty] private string nombreCompleto = string.Empty;
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string rol = string.Empty;

    // Cambio de contraseña
    [ObservableProperty] private string passwordActual = string.Empty;
    [ObservableProperty] private string passwordNuevo = string.Empty;
    [ObservableProperty] private string passwordConfirmar = string.Empty;
    [ObservableProperty] private string mensajePassword = string.Empty;
    [ObservableProperty] private bool mensajeEsError;
    [ObservableProperty] private bool isBusy;

    public PerfilViewModel(SessionService session, ApiService api, INavigationService navigation)
    {
        _session = session;
        _api = api;
        _navigation = navigation;
    }

    [RelayCommand]
    public void CargarPerfil()
    {
        NombreCompleto = _session.NombreCompleto ?? "—";
        Username = _session.Username ?? "—";
        Rol = _session.Rol ?? "—";
    }

    [RelayCommand]
    private async Task CambiarPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(PasswordActual) ||
            string.IsNullOrWhiteSpace(PasswordNuevo) ||
            string.IsNullOrWhiteSpace(PasswordConfirmar))
        {
            MensajePassword = "Completa todos los campos.";
            MensajeEsError = true;
            return;
        }

        if (PasswordNuevo != PasswordConfirmar)
        {
            MensajePassword = "Las contraseñas nuevas no coinciden.";
            MensajeEsError = true;
            return;
        }

        if (PasswordNuevo.Length < 6)
        {
            MensajePassword = "La contraseña debe tener al menos 6 caracteres.";
            MensajeEsError = true;
            return;
        }

        IsBusy = true;
        MensajePassword = string.Empty;
        try
        {
            await _api.CambiarPasswordAsync(PasswordActual, PasswordNuevo);
            MensajePassword = "Contraseña cambiada exitosamente.";
            MensajeEsError = false;
            PasswordActual = string.Empty;
            PasswordNuevo = string.Empty;
            PasswordConfirmar = string.Empty;
        }
        catch (HttpRequestException ex) when ((int?)ex.StatusCode == 400 || (int?)ex.StatusCode == 403)
        {
            MensajePassword = "La contraseña actual es incorrecta.";
            MensajeEsError = true;
        }
        catch (Exception ex)
        {
            MensajePassword = $"Error: {ex.Message}";
            MensajeEsError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CerrarSesion()
    {
        _session.CerrarSesion();
        _navigation.IrAlLogin();
    }
}

