using ChozaMaui.Models;

namespace ChozaMaui.Services;

public class OrderWorkflowService
{
    private readonly PedidoApiService _pedidosApi;
    private readonly PedidoCuentaWorkflowService _pedidoCuentaWorkflow;

    public OrderWorkflowService(PedidoApiService pedidosApi, PedidoCuentaWorkflowService pedidoCuentaWorkflow)
    {
        _pedidosApi = pedidosApi;
        _pedidoCuentaWorkflow = pedidoCuentaWorkflow;
    }

    public async Task<PedidoResponse> SubmitPedidoAsync(PedidoRequest request, string estadoDestino)
    {
        var pedido = await _pedidosApi.CrearPedidoAsync(request);

        await TryAttachPedidoToCuentaAsync(request, pedido);

        if (!string.Equals(estadoDestino, "PENDIENTE", StringComparison.OrdinalIgnoreCase))
            pedido = await _pedidosApi.CambiarEstadoPedidoAsync(pedido.Idpedido, estadoDestino);

        return pedido;
    }

    private async Task TryAttachPedidoToCuentaAsync(PedidoRequest request, PedidoResponse pedido)
    {
        try
        {
            var resolucion = await _pedidoCuentaWorkflow.ResolverCuentaAsync(
                request.IdMesa,
                request.IdCliente,
                pedido.Total);

            await _pedidoCuentaWorkflow.AsegurarPedidoEnCuentaAsync(resolucion.Cuenta, pedido.Idpedido);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            // Algunos roles pueden crear/enviar pedidos pero no mutar cuentas.
            // El pedido queda creado y se vincula a la cuenta al momento del cobro.
        }
    }
}