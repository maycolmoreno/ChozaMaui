using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class PosCatalogService
{
    private static readonly TimeSpan CatalogCacheTtl = TimeSpan.FromMinutes(2);
    private readonly ProductoApiService _productosApi;
    private readonly SessionCacheService _cache;

    public PosCatalogService(ProductoApiService productosApi, SessionCacheService cache)
    {
        _productosApi = productosApi;
        _cache = cache;
    }

    public Task<IReadOnlyList<ProductoResponse>> ObtenerProductosActivosAsync()
        => _cache.GetOrCreateAsync(
            "pos:productos:activos",
            CatalogCacheTtl,
            () => ObtenerProductosAsync(() => _productosApi.GetProductosActivosAsync()));

    public Task<IReadOnlyList<CategoriaResponse>> ObtenerCategoriasActivasAsync()
        => _cache.GetOrCreateAsync(
            "pos:categorias:activas",
            CatalogCacheTtl,
            () => ObtenerCategoriasAsync(() => _productosApi.GetCategoriasActivasAsync()));

    public Task<IReadOnlyList<ProductoResponse>> ObtenerProductosPorCategoriaAsync(int categoriaId)
        => _cache.GetOrCreateAsync(
            $"pos:productos:categoria:{categoriaId}",
            CatalogCacheTtl,
            () => ObtenerProductosAsync(() => _productosApi.GetProductosPorCategoriaAsync(categoriaId)));

    public IReadOnlyList<ProductoResponse> FiltrarProductos(
        IEnumerable<ProductoResponse> productos,
        string? busqueda)
    {
        var query = busqueda?.Trim();
        if (string.IsNullOrWhiteSpace(query))
            return productos.ToList();

        return productos
            .Where(producto =>
                producto.Nombre.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                producto.Descripcion.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task InvalidarAsync()
    {
        await _cache.RemoveByPrefixAsync("pos:productos:");
        await _cache.RemoveByPrefixAsync("pos:categorias:");
    }

    private static async Task<IReadOnlyList<ProductoResponse>> ObtenerProductosAsync(
        Func<Task<List<ProductoResponse>>> fetchAsync)
    {
        var productos = await fetchAsync();
        return productos;
    }

    private static async Task<IReadOnlyList<CategoriaResponse>> ObtenerCategoriasAsync(
        Func<Task<List<CategoriaResponse>>> fetchAsync)
    {
        var categorias = await fetchAsync();
        return categorias;
    }
}