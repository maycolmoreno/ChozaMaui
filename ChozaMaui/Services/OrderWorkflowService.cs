using ChozaMaui.Models;

namespace ChozaMaui.Services;

public class OrderWorkflowService
{
    private readonly PedidoApiService _pedidosApi;

    public OrderWorkflowService(PedidoApiService pedidosApi)
    {
        _pedidosApi = pedidosApi;
    }

    public async Task<OrderSubmissionResult> SubmitPedidoAsync(PedidoRequest request, string estadoDestino)
    {
        var pedido = await _pedidosApi.CrearPedidoConCuentaAsync(request, estadoDestino);
        return new OrderSubmissionResult(pedido, null);
    }
}

public sealed record OrderSubmissionResult(PedidoResponse Pedido, string? VinculoCuentaAdvertencia);