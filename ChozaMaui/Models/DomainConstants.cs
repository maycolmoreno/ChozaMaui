namespace ChozaMaui.Models;

public static class PedidoEstados
{
    public const string Pendiente = "PENDIENTE";
    public const string EnCocina = "EN_COCINA";
    public const string EnBar = "EN_BAR";
    public const string EnProceso = "EN_PROCESO";
    public const string Listo = "LISTO";
    public const string ListoParaEntrega = "LISTO_PARA_ENTREGA";
    public const string Completado = "COMPLETADO";
    public const string Entregado = "ENTREGADO";
    public const string Cancelado = "CANCELADO";
}

public static class CuentaEstados
{
    public const string Abierta = "ABIERTA";
    public const string Pagada = "PAGADA";
    public const string Anulada = "ANULADA";
}

public static class MetodosPago
{
    public const string Efectivo = "EFECTIVO";
    public const string Tarjeta = "TARJETA";
    public const string Transferencia = "TRANSFERENCIA";
    public const string Otro = "OTRO";
}
