using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SessionService _session;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public LoginViewModel(ApiService api, SessionService session, INavigationService navigation)
    {
        _api = api;
        _session = session;
        _navigation = navigation;
    }

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
            var response = await _api.LoginAsync(Username, Password);
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
                // El usuario debe cambiar su contraseña antes de continuar
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
