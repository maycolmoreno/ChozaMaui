using ChozaMaui.Models;

namespace ChozaMaui.Services;

public sealed class PhotoCaptureService
{
    public async Task<FotoAdjunta?> CapturarFotoAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
            throw new InvalidOperationException("Cámara no disponible en este dispositivo.");

        var permiso = await Permissions.RequestAsync<Permissions.Camera>();
        if (permiso != PermissionStatus.Granted)
            throw new InvalidOperationException("Permiso de cámara denegado.");

        var foto = await MediaPicker.Default.CapturePhotoAsync();
        if (foto is null)
            return null;

        var carpeta = FileSystem.AppDataDirectory;
        var nombre = $"foto_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        var destino = Path.Combine(carpeta, nombre);

        await using var origen = await foto.OpenReadAsync();
        await using var archivo = File.OpenWrite(destino);
        await origen.CopyToAsync(archivo);

        return new FotoAdjunta { RutaLocal = destino };
    }
}