namespace ChozaMaui.Views;

public partial class AppShellCajero : Shell
{
    public AppShellCajero()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine($"[PERF][AppShellCajero] InitializeComponent: {sw.ElapsedMilliseconds} ms");
        AppRoutes.Register();
        System.Diagnostics.Debug.WriteLine($"[PERF][AppShellCajero] Constructor total: {sw.ElapsedMilliseconds} ms");
    }
}
