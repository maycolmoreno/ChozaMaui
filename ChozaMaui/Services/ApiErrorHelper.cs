using System.Text.Json;

namespace ChozaMaui.Services;

internal static class ApiErrorHelper
{
    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
            return;

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = ExtractMessage(raw) ?? $"Error del servidor ({(int)response.StatusCode}).";
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
}
