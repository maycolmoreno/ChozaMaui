using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class MapaPresentationService
{
    public MapaPresentationSnapshot Build(IReadOnlyList<MesaResponse> mesas, IReadOnlyList<PedidoResponse> pedidos)
    {
        var pedidosActivos = pedidos
            .Where(p => p.EsActivo && p.Mesa is not null)
            .ToList();

        var mesasVisuales = mesas
            .Select(mesa => new MesaVisual
            {
                Mesa = mesa,
                PedidosActivos = pedidosActivos
                    .Where(p => p.Mesa?.Idmesa == mesa.Idmesa)
                    .OrderByDescending(p => p.Fecha)
                    .ToList()
            })
            .ToList();

        var grupos = mesasVisuales
            .GroupBy(m => m.NombreComedor)
            .Select(g => new GrupoMesaVisual(g.Key, g.OrderBy(m => m.Numero)))
            .OrderBy(g => g.Nombre)
            .ToList();

        return new MapaPresentationSnapshot(
            grupos,
            mesasVisuales.Count(m => m.EstadoVisual == "Disponible"),
            mesasVisuales.Count(m => m.EstadoVisual == "Ocupada"),
            mesasVisuales.Count(m => m.EstadoVisual == "En preparacion"),
            mesasVisuales.Count(m => m.EstadoVisual == "Pendiente de pago"));
    }

    public string BuildSheetPedidoResumen(PedidoResponse? pedido)
    {
        if (pedido?.Detalle is null || pedido.Detalle.Count == 0)
            return pedido is null ? string.Empty : "Sin detalle disponible";

        return string.Join("\n", pedido.Detalle.Select(item =>
            $"{item.Cantidad}x {item.Producto?.Nombre ?? "Producto"}"));
    }
}

public sealed record MapaPresentationSnapshot(
    IReadOnlyList<GrupoMesaVisual> Grupos,
    int TotalDisponibles,
    int TotalOcupadas,
    int TotalEnPreparacion,
    int TotalPendientePago);