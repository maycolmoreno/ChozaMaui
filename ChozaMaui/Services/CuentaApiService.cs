using System.Net.Http.Json;
using System.Text.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Operaciones de API para Cuentas.
/// </summary>
public class CuentaApiService
{
    private static readonly TimeSpan CuentasAbiertasCacheTtl = TimeSpan.FromSeconds(8);
    private const string CuentasAbiertasCacheKey = "api:cuentas:abiertas";

    private readonly HttpClient _http;
    private readonly SessionCacheService _cache;
    private static readonly JsonSerializerOptions _camelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public CuentaApiService(HttpClient http, SessionCacheService cache)
    {
        _http = http;
        _cache = cache;
    }

    public async Task<List<CuentaResponse>> GetTodasCuentasAsync()
    {
        var r = await _http.GetAsync("/api/cuentas");
        await ApiErrorHelper.EnsureSuccessAsync(r);
        return (await r.Content.ReadFromJsonAsync<List<CuentaResponse>>()) ?? [];
    }

    public async Task<List<CuentaResponse>> ObtenerCuentasAbiertasAsync()
        => await _cache.GetOrCreateAsync(
            CuentasAbiertasCacheKey,
            CuentasAbiertasCacheTtl,
            ObtenerCuentasAbiertasDesdeApiAsync);

    private async Task<List<CuentaResponse>> ObtenerCuentasAbiertasDesdeApiAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var r = await _http.GetAsync("/api/cuentas/abiertas");
        System.Diagnostics.Debug.WriteLine($"[HTTP][Cuenta] GET /api/cuentas/abiertas status: {(int)r.StatusCode} en {sw.ElapsedMilliseconds} ms");
        await ApiErrorHelper.EnsureSuccessAsync(r);
        return (await r.Content.ReadFromJsonAsync<List<CuentaResponse>>()) ?? [];
    }

    public async Task<CuentaResponse?> ObtenerCuentaAbiertaPorMesaAsync(int idMesa)
    {
        var r = await _http.GetAsync($"/api/cuentas/mesa/{idMesa}/abierta");
        if (r.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        await ApiErrorHelper.EnsureSuccessAsync(r);
        return await r.Content.ReadFromJsonAsync<CuentaResponse>();
    }

    public async Task<CuentaResponse> CrearCuentaAsync(int idMesa, int idCliente, double total = 0)
    {
        var r = await _http.PostAsJsonAsync("/api/cuentas",
            new CuentaRequest { IdMesa = idMesa, IdCliente = idCliente, Total = total }, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(r);
        var cuenta = (await r.Content.ReadFromJsonAsync<CuentaResponse>())!;
        await InvalidarCacheCuentasAbiertasAsync();
        return cuenta;
    }

    public async Task<CuentaResponse> AgregarPedidoACuentaAsync(int idCuenta, int idPedido)
    {
        var r = await _http.PostAsync($"/api/cuentas/{idCuenta}/pedidos/{idPedido}", null);
        await ApiErrorHelper.EnsureSuccessAsync(r);
        var cuenta = (await r.Content.ReadFromJsonAsync<CuentaResponse>())!;
        await InvalidarCacheCuentasAbiertasAsync();
        return cuenta;
    }

    public async Task<CuentaResponse> AsignarClienteCuentaAsync(int idCuenta, int idCliente)
    {
        var r = await _http.PatchAsJsonAsync($"/api/cuentas/{idCuenta}/cliente",
            new { idCliente }, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(r);
        var cuenta = (await r.Content.ReadFromJsonAsync<CuentaResponse>())!;
        await InvalidarCacheCuentasAbiertasAsync();
        return cuenta;
    }

    public async Task<CuentaResponse> CerrarCuentaAsync(int idCuenta)
    {
        var r = await _http.PatchAsJsonAsync($"/api/cuentas/{idCuenta}/estado",
            new CambiarEstadoRequest { Estado = CuentaEstados.Pagada }, _camelCase);
        await ApiErrorHelper.EnsureSuccessAsync(r);
        var cuenta = (await r.Content.ReadFromJsonAsync<CuentaResponse>())!;
        await InvalidarCacheCuentasAbiertasAsync();
        return cuenta;
    }

    public Task InvalidarCacheCuentasAbiertasAsync()
        => _cache.RemoveAsync(CuentasAbiertasCacheKey);
}
