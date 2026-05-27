using Android.Graphics;
using ChozaMaui.Services;

namespace ChozaMaui.Platforms.Android.Services;

/// <summary>
/// Implementación Android de <see cref="IImageCompressorService"/> usando
/// <c>Android.Graphics.Bitmap</c> para reescalar y recomprimir imágenes JPEG
/// antes de subirlas a Dropbox, sin dependencias NuGet adicionales.
/// </summary>
public class ImageCompressorService : IImageCompressorService
{
    public async Task<string> ComprimirAsync(
        string rutaOriginal,
        string rutaDestino,
        int    calidad     = 72,
        int    anchoMaximo = 1920,
        long   umbralBytes = 512_000)
    {
        var info = new FileInfo(rutaOriginal);
        if (!info.Exists)
            throw new FileNotFoundException("Archivo de comprobante no encontrado", rutaOriginal);

        // Si ya está por debajo del umbral, copiar directamente sin comprimir
        if (info.Length <= umbralBytes)
        {
            if (rutaOriginal != rutaDestino)
                File.Copy(rutaOriginal, rutaDestino, overwrite: true);
            return rutaDestino;
        }

        // Decodificar en hilo de background para no bloquear UI
        return await Task.Run(() =>
        {
            using var original = BitmapFactory.DecodeFile(rutaOriginal)
                ?? throw new InvalidOperationException($"No se pudo decodificar la imagen: {rutaOriginal}");

            // Calcular nuevo tamaño preservando relación de aspecto
            Bitmap bitmapFinal;
            if (original.Width > anchoMaximo)
            {
                float ratio  = (float)anchoMaximo / original.Width;
                int   newH   = (int)(original.Height * ratio);
                bitmapFinal  = Bitmap.CreateScaledBitmap(original, anchoMaximo, newH, true)!;
            }
            else
            {
                bitmapFinal = original;
            }

            try
            {
                using var outStream = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write);
                bitmapFinal.Compress(Bitmap.CompressFormat.Jpeg!, calidad, outStream);
            }
            finally
            {
                if (!ReferenceEquals(bitmapFinal, original))
                    bitmapFinal.Recycle();
            }

            return rutaDestino;
        });
    }
}
