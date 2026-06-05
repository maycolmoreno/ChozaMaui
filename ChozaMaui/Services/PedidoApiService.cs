using System.Net.Http.Json;
using System.Text.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Operaciones de API para Pedidos.
/// </summary>
public class PedidoApiService
{
    private static readonly TimeSpan PedidosCacheTtl = TimeSpan.FromSeconds(8);
    private const string PedidosCacheKey = "api:pedidos:all";

    private readonly HttpClient _http;
    private readonly SessionService _session;
    private readonly SessionCacheService _cache;
    private static readonly JsonSerializerOptions _camelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PedidoApiService(HttpClient http, SessionService session, SessionCacheService cache)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _http = http;
        _session = session;
        _cache = cache;
        System.Diagnostics.Debug.WriteLine($"[PERF][PedidoApiService] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    public async Task<List<PedidoResponse>> GetPedidosAsync()
        => await _cache.GetOrCreateAsync(
            PedidosCacheKey,
            PedidosCacheTtl,
            GetPedidosDesdeApiAsync);

    private async Task<List<PedidoResponse>> GetPedidosDesdeApiAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await _http.GetAsync("/api/pedidos");
        System.Diagnostics.Debug.WriteLine($"[HTTP][Pedido] GET /api/pedidos status: {(int)response.StatusCode} en {sw.ElapsedMilliseconds} ms");
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<List<PedidoResponse>>()) ?? [];
    }

    public async Task<PedidoResponse> GetPedidoPorIdAsync(int id)
    {
        var response = await _http.GetAsync($"/api/pedidos/{id}");
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }

    public async Task<List<PedidoHistorialResponse>> ObtenerHistorialPedidoAsync(int id)
    {
        var response = await _http.GetAsync($"/api/pedidos/{id}/historial");
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<List<PedidoHistorialResponse>>()) ?? [];
    }

    public async Task<PedidoResponse> GetPedidoRecientePorCuentaAsync(int idCuenta)
    {
        var response = await _http.GetAsync($"/api/pedidos/cuenta/{idCuenta}/reciente");
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }

    public async Task<PedidoResponse> CrearPedidoAsync(PedidoRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/pedidos", request, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(response);
        var pedido = (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
        await InvalidarCachePedidosAsync();
        return pedido;
    }

    public async Task<PedidoResponse> CrearPedidoConCuentaAsync(PedidoRequest request, string estadoDestino)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var estado = Uri.EscapeDataString((estadoDestino ?? string.Empty).Trim().ToUpperInvariant());
        try
        {
            var response = await _http.PostAsJsonAsync($"/api/pedidos/con-cuenta?estadoDestino={estado}", request, _camelCase);
            System.Diagnostics.Debug.WriteLine($"[HTTP][Pedido] Crear con cuenta status: {(int)response.StatusCode} en {sw.ElapsedMilliseconds} ms");
            await ApiErrorHelper.EnsureSuccessAsync(response);
            var pedido = (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
            await InvalidarCachePedidosAsync();
            return pedido;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR][Pedido] Crear con cuenta: {ex.GetType().Name} | {ApiErrorHelper.ToUserMessage(ex, "enviar pedido")}");
            throw;
        }
    }

    public async Task<PedidoResponse> CambiarEstadoPedidoAsync(int id, string nuevoEstado)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var estado = (nuevoEstado ?? string.Empty).Trim().ToUpperInvariant();
        var rutaSemantica = estado switch
        {
            PedidoEstados.EnCocina => string.Equals(_session.Rol, "COCINA", StringComparison.OrdinalIgnoreCase)
                ? $"/api/pedidos/{id}/preparando"
                : $"/api/pedidos/{id}/confirmar",
            PedidoEstados.Listo or PedidoEstados.ListoParaEntrega => $"/api/pedidos/{id}/listo",
            PedidoEstados.Completado or PedidoEstados.Entregado => $"/api/pedidos/{id}/entregado",
            PedidoEstados.Cancelado => $"/api/pedidos/{id}/cancelar",
            _ => null
        };

        try
        {
            HttpResponseMessage response;
            if (rutaSemantica is not null)
            {
                response = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Patch, rutaSemantica));
            }
            else
            {
                response = await _http.PatchAsJsonAsync(
                    $"/api/pedidos/{id}/estado",
                    new CambiarEstadoRequest { Estado = estado },
                    _camelCase);
            }

            System.Diagnostics.Debug.WriteLine($"[HTTP][Pedido] Cambiar estado status: {(int)response.StatusCode} estado={estado} en {sw.ElapsedMilliseconds} ms");
            await ApiErrorHelper.EnsureSuccessAsync(response);
            var pedido = (await response.Content.ReadFromJsonAsync<PedidoResponse>())!;
            await InvalidarCachePedidosAsync();
            return pedido;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR][Pedido] Cambiar estado: {ex.GetType().Name} | {ApiErrorHelper.ToUserMessage(ex, "cambiar estado del pedido")}");
            throw;
        }
    }

    public Task InvalidarCachePedidosAsync()
        => _cache.RemoveAsync(PedidosCacheKey);
}
