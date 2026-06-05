using System.Net.Http.Headers;

namespace ChozaMaui.Services;

/// <summary>
/// DelegatingHandler que inyecta el JWT en cada request automáticamente.
/// Elimina la necesidad de adjuntar manualmente el token en cada servicio HTTP.
/// </summary>
public class AuthHandler : DelegatingHandler
{
    private readonly SessionService _session;

    public AuthHandler(SessionService session) => _session = session;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_session.Token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _session.Token);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[HTTP][Auth] Request sin token: {request.Method} {request.RequestUri?.AbsolutePath}");
        }

        return SendWithLogAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithLogAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        System.Diagnostics.Debug.WriteLine($"[HTTP][Auth] {request.Method} {request.RequestUri?.AbsolutePath} -> {(int)response.StatusCode} en {sw.ElapsedMilliseconds} ms");
        return response;
    }
}
