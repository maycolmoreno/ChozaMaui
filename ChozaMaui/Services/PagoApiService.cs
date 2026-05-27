using System.Net.Http.Json;
using System.Text.Json;
using ChozaMaui.Models;

namespace ChozaMaui.Services;

/// <summary>
/// Operaciones de API para Pagos.
/// </summary>
public class PagoApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _camelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PagoApiService(HttpClient http) => _http = http;

    public async Task<PagoResponse> RegistrarPagoAsync(int idCuenta, double monto, string metodo,
        string usuario, string? referencia = null)
    {
        var r = await _http.PostAsJsonAsync($"/api/cuentas/{idCuenta}/pagos",
            new PagoRequest { Monto = monto, Metodo = metodo, Usuario = usuario, Referencia = referencia }, _camelCase);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PagoResponse>())!;
    }

    public async Task<List<PagoResponse>> ListarPagosCuentaAsync(int idCuenta)
    {
        var r = await _http.GetAsync($"/api/cuentas/{idCuenta}/pagos");
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<List<PagoResponse>>(_camelCase)) ?? [];
    }

    public async Task<SaldoCuentaResponse> ObtenerResumenCuentaAsync(int idCuenta)
    {
        var r = await _http.GetAsync($"/api/cuentas/{idCuenta}/pagos/resumen");
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<SaldoCuentaResponse>(_camelCase))!;
    }

    /// <summary>
    /// Sube el comprobante de pago (foto) al backend como multipart/form-data.
    /// Acepta un token de cancelación para aplicar timeout desde el ViewModel.
    /// </summary>
    public async Task<ComprobanteResponse> SubirComprobanteAsync(
        int idCuenta, int idPago, string rutaArchivo, string usuario,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        var fileBytes   = await File.ReadAllBytesAsync(rutaArchivo, cancellationToken);
        var fileName    = Path.GetFileName(rutaArchivo);
        var mimeType    = fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                          ? "image/png" : "image/jpeg";
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        content.Add(fileContent, "archivo", fileName);
        content.Add(new StringContent(usuario), "usuario");

        var r = await _http.PostAsync(
            $"/api/cuentas/{idCuenta}/pagos/{idPago}/comprobante", content, cancellationToken);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ComprobanteResponse>(_camelCase, cancellationToken))!;
    }

    /// <summary>
    /// Obtiene los metadatos del comprobante asociado a un pago (incluye URL pública de Dropbox).
    /// </summary>
    public async Task<ComprobanteResponse?> ObtenerComprobanteAsync(int idCuenta, int idPago)
    {
        var r = await _http.GetAsync($"/api/cuentas/{idCuenta}/pagos/{idPago}/comprobante");
        if (r.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ComprobanteResponse>(_camelCase);
    }

    /// <summary>
    /// Elimina el comprobante del pago (solo ADMIN). Lo borra de Dropbox y de la BD.
    /// </summary>
    public async Task EliminarComprobanteAsync(int idCuenta, int idPago)
    {
        var r = await _http.DeleteAsync($"/api/cuentas/{idCuenta}/pagos/{idPago}/comprobante");
        r.EnsureSuccessStatusCode();
    }
}
