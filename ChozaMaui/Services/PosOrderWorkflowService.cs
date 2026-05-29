using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class PosOrderWorkflowService
{
    private static readonly TimeSpan PedidoCacheTtl = TimeSpan.FromSeconds(10);
    private readonly PedidoApiService _pedidosApi;
    private readonly ProductoApiService _productosApi;
    private readonly ConnectivityService _connectivity;
    private readonly MesaStateService _mesas;
    private readonly PosCatalogService _catalog;
    private readonly OrderWorkflowService _orderWorkflow;
    private readonly PendingOrderService _pendingOrders;
    private readonly SessionCacheService _cache;

    public PosOrderWorkflowService(
        PedidoApiService pedidosApi,
        ProductoApiService productosApi,
        ConnectivityService connectivity,
        MesaStateService mesas,
        PosCatalogService catalog,
        OrderWorkflowService orderWorkflow,
        PendingOrderService pendingOrders,
        SessionCacheService cache)
    {
        _pedidosApi = pedidosApi;
        _productosApi = productosApi;
        _connectivity = connectivity;
        _mesas = mesas;
        _catalog = catalog;
        _orderWorkflow = orderWorkflow;
        _pendingOrders = pendingOrders;
        _cache = cache;
    }

    public async Task<PosOrderSubmissionResult> SubmitPedidoAsync(PedidoRequest request, string estadoDestino)
    {
        if (!_connectivity.IsOnline)
            return await EncolarAsync(request, estadoDestino);

        try
        {
            var resultado = await _orderWorkflow.SubmitPedidoAsync(request, estadoDestino);
            await InvalidarCachesTrasCrearPedidoAsync(request.IdMesa);
            await GuardarPedidoEnCacheAsync(resultado.Pedido);
            return PosOrderSubmissionResult.Submitted(resultado.Pedido, resultado.VinculoCuentaAdvertencia);
        }
        catch (Exception ex) when (PendingOrderService.IsRecoverable(ex))
        {
            await _connectivity.RefreshStatusAsync();
            return await EncolarAsync(request, estadoDestino);
        }
    }

    public Task<PedidoResponse?> ObtenerPedidoEnCursoAsync(MesaResponse mesa, int pedidoId)
        => _cache.GetOrCreateAsync(
            BuildPedidoMesaKey(mesa.Idmesa),
            PedidoCacheTtl,
            () => ObtenerPedidoEnCursoDesdeApiAsync(mesa, pedidoId));

    public Task<PedidoResponse> ObtenerPedidoDetalleAsync(int pedidoId)
        => _cache.GetOrCreateAsync(
            BuildPedidoDetalleKey(pedidoId),
            PedidoCacheTtl,
            () => _pedidosApi.GetPedidoPorIdAsync(pedidoId));

    public async Task<PedidoResponse> CambiarEstadoPedidoAsync(PedidoResponse pedido, string estado)
    {
        var actualizado = await _pedidosApi.CambiarEstadoPedidoAsync(pedido.Idpedido, estado);
        await InvalidarPedidoCacheAsync(pedido);
        await InvalidarCachesTrasCerrarPedidoAsync(actualizado.Mesa, actualizado.Estado);
        await GuardarPedidoEnCacheAsync(actualizado);
        await InvalidarMesasSiPedidoFinalizoAsync(actualizado.Mesa, actualizado.Estado);
        return actualizado;
    }

    public async Task<PedidoResponse> CambiarEstadoPedidoAsync(int pedidoId, string estado)
    {
        var pedidoAnterior = await ObtenerPedidoDetalleAsync(pedidoId);
        var actualizado = await _pedidosApi.CambiarEstadoPedidoAsync(pedidoId, estado);
        await InvalidarPedidoCacheAsync(pedidoAnterior);
        await InvalidarCachesTrasCerrarPedidoAsync(actualizado.Mesa, actualizado.Estado);
        await GuardarPedidoEnCacheAsync(actualizado);
        await InvalidarMesasSiPedidoFinalizoAsync(actualizado.Mesa, actualizado.Estado);
        return actualizado;
    }

    public async Task<string?> ValidarStockCarritoAsync(IEnumerable<ItemCarrito> carrito)
    {
        try
        {
            var productosActuales = await _productosApi.GetProductosActivosAsync();
            foreach (var item in carrito)
            {
                var productoActual = productosActuales
                    .FirstOrDefault(p => p.Idproducto == item.Producto.Idproducto);

                if (productoActual is null || !productoActual.Estado)
                    return $"{item.Producto.Nombre} ya no está disponible.";

                if (item.Cantidad > productoActual.StockActual)
                    return $"Stock insuficiente para {productoActual.Nombre}. Disponible: {productoActual.StockActual}.";
            }

            return null;
        }
        catch (Exception ex)
        {
            return $"No se pudo validar el stock: {ex.Message}";
        }
    }

    private async Task<PosOrderSubmissionResult> EncolarAsync(PedidoRequest request, string estadoDestino)
    {
        var totalPendientes = await _pendingOrders.EnqueueAsync(request, estadoDestino);
        return PosOrderSubmissionResult.Queued(totalPendientes);
    }

    private async Task InvalidarMesasSiPedidoFinalizoAsync(MesaResponse? mesa, string estadoPedido)
    {
        if (mesa is null || estadoPedido is not (PedidoEstados.Cancelado or PedidoEstados.Completado or PedidoEstados.Entregado))
            return;

        await _mesas.InvalidarAsync();
    }

    private async Task InvalidarCachesTrasCrearPedidoAsync(int mesaId)
    {
        await _catalog.InvalidarAsync();
        await _mesas.InvalidarAsync();
        await _cache.RemoveAsync(BuildCuentaMesaKey(mesaId));
        await _cache.RemoveAsync(BuildPedidoMesaKey(mesaId));
    }

    private async Task InvalidarCachesTrasCerrarPedidoAsync(MesaResponse? mesa, string estadoPedido)
    {
        if (mesa is null)
            return;

        await _cache.RemoveAsync(BuildCuentaMesaKey(mesa.Idmesa));

        if (estadoPedido is PedidoEstados.Cancelado or PedidoEstados.Completado or PedidoEstados.Entregado)
            await _cache.RemoveAsync(BuildPedidoMesaKey(mesa.Idmesa));
    }

    private async Task<PedidoResponse?> ObtenerPedidoEnCursoDesdeApiAsync(MesaResponse mesa, int pedidoId)
    {
        PedidoResponse? pedidoCompleto = null;

        if (pedidoId > 0)
        {
            var pedidoExacto = await ObtenerPedidoDetalleAsync(pedidoId);
            if (pedidoExacto.Mesa?.Idmesa == mesa.Idmesa)
                pedidoCompleto = pedidoExacto;
        }

        if (pedidoCompleto is not null)
            return pedidoCompleto;

        var pedidos = await _pedidosApi.GetPedidosAsync();
        var pedidoBase = pedidos
            .Where(p => p.EsActivo && p.Mesa?.Idmesa == mesa.Idmesa)
            .OrderByDescending(p => p.Fecha)
            .FirstOrDefault();

        return pedidoBase is not null
            ? await ObtenerPedidoDetalleAsync(pedidoBase.Idpedido)
            : null;
    }

    private async Task InvalidarPedidoCacheAsync(PedidoResponse? pedido)
    {
        if (pedido is null)
            return;

        await _cache.RemoveAsync(BuildPedidoDetalleKey(pedido.Idpedido));

        if (pedido.Mesa is not null)
            await _cache.RemoveAsync(BuildPedidoMesaKey(pedido.Mesa.Idmesa));
    }

    private async Task GuardarPedidoEnCacheAsync(PedidoResponse pedido)
    {
        await _cache.SetAsync(BuildPedidoDetalleKey(pedido.Idpedido), pedido, PedidoCacheTtl);

        if (pedido.Mesa is null)
            return;

        if (pedido.EsActivo)
            await _cache.SetAsync<PedidoResponse?>(BuildPedidoMesaKey(pedido.Mesa.Idmesa), pedido, PedidoCacheTtl);
        else
            await _cache.RemoveAsync(BuildPedidoMesaKey(pedido.Mesa.Idmesa));
    }

    private static string BuildCuentaMesaKey(int mesaId) => $"cuenta:mesa:{mesaId}";
    private static string BuildPedidoMesaKey(int mesaId) => $"pedido:mesa:{mesaId}";
    private static string BuildPedidoDetalleKey(int pedidoId) => $"pedido:detalle:{pedidoId}";
}

public sealed record PosOrderSubmissionResult(PedidoResponse? Pedido, bool SeEncoloOffline, int Pendientes, string? VinculoCuentaAdvertencia)
{
    public static PosOrderSubmissionResult Submitted(PedidoResponse pedido, string? vinculoCuentaAdvertencia)
        => new(pedido, false, 0, vinculoCuentaAdvertencia);

    public static PosOrderSubmissionResult Queued(int pendientes)
        => new(null, true, pendientes, null);
}
