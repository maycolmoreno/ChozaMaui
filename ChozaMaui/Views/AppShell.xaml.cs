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
        var rolNormalizado = (rol ?? string.Empty).Trim().ToUpperInvariant();
        var esCocina = rolNormalizado == "COCINA";

        MesasTab.IsVisible = !esCocina;
        PedidosTab.IsVisible = true;
        AvisosTab.IsVisible = true;
        PerfilTab.IsVisible = true;

        if (esCocina)
            CurrentItem = PedidosTab;
    }
}
