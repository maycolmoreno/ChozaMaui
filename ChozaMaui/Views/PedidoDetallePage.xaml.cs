using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class PedidoDetallePage : ContentPage
{
    public PedidoDetallePage(PedidoDetalleViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
