using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class PagoComprobanteService
{
    private readonly PagoApiService _pagosApi;
    private readonly IImageCompressorService? _compresor;

    public PagoComprobanteService(PagoApiService pagosApi, IImageCompressorService? compresor = null)
    {
        _pagosApi = pagosApi;
        _compresor = compresor;
    }

    public async Task<PagoComprobanteArchivo?> CapturarDesdeCamaraAsync()
    {
        var foto = await MediaPicker.Default.CapturePhotoAsync(
            new MediaPickerOptions { Title = "Comprobante de transferencia" });
        return foto is null ? null : await PrepararArchivoAsync(foto, "comp_orig");
    }

    public async Task<PagoComprobanteArchivo?> SeleccionarDesdeGaleriaAsync()
    {
        var fotos = await MediaPicker.Default.PickPhotosAsync(
            new MediaPickerOptions { Title = "Seleccionar comprobante" });
        var foto = fotos?.FirstOrDefault();
        return foto is null ? null : await PrepararArchivoAsync(foto, "comp_gal");
    }

    public async Task<PagoComprobanteUploadResult> SubirAsync(
        int idCuenta,
        int idPago,
        string rutaArchivoComprobante,
        string usuario,
        int intentoActual)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var comprobante = await _pagosApi.SubirComprobanteAsync(
                idCuenta,
                idPago,
                rutaArchivoComprobante,
                usuario,
                cts.Token);

            LimpiarArchivoTemporal(rutaArchivoComprobante);
            return PagoComprobanteUploadResult.CrearExitoso(comprobante);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("[Comprobante] Timeout al subir comprobante");
            return PagoComprobanteUploadResult.CrearFallido(
                "Tiempo de espera agotado. Verifica tu conexión y reintenta.");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("No address associated"))
        {
            return PagoComprobanteUploadResult.CrearFallido(
                "Sin conexión a internet. El comprobante no fue subido.");
        }
        catch (HttpRequestException ex)
        {
            var error = intentoActual < 3
                ? $"Error de red (intento {intentoActual}/3). Toca 'Reintentar'."
                : $"No se pudo subir el comprobante: {ex.Message}";
            return PagoComprobanteUploadResult.CrearFallido(error);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Comprobante] Error: {ex}");
            return PagoComprobanteUploadResult.CrearFallido($"Error al subir comprobante: {ex.Message}");
        }
    }

    public void LimpiarArchivoTemporal(string? rutaArchivoComprobante)
    {
        try
        {
            if (!string.IsNullOrEmpty(rutaArchivoComprobante) &&
                File.Exists(rutaArchivoComprobante) &&
                rutaArchivoComprobante.StartsWith(FileSystem.CacheDirectory, StringComparison.Ordinal))
            {
                File.Delete(rutaArchivoComprobante);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Comprobante] No se pudo limpiar temporal: {ex.Message}");
        }
    }

    private async Task<PagoComprobanteArchivo> PrepararArchivoAsync(FileResult foto, string prefijoTemporal)
    {
        var cacheDir = FileSystem.CacheDirectory;
        var marca = DateTime.Now.ToString("yyyyMMddHHmmss");
        var tempPath = Path.Combine(cacheDir, $"{prefijoTemporal}_{marca}.jpg");
        var finalPath = Path.Combine(cacheDir, $"comp_{marca}.jpg");

        await using (var src = await foto.OpenReadAsync())
        await using (var dst = File.OpenWrite(tempPath))
            await src.CopyToAsync(dst);

        if (_compresor is not null)
        {
            finalPath = await _compresor.ComprimirAsync(tempPath, finalPath);
            if (File.Exists(tempPath) && tempPath != finalPath)
                File.Delete(tempPath);
        }
        else
        {
            finalPath = tempPath;
        }

        return new PagoComprobanteArchivo(finalPath, ImageSource.FromFile(finalPath));
    }
}

public sealed record PagoComprobanteArchivo(string RutaArchivo, ImageSource VistaPrevia);

public sealed record PagoComprobanteUploadResult(ComprobanteResponse? Comprobante, string? Error)
{
    public bool Exitoso => Comprobante is not null && string.IsNullOrEmpty(Error);

    public static PagoComprobanteUploadResult CrearExitoso(ComprobanteResponse comprobante)
        => new(comprobante, null);

    public static PagoComprobanteUploadResult CrearFallido(string error)
        => new(null, error);
}