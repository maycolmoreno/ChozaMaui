namespace ChozaMaui.Views;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        AppRoutes.Register();
    }

    public void AplicarVisibilidadRol(string? rol)
    {
        // Turnos ya no está en el TabBar principal — no se necesita filtrar por rol aquí
    }
}
