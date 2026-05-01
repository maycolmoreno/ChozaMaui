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
}
