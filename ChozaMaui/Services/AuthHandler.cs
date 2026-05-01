using System.Net.Http.Headers;

namespace ChozaMaui.Services;

/// <summary>
/// DelegatingHandler que inyecta el JWT en cada request automáticamente.
/// Elimina la necesidad de llamar AplicarToken() manualmente en ApiService.
/// </summary>
public class AuthHandler : DelegatingHandler
{
    private readonly SessionService _session;

    public AuthHandler(SessionService session) => _session = session;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_session.Token is not null)
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _session.Token);

        return base.SendAsync(request, cancellationToken);
    }
}
