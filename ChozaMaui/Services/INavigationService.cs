namespace ChozaMaui.Services;

/// <summary>
/// Abstrae la navegación entre shells/páginas.
/// Los ViewModels dependen de esta interfaz, no de IServiceProvider ni de App.
/// </summary>
public interface INavigationService
{
    /// <summary>Navega al shell correspondiente según el rol del usuario autenticado.</summary>
    void IrAlShellSegunRol();

    /// <summary>Vuelve a la pantalla de login (cierre de sesión).</summary>
    void IrAlLogin();

    /// <summary>Navega a una ruta Shell (//tab, ruta relativa o ruta con query string).</summary>
    Task GoToAsync(string route);

    /// <summary>Navega a una ruta Shell pasando parámetros de query como diccionario.</summary>
    Task GoToAsync(string route, IDictionary<string, object> parameters);
}
