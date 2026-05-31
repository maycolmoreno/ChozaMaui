using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly UsuarioApiService _usuariosApi;
    private readonly ServerConnectionService _serverConnection;
    private readonly SessionService  _session;
    private readonly SettingsService _settings;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string username     = string.Empty;
    [ObservableProperty] private string password     = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool   isBusy;

    // ── Configuración del servidor ─────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UrlPreview))]
    private string servidorHost = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UrlPreview))]
    private string servidorPuerto = string.Empty;

    [ObservableProperty] private string estadoServidor     = string.Empty;
    [ObservableProperty] private string estadoServidorColor = "#8A94A6";
    [ObservableProperty] private bool   probandoConexion;
    [ObservableProperty] private bool   panelServidorVisible;

    public string UrlPreview =>
        SettingsService.TryNormalizeServer(ServidorHost, ServidorPuerto, out var host, out var port, out _)
            ? $"http://{host}:{port}"
            : $"http://{ServidorHost}:{ServidorPuerto}";

    public LoginViewModel(UsuarioApiService usuariosApi, ServerConnectionService serverConnection,
                          SessionService session, SettingsService settings, INavigationService navigation)
    {
        _usuariosApi = usuariosApi;
        _serverConnection = serverConnection;
        _session    = session;
        _settings   = settings;
        _navigation = navigation;

        // Cargar valores actuales de Preferences
        ServidorHost   = _settings.Host;
        ServidorPuerto = _settings.Port;
    }

    // ── Servidor: guardar y actualizar ────────────────────────────

    [RelayCommand]
    private void GuardarServidor()
    {
        TryGuardarServidor();
    }

    private bool TryGuardarServidor()
    {
        if (string.IsNullOrWhiteSpace(ServidorHost) || string.IsNullOrWhiteSpace(ServidorPuerto))
        {
            EstadoServidor      = "Ingresa host y puerto válidos.";
            EstadoServidorColor = "#DC2626";
            return false;
        }

        if (!_settings.TrySetServer(ServidorHost, ServidorPuerto, out var errorMessage))
        {
            EstadoServidor      = $"Servidor inválido: {errorMessage}";
            EstadoServidorColor = "#DC2626";
            return false;
        }

        ServidorHost = _settings.Host;
        ServidorPuerto = _settings.Port;

        EstadoServidor      = $"URL guardada: {_settings.BaseUrl}";
        EstadoServidorColor = "#64748B";
        return true;
    }

    [RelayCommand]
    private async Task ProbarConexionAsync()
    {
        if (!TryGuardarServidor())   // asegurar que el cliente HTTP usa la URL actual
            return;

        ProbandoConexion    = true;
        EstadoServidor      = "Probando conexión…";
        EstadoServidorColor = "#D97706";

        try
        {
            var (ok, estado) = await _serverConnection.PingAsync();
            EstadoServidor      = estado;
            EstadoServidorColor = ok ? "#16A34A" : "#DC2626";
        }
        finally
        {
            ProbandoConexion = false;
        }
    }

    [RelayCommand]
    private void TogglePanelServidor() =>
        PanelServidorVisible = !PanelServidorVisible;

    // ── Login ─────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Ingresa usuario y contraseña.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var response = await _usuariosApi.LoginAsync(Username, Password);
            await _session.GuardarSesionAsync(
                response.Token,
                response.Idusuario,
                response.Username,
                response.NombreCompleto,
                response.Rol);

            // Limpiar credenciales de memoria antes de navegar
            Password = string.Empty;
            Username = string.Empty;

            if (response.RequiereCambioPassword)
            {
                _navigation.IrAlShellSegunRol();
                await Shell.Current.GoToAsync("//perfil");
            }
            else
            {
                _navigation.IrAlShellSegunRol();
            }
        }
        catch (HttpRequestException ex) when ((int?)ex.StatusCode == 401)
        {
            ErrorMessage = "Usuario o contraseña incorrectos.";
        }
        catch (Exception)
        {
            ErrorMessage = "No se pudo conectar con el servidor. Verifica la red.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
