using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class HistorialCuentasCobroService
{
    private static readonly TimeSpan PedidoRecienteCacheTtl = TimeSpan.FromSeconds(15);
    private readonly PedidoApiService _pedidos;
    private readonly SessionCacheService _cache;

    public HistorialCuentasCobroService(PedidoApiService pedidos, SessionCacheService cache)
    {
        _pedidos = pedidos;
        _cache = cache;
    }

    public Task<PedidoResponse> ObtenerPedidoParaCobroAsync(CuentaResponse cuenta)
        => _cache.GetOrCreateAsync(
            BuildPedidoCuentaKey(cuenta.Idcuenta),
            PedidoRecienteCacheTtl,
            () => _pedidos.GetPedidoRecientePorCuentaAsync(cuenta.Idcuenta));

    private static string BuildPedidoCuentaKey(int idCuenta) => $"pedido:cuenta:{idCuenta}";
}