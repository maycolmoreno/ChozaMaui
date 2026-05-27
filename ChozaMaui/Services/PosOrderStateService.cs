using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class PosOrderStateService
{
    public PosOrderStateSnapshot CrearSnapshot(PedidoResponse? pedido)
    {
        var carrito = (pedido?.Detalle ?? [])
            .Where(detalle => detalle.Producto is not null)
            .Select(detalle => new ItemCarrito
            {
                Producto = detalle.Producto!,
                Cantidad = detalle.Cantidad
            })
            .ToList();

        return new PosOrderStateSnapshot(
            pedido,
            pedido?.Idpedido ?? 0,
            carrito,
            pedido?.Cliente,
            pedido?.Observaciones ?? string.Empty,
            pedido?.Total ?? carrito.Sum(item => item.Subtotal));
    }

    public PosOrderStateSnapshot CrearSnapshotVacio()
        => new(null, 0, [], null, string.Empty, 0);
}

public sealed record PosOrderStateSnapshot(
    PedidoResponse? Pedido,
    int PedidoId,
    IReadOnlyList<ItemCarrito> Carrito,
    ClienteResponse? Cliente,
    string Observaciones,
    double Total);