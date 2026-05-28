using ChozaMaui.Models;
using ChozaMaui.ViewModels;

namespace ChozaMaui.Services;

public sealed class PedidoPresentationService
{
    public PedidosPresentationSnapshot BuildList(IReadOnlyList<PedidoResponse> pedidos, string filtroEstado, string? busqueda)
    {
        var hoy = DateTime.Today;
        var visibles = pedidos
            .Where(p => p.EsActivo || p.Fecha.Date == hoy)
            .ToList();

        IEnumerable<PedidoResponse> resultado = visibles;

        resultado = filtroEstado switch
        {
            "EN_PREPARACION" => resultado.Where(p =>
                p.Estado is "EN_COCINA" or "EN_BAR" or "EN_PROCESO" or "PENDIENTE"),
            "LISTOS" => resultado.Where(p =>
                p.Estado is "LISTO_PARA_ENTREGA" or "LISTO"),
            "ENTREGADOS" => resultado.Where(p =>
                p.Estado is "COMPLETADO" or "ENTREGADO"),
            "CANCELADOS" => resultado.Where(p => p.Estado == "CANCELADO"),
            _ => resultado
        };

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var q = busqueda.Trim().ToLowerInvariant();
            resultado = resultado.Where(p =>
                p.Idpedido.ToString().Contains(q) ||
                (p.Mesa?.Etiqueta.ToLowerInvariant().Contains(q) ?? false) ||
                (p.Cliente?.Nombre.ToLowerInvariant().Contains(q) ?? false));
        }

        var lista = resultado.OrderByDescending(p => p.Fecha).ToList();

        var totalEnPreparacion = visibles.Count(p =>
            p.Estado is "EN_COCINA" or "EN_BAR" or "EN_PROCESO" or "PENDIENTE");
        var totalListos = visibles.Count(p => p.Estado is "LISTO_PARA_ENTREGA" or "LISTO");
        var totalEntregados = visibles.Count(p => p.Fecha.Date == hoy && p.Estado is ("COMPLETADO" or "ENTREGADO"));
        var totalCancelados = visibles.Count(p => p.Fecha.Date == hoy && p.Estado == "CANCELADO");

        return new PedidosPresentationSnapshot(
            lista,
            totalEnPreparacion,
            totalListos,
            totalEntregados,
            totalCancelados,
            totalEnPreparacion + totalListos);
    }

    public PedidoDetailPresentationModel BuildDetail(PedidoResponse pedido)
    {
        var estadoBadgeTexto = MapearEstadoVisual(pedido.Estado);
        var responsable = string.IsNullOrWhiteSpace(pedido.Usuario?.NombreCompleto)
            ? "Sin asignar"
            : pedido.Usuario.NombreCompleto;

        return new PedidoDetailPresentationModel(
            $"Pedido #{pedido.Idpedido}",
            $"{pedido.Mesa?.Etiqueta ?? "Mesa —"}  ·  {(string.IsNullOrWhiteSpace(pedido.Mesa?.NombreComedor) ? "Comedor" : pedido.Mesa.NombreComedor)}",
            pedido.Estado,
            pedido.EstadoBadgeColor,
            estadoBadgeTexto,
            pedido.Fecha.ToString("dd/MM/yyyy HH:mm"),
            pedido.Mesa?.Etiqueta ?? "—",
            pedido.Mesa is null ? "—" : $"{pedido.Mesa.Capacidad} personas",
            pedido.Cliente?.NombreCompleto ?? "Sin cliente",
            string.IsNullOrWhiteSpace(pedido.Cliente?.Telefono) ? "Sin teléfono" : pedido.Cliente.Telefono!,
            responsable,
            pedido.Fecha.ToString("hh:mm tt"),
            pedido.Observaciones ?? "Sin observaciones",
            pedido.Total,
            pedido.Subtotal > 0 ? pedido.Subtotal : pedido.Total,
            0,
            BuildTimeline(pedido, responsable, pedido.Estado));
    }

    public string MapearEstadoVisual(string estado) => estado switch
    {
        "PENDIENTE" => "PENDIENTE",
        "EN_COCINA" or "EN_BAR" or "EN_PROCESO" => "EN PREPARACION",
        "LISTO" or "LISTO_PARA_ENTREGA" => "LISTO PARA ENTREGA",
        "COMPLETADO" or "ENTREGADO" => "ENTREGADO",
        "CANCELADO" => "CANCELADO",
        _ => estado
    };

    private static IReadOnlyList<PedidoTimelineItem> BuildTimeline(PedidoResponse pedido, string responsable, string estado)
    {
        var historial = new List<PedidoTimelineItem>
        {
            new()
            {
                Hora = FormatearHora(pedido.Fecha),
                Evento = "Pedido creado",
                Responsable = responsable,
                DotColor = "#f59e0b",
                MostrarLinea = true
            }
        };

        var enCocina = estado is not "PENDIENTE" and not "CANCELADO";
        historial.Add(new PedidoTimelineItem
        {
            Hora = enCocina ? FormatearHora(pedido.FechaEnCocina) : "--:-- --",
            Evento = "Enviado a cocina",
            Responsable = enCocina ? responsable : string.Empty,
            DotColor = enCocina ? "#3b82f6" : "#d1d5db",
            MostrarLinea = true
        });

        var listo = estado is "LISTO" or "LISTO_PARA_ENTREGA" or "COMPLETADO" or "ENTREGADO";
        historial.Add(new PedidoTimelineItem
        {
            Hora = listo ? FormatearHora(pedido.FechaListoParaEntrega) : "--:-- --",
            Evento = "Listo para entregar",
            Responsable = listo ? responsable : string.Empty,
            DotColor = listo ? "#f59e0b" : "#d1d5db",
            MostrarLinea = true
        });

        var entregado = estado is "COMPLETADO" or "ENTREGADO";
        historial.Add(new PedidoTimelineItem
        {
            Hora = entregado ? FormatearHora(pedido.FechaEntregado) : "--:-- --",
            Evento = "Entregado al cliente",
            Responsable = entregado ? responsable : string.Empty,
            DotColor = entregado ? "#10b981" : "#d1d5db",
            MostrarLinea = false
        });

        return historial;
    }

    private static string FormatearHora(DateTime? fecha)
        => fecha.HasValue && fecha.Value != default ? fecha.Value.ToString("hh:mm tt") : "--:-- --";
}

public sealed record PedidosPresentationSnapshot(
    IReadOnlyList<PedidoResponse> Pedidos,
    int TotalEnPreparacion,
    int TotalListos,
    int TotalEntregadosHoy,
    int TotalCancelados,
    int PedidosActivos);

public sealed record PedidoDetailPresentationModel(
    string TituloPedido,
    string SubtituloPedido,
    string Estado,
    string EstadoColor,
    string EstadoBadgeTexto,
    string FechaTexto,
    string MesaTexto,
    string MesaCapacidadTexto,
    string ClienteTexto,
    string ClienteTelefonoTexto,
    string MeseroTexto,
    string MeseroHoraTexto,
    string Observaciones,
    double Total,
    double Subtotal,
    double Impuestos,
    IReadOnlyList<PedidoTimelineItem> Historial);
