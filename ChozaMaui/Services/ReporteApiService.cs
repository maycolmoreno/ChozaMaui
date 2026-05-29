using System.Net.Http.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Operaciones de API para reportes.
/// </summary>
public class ReporteApiService
{
    private readonly HttpClient _http;

    public ReporteApiService(HttpClient http) => _http = http;

    public async Task<ReporteVentasDia> GetReporteVentasDiaAsync(DateTime? fecha = null)
    {
        var query = fecha.HasValue ? $"?fecha={fecha.Value:yyyy-MM-dd}" : string.Empty;
        var response = await _http.GetAsync($"/api/reportes/ventas-dia{query}");
        await ApiErrorHelper.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ReporteVentasDia>()) ?? new();
    }
}
