using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class PerfilViewModel : ObservableObject
{
    private readonly SessionService _session;
    private readonly UsuarioApiService _usuariosApi;
    private readonly INavigationService _navigation;
    private readonly NotificationService _notifications;

    // Datos del perfil
    [ObservableProperty] private string nombreCompleto = string.Empty;
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string rol = string.Empty;
    [ObservableProperty] private string inicialUsuario = "U";
    [ObservableProperty] private string rolHeader = "Usuario";
    [ObservableProperty] private string headerKpi1Titulo = "Cuenta";
    [ObservableProperty] private string headerKpi1Valor = "Activa";
    [ObservableProperty] private string headerKpi2Titulo = "Usuario";
    [ObservableProperty] private string headerKpi2Valor = "--";
    [ObservableProperty] private string headerKpi3Titulo = "Alertas";
    [ObservableProperty] private string headerKpi3Valor = "0";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneAlertasHeader))]
    private int totalAlertasHeader;

    // Cambio de contraseña
    [ObservableProperty] private string passwordActual = string.Empty;
    [ObservableProperty] private string passwordNuevo = string.Empty;
    [ObservableProperty] private string passwordConfirmar = string.Empty;
    [ObservableProperty] private string mensajePassword = string.Empty;
    [ObservableProperty] private bool mensajeEsError;
    [ObservableProperty] private bool isBusy;
    public bool TieneAlertasHeader => TotalAlertasHeader > 0;

    public PerfilViewModel(SessionService session, UsuarioApiService usuariosApi, INavigationService navigation, NotificationService notifications)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _session = session;
        _usuariosApi = usuariosApi;
        _navigation = navigation;
        _notifications = notifications;
        System.Diagnostics.Debug.WriteLine($"[PERF][PerfilViewModel] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    [RelayCommand]
    public void CargarPerfil()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        NombreCompleto = _session.NombreCompleto ?? "—";
        Username = _session.Username ?? "—";
        Rol = _session.Rol ?? "—";
        InicialUsuario = string.Concat(NombreCompleto.Split(' ').Take(2)
            .Select(p => p.Length > 0 ? p[0].ToString().ToUpper() : ""));
        if (string.IsNullOrEmpty(InicialUsuario)) InicialUsuario = "U";
        RolHeader = FormatearRol(Rol);
        HeaderKpi1Titulo = "Cuenta";
        HeaderKpi1Valor = "Activa";
        HeaderKpi2Titulo = "Usuario";
        HeaderKpi2Valor = Username;
        HeaderKpi3Titulo = "Alertas";
        TotalAlertasHeader = _notifications.Historial.Count(n => !n.Leida);
        HeaderKpi3Valor = TotalAlertasHeader.ToString();
        System.Diagnostics.Debug.WriteLine($"[PERF][PerfilViewModel] CargarPerfil: {sw.ElapsedMilliseconds} ms");
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

    [RelayCommand]
    private async Task IrNotificacionesAsync()
    {
        await Shell.Current.GoToAsync("notificacionesPage");
    }

    private Task IrAInicioSegunRolAsync()
    {
        var ruta = (_session.Rol ?? string.Empty).ToUpperInvariant() switch
        {
            "CAJERO" => "//turnos",
            "COCINA" => "//pedidos",
            _ => "//mapa"
        };

        return Shell.Current.GoToAsync(ruta);
    }

    private static string FormatearRol(string? rol)
        => (rol ?? "USUARIO").ToUpperInvariant() switch
        {
            "CAJERO" => "Cajero",
            "CAMARERO" => "Camarero",
            "COCINA" => "Cocina",
            "ADMIN" => "Administrador",
            _ => "Usuario"
        };
}

