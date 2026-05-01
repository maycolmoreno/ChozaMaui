namespace ChozaMaui.Views;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        // Ruta para detalle de pedido (pantalla 5)
        Routing.RegisterRoute("pedidodetalle", typeof(PedidoDetallePage));
        // Ruta para detalle de mesa
        Routing.RegisterRoute("mesadetalle", typeof(MesaDetallePage));
        // Ruta para pago / facturación
        Routing.RegisterRoute("pago", typeof(PagoPage));
    }
}
