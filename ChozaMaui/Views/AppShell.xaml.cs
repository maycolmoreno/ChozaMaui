namespace ChozaMaui.Views;

public partial class AppShell : Shell
{
    public AppShell()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine($"[PERF][AppShell] InitializeComponent: {sw.ElapsedMilliseconds} ms");
        AppRoutes.Register();
        System.Diagnostics.Debug.WriteLine($"[PERF][AppShell] Constructor total: {sw.ElapsedMilliseconds} ms");
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
