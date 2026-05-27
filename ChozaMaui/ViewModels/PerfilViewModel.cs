using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class PerfilViewModel : ObservableObject
{
    private readonly SessionService _session;
    private readonly UsuarioApiService _usuariosApi;
    private readonly INavigationService _navigation;

    // Datos del perfil
    [ObservableProperty] private string nombreCompleto = string.Empty;
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string rol = string.Empty;
    [ObservableProperty] private string inicialUsuario = "U";

    // Cambio de contraseña
    [ObservableProperty] private string passwordActual = string.Empty;
    [ObservableProperty] private string passwordNuevo = string.Empty;
    [ObservableProperty] private string passwordConfirmar = string.Empty;
    [ObservableProperty] private string mensajePassword = string.Empty;
    [ObservableProperty] private bool mensajeEsError;
    [ObservableProperty] private bool isBusy;

    public PerfilViewModel(SessionService session, UsuarioApiService usuariosApi, INavigationService navigation)
    {
        _session = session;
        _usuariosApi = usuariosApi;
        _navigation = navigation;
    }

    [RelayCommand]
    public void CargarPerfil()
    {
        NombreCompleto = _session.NombreCompleto ?? "—";
        Username = _session.Username ?? "—";
        Rol = _session.Rol ?? "—";
        InicialUsuario = string.Concat(NombreCompleto.Split(' ').Take(2)
            .Select(p => p.Length > 0 ? p[0].ToString().ToUpper() : ""));
        if (string.IsNullOrEmpty(InicialUsuario)) InicialUsuario = "U";
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
            await _usuariosApi.CambiarPasswordAsync(PasswordActual, PasswordNuevo);
            MensajePassword = "Contraseña cambiada exitosamente.";
            MensajeEsError = false;
            PasswordActual = string.Empty;
            PasswordNuevo = string.Empty;
            PasswordConfirmar = string.Empty;

            await IrAInicioSegunRolAsync();
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

    private Task IrAInicioSegunRolAsync()
    {
        var ruta = (_session.Rol ?? string.Empty).ToUpperInvariant() switch
        {
            "CAJERO" => "//cuentas",
            "COCINA" => "//pedidos",
            _ => "//mapa"
        };

        return Shell.Current.GoToAsync(ruta);
    }
}

