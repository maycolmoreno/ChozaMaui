using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class PosDataService
{
    private readonly PedidoApiService _pedidosApi;
    private readonly PosCatalogService _catalog;
    private readonly MesaStateService _mesas;

    public PosDataService(PedidoApiService pedidosApi, PosCatalogService catalog, MesaStateService mesas)
    {
        _pedidosApi = pedidosApi;
        _catalog = catalog;
        _mesas = mesas;
    }

    public async Task<PosDataSnapshot> CargarDatosAsync()
    {
        var mesasTask = _mesas.ObtenerMesasAsync();
        var pedidosTask = _pedidosApi.GetPedidosAsync();
        var categoriasTask = _catalog.ObtenerCategoriasActivasAsync();
        var productosTask = _catalog.ObtenerProductosActivosAsync();

        await Task.WhenAll(mesasTask, pedidosTask, categoriasTask, productosTask);

        var pedidosActivos = pedidosTask.Result.Where(p => p.EsActivo).ToList();
        var mesasVisuales = mesasTask.Result
            .Select(mesa => new MesaVisual
            {
                Mesa = mesa,
                PedidosActivos = pedidosActivos.Where(p => p.Mesa?.Idmesa == mesa.Idmesa).ToList()
            })
            .ToList();

        return new PosDataSnapshot(
            mesasTask.Result.ToList(),
            mesasVisuales,
            categoriasTask.Result.ToList(),
            productosTask.Result.ToList());
    }
}

public sealed record PosDataSnapshot(
    IReadOnlyList<MesaResponse> Mesas,
    IReadOnlyList<MesaVisual> MesasVisuales,
    IReadOnlyList<CategoriaResponse> Categorias,
    IReadOnlyList<ProductoResponse> Productos);