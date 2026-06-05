using ChozaMaui.Models;
using System.Diagnostics;

namespace ChozaMaui.Services;

public sealed class PosDataService
{
    private readonly PedidoApiService _pedidosApi;
    private readonly CuentaApiService _cuentasApi;
    private readonly PosCatalogService _catalog;
    private readonly MesaStateService _mesas;

    public PosDataService(PedidoApiService pedidosApi, CuentaApiService cuentasApi, PosCatalogService catalog, MesaStateService mesas)
    {
        var sw = Stopwatch.StartNew();
        _pedidosApi = pedidosApi;
        _cuentasApi = cuentasApi;
        _catalog = catalog;
        _mesas = mesas;
        Debug.WriteLine($"[PERF][PosDataService] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    public async Task<PosDataSnapshot> CargarDatosAsync()
    {
        var totalSw = Stopwatch.StartNew();
        var mesasTask = _mesas.ObtenerMesasAsync();
        var pedidosTask = _pedidosApi.GetPedidosAsync();
        var cuentasAbiertasTask = _cuentasApi.ObtenerCuentasAbiertasAsync();
        var categoriasTask = _catalog.ObtenerCategoriasActivasAsync();
        var productosTask = _catalog.ObtenerProductosActivosAsync();

        await Task.WhenAll(mesasTask, pedidosTask, cuentasAbiertasTask, categoriasTask, productosTask);
        var mesas = await mesasTask;
        var pedidos = await pedidosTask;
        var cuentasAbiertas = await cuentasAbiertasTask;
        var categorias = await categoriasTask;
        var productos = await productosTask;
        Debug.WriteLine($"[PERF][POS][API] Mesas: {mesas.Count} | Pedidos: {pedidos.Count} | Cuentas abiertas: {cuentasAbiertas.Count} | Categorias: {categorias.Count} | Productos: {productos.Count} | Total paralelo: {totalSw.ElapsedMilliseconds} ms");

        var mapSw = Stopwatch.StartNew();
        var mesasConCuentaAbierta = cuentasAbiertas
            .Where(c => c.Mesa?.Idmesa > 0 && string.Equals(c.Estado, CuentaEstados.Abierta, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Mesa!.Idmesa)
            .ToHashSet();

        var pedidosActivos = pedidos
            .Where(p =>
            {
                var mesaId = p.Mesa?.Idmesa ?? 0;
                if (mesaId <= 0 || !p.MantieneMesaOcupada)
                    return false;

                return !p.EsPendienteCobro || mesasConCuentaAbierta.Contains(mesaId);
            })
            .ToList();
        var mesasVisuales = mesas
            .Select(mesa => new MesaVisual
            {
                Mesa = mesa,
                TieneCuentaAbierta = mesasConCuentaAbierta.Contains(mesa.Idmesa),
                PedidosActivos = pedidosActivos.Where(p => p.Mesa?.Idmesa == mesa.Idmesa).ToList()
            })
            .ToList();
        Debug.WriteLine($"[PERF][POS] Construir mesas visuales: {mapSw.ElapsedMilliseconds} ms");

        return new PosDataSnapshot(
            mesas.ToList(),
            mesasVisuales,
            categorias.ToList(),
            productos.ToList());
    }
}

public sealed record PosDataSnapshot(
    IReadOnlyList<MesaResponse> Mesas,
    IReadOnlyList<MesaVisual> MesasVisuales,
    IReadOnlyList<CategoriaResponse> Categorias,
    IReadOnlyList<ProductoResponse> Productos);
