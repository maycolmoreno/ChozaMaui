namespace ChozaMaui.Services;

/// <summary>
/// Contrato para comprimir imágenes antes de subirlas al servidor.
/// Reduce el peso de la imagen preservando calidad aceptable para comprobantes.
/// </summary>
public interface IImageCompressorService
{
    /// <summary>
    /// Comprime la imagen en <paramref name="rutaOriginal"/> y la escribe en
    /// <paramref name="rutaDestino"/>. Si el archivo ya está por debajo del
    /// <paramref name="umbralBytes"/> no lo modifica y copia directamente.
    /// </summary>
    /// <param name="rutaOriginal">Ruta completa del archivo original (JPEG/PNG).</param>
    /// <param name="rutaDestino">Ruta de destino para la imagen comprimida.</param>
    /// <param name="calidad">Calidad JPEG 1–100. Por defecto 72.</param>
    /// <param name="anchoMaximo">Ancho máximo en píxeles. Por defecto 1920.</param>
    /// <param name="umbralBytes">Si el archivo es menor que este límite no se comprime. Por defecto 500 KB.</param>
    /// <returns>Ruta del archivo resultante (puede coincidir con <paramref name="rutaOriginal"/>
    /// si no necesitó compresión).</returns>
    Task<string> ComprimirAsync(
        string rutaOriginal,
        string rutaDestino,
        int    calidad      = 72,
        int    anchoMaximo  = 1920,
        long   umbralBytes  = 512_000);
}
