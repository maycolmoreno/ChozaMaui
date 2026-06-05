using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class PedidoDetallePage : ContentPage
{
    public PedidoDetallePage(PedidoDetalleViewModel vm)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        BindingContext = vm;
        System.Diagnostics.Debug.WriteLine($"[PERF][PedidoDetallePage] Constructor: {sw.ElapsedMilliseconds} ms");
    }
}
