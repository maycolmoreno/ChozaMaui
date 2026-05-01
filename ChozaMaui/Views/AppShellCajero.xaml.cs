namespace ChozaMaui.Views;

public partial class AppShellCajero : Shell
{
    public AppShellCajero()
    {
        InitializeComponent();
        Routing.RegisterRoute("pedidodetalle", typeof(PedidoDetallePage));
        Routing.RegisterRoute("pago", typeof(PagoPage));
    }
}
