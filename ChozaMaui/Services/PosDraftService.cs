using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class PosDraftService
{
    public string? ValidarPedido(
        MesaResponse? mesa,
        IReadOnlyCollection<ItemCarrito> carrito,
        ClienteResponse? cliente)
    {
        if (mesa is null)
            return "Selecciona una mesa antes de enviar.";

        if (carrito.Count == 0)
            return "El carrito está vacío.";

        if (cliente is null)
            return "Asigna un cliente antes de enviar el pedido.";

        return null;
    }

    public PedidoRequest CrearPedidoRequest(
        int userId,
        MesaResponse mesa,
        ClienteResponse cliente,
        IEnumerable<ItemCarrito> carrito,
        string observaciones,
        FotoAdjunta? fotoAdjunta)
    {
        return new PedidoRequest
        {
            Fecha = DateTime.Now.ToString("s"),
            IdUsuario = userId,
            IdMesa = mesa.Idmesa,
            IdCliente = cliente.Idcliente,
            Observaciones = ConstruirObservaciones(observaciones, fotoAdjunta),
            Detalles = carrito.Select(item => new PedidoDetalleRequest
            {
                IdProducto = item.Producto.Idproducto,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.Producto.Precio
            }).ToList()
        };
    }

    private static string ConstruirObservaciones(string observaciones, FotoAdjunta? fotoAdjunta)
    {
        if (fotoAdjunta is null)
            return observaciones;

        return string.IsNullOrWhiteSpace(observaciones)
            ? $"[Foto: {fotoAdjunta.NombreArchivo}]"
            : $"{observaciones} [Foto: {fotoAdjunta.NombreArchivo}]";
    }
}