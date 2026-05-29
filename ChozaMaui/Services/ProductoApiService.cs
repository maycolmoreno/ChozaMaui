using System.Net.Http.Json;
using System.Text.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Operaciones de API para Categorías y Productos.
/// </summary>
public class ProductoApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _camelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ProductoApiService(HttpClient http) => _http = http;

    // ── Categorías ────────────────────────────────────────────────
    public async Task<List<CategoriaResponse>> GetCategoriasActivasAsync()
    {
        var r = await _http.GetAsync("/api/categorias");
        await ApiErrorHelper.EnsureSuccessAsync(r);
        var result = await r.Content.ReadFromJsonAsync<List<CategoriaResponse>>();
        return result?.Where(c => c.Estado).ToList() ?? [];
    }

    public async Task<List<CategoriaResponse>> GetTodasCategoriasAsync()
    {
        var r = await _http.GetAsync("/api/categorias");
        await ApiErrorHelper.EnsureSuccessAsync(r);
        return (await r.Content.ReadFromJsonAsync<List<CategoriaResponse>>()) ?? [];
    }

    public async Task<CategoriaResponse> CrearCategoriaAsync(CategoriaRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/categorias", req, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(r);
        return (await r.Content.ReadFromJsonAsync<CategoriaResponse>())!;
    }

    public async Task<CategoriaResponse> ActualizarCategoriaAsync(int id, CategoriaRequest req)
    {
        var r = await _http.PutAsJsonAsync($"/api/categorias/{id}", req, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(r);
        return (await r.Content.ReadFromJsonAsync<CategoriaResponse>())!;
    }

    public async Task EliminarCategoriaAsync(int id)
    {
        var r = await _http.DeleteAsync($"/api/categorias/{id}");
        await ApiErrorHelper.EnsureSuccessAsync(r);
    }

    // ── Productos ─────────────────────────────────────────────────
    public async Task<List<ProductoResponse>> GetProductosActivosAsync()
    {
        var r = await _http.GetAsync("/api/productos/activos");
        await ApiErrorHelper.EnsureSuccessAsync(r);
        return (await r.Content.ReadFromJsonAsync<List<ProductoResponse>>()) ?? [];
    }

    public async Task<List<ProductoResponse>> GetProductosPorCategoriaAsync(int idCategoria)
    {
        var r = await _http.GetAsync($"/api/productos/categoria/{idCategoria}");
        await ApiErrorHelper.EnsureSuccessAsync(r);
        var result = await r.Content.ReadFromJsonAsync<List<ProductoResponse>>();
        return result?.Where(p => p.Estado).ToList() ?? [];
    }

    public async Task<List<ProductoResponse>> GetTodosProductosAsync()
    {
        var r = await _http.GetAsync("/api/productos");
        await ApiErrorHelper.EnsureSuccessAsync(r);
        return (await r.Content.ReadFromJsonAsync<List<ProductoResponse>>()) ?? [];
    }

    public async Task<ProductoResponse> CrearProductoAsync(ProductoRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/productos", req, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(r);
        return (await r.Content.ReadFromJsonAsync<ProductoResponse>())!;
    }

    public async Task<ProductoResponse> ActualizarProductoAsync(int id, ProductoRequest req)
    {
        var r = await _http.PutAsJsonAsync($"/api/productos/{id}", req, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(r);
        return (await r.Content.ReadFromJsonAsync<ProductoResponse>())!;
    }

    public async Task EliminarProductoAsync(int id)
    {
        var r = await _http.DeleteAsync($"/api/productos/{id}");
        await ApiErrorHelper.EnsureSuccessAsync(r);
    }
}
