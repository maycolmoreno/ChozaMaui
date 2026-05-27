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
        var result = await _http.GetFromJsonAsync<List<CategoriaResponse>>("/api/categorias");
        return result?.Where(c => c.Estado).ToList() ?? [];
    }

    public async Task<List<CategoriaResponse>> GetTodasCategoriasAsync()
        => await _http.GetFromJsonAsync<List<CategoriaResponse>>("/api/categorias") ?? [];

    public async Task<CategoriaResponse> CrearCategoriaAsync(CategoriaRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/categorias", req, _camelCase);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CategoriaResponse>())!;
    }

    public async Task<CategoriaResponse> ActualizarCategoriaAsync(int id, CategoriaRequest req)
    {
        var r = await _http.PutAsJsonAsync($"/api/categorias/{id}", req, _camelCase);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CategoriaResponse>())!;
    }

    public async Task EliminarCategoriaAsync(int id)
    {
        var r = await _http.DeleteAsync($"/api/categorias/{id}");
        r.EnsureSuccessStatusCode();
    }

    // ── Productos ─────────────────────────────────────────────────
    public async Task<List<ProductoResponse>> GetProductosActivosAsync()
        => await _http.GetFromJsonAsync<List<ProductoResponse>>("/api/productos/activos") ?? [];

    public async Task<List<ProductoResponse>> GetProductosPorCategoriaAsync(int idCategoria)
    {
        var result = await _http.GetFromJsonAsync<List<ProductoResponse>>($"/api/productos/categoria/{idCategoria}");
        return result?.Where(p => p.Estado).ToList() ?? [];
    }

    public async Task<List<ProductoResponse>> GetTodosProductosAsync()
        => await _http.GetFromJsonAsync<List<ProductoResponse>>("/api/productos") ?? [];

    public async Task<ProductoResponse> CrearProductoAsync(ProductoRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/productos", req, _camelCase);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ProductoResponse>())!;
    }

    public async Task<ProductoResponse> ActualizarProductoAsync(int id, ProductoRequest req)
    {
        var r = await _http.PutAsJsonAsync($"/api/productos/{id}", req, _camelCase);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ProductoResponse>())!;
    }

    public async Task EliminarProductoAsync(int id)
    {
        var r = await _http.DeleteAsync($"/api/productos/{id}");
        r.EnsureSuccessStatusCode();
    }
}
