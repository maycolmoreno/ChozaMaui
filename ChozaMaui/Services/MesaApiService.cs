using System.Net.Http.Json;
using System.Text.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Operaciones de API para Comedores y Mesas.
/// </summary>
public class MesaApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _camelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public MesaApiService(HttpClient http) => _http = http;

    // ── Mesas ──────────────────────────────────────────────────────

    public async Task<List<MesaResponse>> ObtenerMesasAsync()
    {
        var response = await _http.GetAsync("/api/mesas");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<MesaResponse>>()) ?? [];
    }

    public async Task<MesaResponse> CrearMesaAsync(MesaUpdateRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/mesas", req, _camelCase);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<MesaResponse>())!;
    }

    public async Task<MesaResponse> ActualizarMesaAsync(int id, MesaUpdateRequest req)
    {
        var r = await _http.PutAsJsonAsync($"/api/mesas/{id}", req, _camelCase);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<MesaResponse>())!;
    }

    public async Task EliminarMesaAsync(int id)
    {
        var r = await _http.DeleteAsync($"/api/mesas/{id}");
        r.EnsureSuccessStatusCode();
    }

    public async Task<MesaResponse> ActualizarEstadoMesaAsync(MesaResponse mesa, bool nuevoEstado)
    {
        var response = await _http.PutAsJsonAsync($"/api/mesas/{mesa.Idmesa}",
            new MesaUpdateRequest
            {
                Numero    = mesa.Numero,
                Capacidad = mesa.Capacidad,
                Estado    = nuevoEstado,
                Idcomedor = mesa.Idcomedor
            }, _camelCase);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MesaResponse>())!;
    }

    // ── Comedores ──────────────────────────────────────────────────
    public async Task<List<ComedorResponse>> GetComedoresAsync()
        => await _http.GetFromJsonAsync<List<ComedorResponse>>("/api/comedores") ?? [];

    public async Task<ComedorResponse> CrearComedorAsync(ComedorRequest req)
    {
        var r = await _http.PostAsJsonAsync("/api/comedores", req, _camelCase);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ComedorResponse>())!;
    }

    public async Task<ComedorResponse> ActualizarComedorAsync(int id, ComedorRequest req)
    {
        var r = await _http.PutAsJsonAsync($"/api/comedores/{id}", req, _camelCase);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ComedorResponse>())!;
    }

    public async Task EliminarComedorAsync(int id)
    {
        var r = await _http.DeleteAsync($"/api/comedores/{id}");
        r.EnsureSuccessStatusCode();
    }
}
