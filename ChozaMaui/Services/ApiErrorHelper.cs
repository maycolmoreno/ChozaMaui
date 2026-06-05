using System.Text.Json;
using System.Net;

namespace ChozaMaui.Services;

internal static class ApiErrorHelper
{
    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
            return;

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = ExtractMessage(raw) ?? BuildFallbackMessage(response);
        throw new HttpRequestException(message, null, response.StatusCode);
    }

    public static string? ExtractMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            foreach (var property in new[] { "mensaje", "message", "error", "title", "detail" })
            {
                if (root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }
        }
        catch (JsonException)
        {
        }

        return raw.Trim();
    }

    public static string ToUserMessage(Exception ex, string operacion)
    {
        return ex switch
        {
            TaskCanceledException =>
                $"Tiempo de espera agotado al {operacion}. El pedido no se perdio; intenta nuevamente.",
            HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } =>
                "Sesion expirada o token invalido. Inicia sesion nuevamente.",
            HttpRequestException { StatusCode: HttpStatusCode.Forbidden } =>
                "Tu rol no tiene permiso para realizar esta accion.",
            HttpRequestException { StatusCode: HttpStatusCode.BadRequest } httpEx =>
                $"Error de validacion: {httpEx.Message}",
            HttpRequestException { StatusCode: HttpStatusCode.InternalServerError } httpEx =>
                $"Error interno del servidor: {httpEx.Message}",
            HttpRequestException { StatusCode: not null } httpEx =>
                $"Error HTTP {(int)httpEx.StatusCode.Value}: {httpEx.Message}",
            HttpRequestException httpEx =>
                $"Servidor no disponible al {operacion}. Verifica que la API este encendida y la URL sea correcta. Detalle: {httpEx.Message}",
            InvalidOperationException invalidEx when invalidEx.Message.Contains("servidor", StringComparison.OrdinalIgnoreCase) =>
                invalidEx.Message,
            _ => ex.Message
        };
    }

    private static string BuildFallbackMessage(HttpResponseMessage response)
    {
        var path = response.RequestMessage?.RequestUri?.AbsolutePath;
        var suffix = string.IsNullOrWhiteSpace(path) ? string.Empty : $" Ruta: {path}.";

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized =>
                "Tu sesion no esta activa o el token no fue enviado. Cierra sesion e ingresa nuevamente." + suffix,
            HttpStatusCode.Forbidden =>
                "Tu rol no tiene permiso para realizar esta accion. Verifica que ingresaste con el usuario correcto." + suffix,
            HttpStatusCode.NotFound =>
                "No se encontro el recurso solicitado." + suffix,
            HttpStatusCode.Conflict =>
                "La operacion no se pudo completar porque entra en conflicto con el estado actual del sistema." + suffix,
            _ => $"Error del servidor ({(int)response.StatusCode})." + suffix
        };
    }
}
