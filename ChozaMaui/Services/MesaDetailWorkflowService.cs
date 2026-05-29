using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class MesaDetailWorkflowService
{
    private readonly PedidoApiService _pedidosApi;
    private readonly MesaStateService _mesas;
    private readonly PosOrderWorkflowService _pedidoWorkflow;

    public MesaDetailWorkflowService(PedidoApiService pedidosApi, MesaStateService mesas, PosOrderWorkflowService pedidoWorkflow)
    {
        _pedidosApi = pedidosApi;
        _mesas = mesas;
        _pedidoWorkflow = pedidoWorkflow;
    }

    public async Task<MesaDetailSnapshot> CargarAsync(MesaResponse mesa)
    {
        var pedidos = await _pedidosApi.GetPedidosAsync();
        var activosMesa = pedidos
            .Where(p => p.EsActivo && p.Mesa?.Idmesa == mesa.Idmesa)
            .OrderBy(p => p.Fecha)
            .ToList();

        var pedidosListos = activosMesa
            .Where(p => p.EstaListoParaEntrega)
            .ToList();

        return new MesaDetailSnapshot(
            activosMesa,
            pedidosListos,
            activosMesa.Sum(p => p.Total),
            CalcularTiempoMesa(activosMesa));
    }

    public Task<PedidoResponse> EntregarPedidoAsync(PedidoResponse pedido)
        => _pedidoWorkflow.CambiarEstadoPedidoAsync(pedido, PedidoEstados.Completado);

    public async Task EntregarPedidosAsync(IEnumerable<PedidoResponse> pedidos)
    {
        foreach (var pedido in pedidos)
            await _pedidoWorkflow.CambiarEstadoPedidoAsync(pedido, PedidoEstados.Completado);
    }

    public Task<MesaResponse> CerrarMesaAsync(MesaResponse mesa)
        => _mesas.ActualizarEstadoMesaAsync(mesa, true);

    private static string CalcularTiempoMesa(List<PedidoResponse> pedidos)
    {
        if (pedidos.Count == 0)
            return "0 min";

        var primerPedido = pedidos.Min(p => p.Fecha);
        var diff = DateTime.Now - primerPedido;
        if (diff.TotalHours >= 1)
            return $"{(int)diff.TotalHours}h {diff.Minutes:D2}m";

        return $"{Math.Max(1, (int)diff.TotalMinutes)} min";
    }
}

public sealed record MesaDetailSnapshot(
    IReadOnlyList<PedidoResponse> PedidosActivos,
    IReadOnlyList<PedidoResponse> PedidosListos,
    double TotalMesa,
    string TiempoMesaTexto);
