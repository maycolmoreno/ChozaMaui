namespace ChozaMaui.Services;

/// <summary>
/// Almacena y expone la URL del servidor backend.
/// Los valores se persisten con Preferences (sobreviven reinicios de la app).
/// </summary>
public class SettingsService
{
    private const string HostKey = "server_host";
    private const string PortKey = "server_port";

    // Valores por defecto: emulador Android → 10.0.2.2 (localhost del PC)
    private const string DefaultHost = "192.168.0.1";
    private const string DefaultPort = "8081";

    public string Host
    {
        get => Preferences.Default.Get(HostKey, DefaultHost);
        set => Preferences.Default.Set(HostKey, NormalizeHostOnly(value));
    }

    public string Port
    {
        get => Preferences.Default.Get(PortKey, DefaultPort);
        set => Preferences.Default.Set(PortKey, NormalizePort(value));
    }

    public string BaseUrl => BuildBaseUrl(Host, Port);

    public string WsUrl => $"ws://{Host}:{Port}/ws";

    public bool TrySetServer(string hostInput, string portInput, out string errorMessage)
    {
        if (!TryNormalizeServer(hostInput, portInput, out var host, out var port, out errorMessage))
            return false;

        Preferences.Default.Set(HostKey, host);
        Preferences.Default.Set(PortKey, port);
        return true;
    }

    public void ResetToDefaults()
    {
        Preferences.Default.Set(HostKey, DefaultHost);
        Preferences.Default.Set(PortKey, DefaultPort);
    }

    public bool TryGetBaseUri(out Uri baseUri, out string errorMessage)
    {
        return TryBuildBaseUri(Host, Port, out baseUri, out errorMessage);
    }

    public static bool TryNormalizeServer(
        string hostInput,
        string portInput,
        out string host,
        out string port,
        out string errorMessage)
    {
        host = string.Empty;
        port = string.Empty;
        errorMessage = string.Empty;

        var rawHost = (hostInput ?? string.Empty).Trim();
        var rawPort = (portInput ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(rawHost))
        {
            errorMessage = "Ingresa la IP o nombre del servidor.";
            return false;
        }

        rawHost = rawHost.TrimEnd('/');

        if (TryReadUri(rawHost, out var uri))
        {
            host = uri.Host;
            port = uri.IsDefaultPort ? rawPort : uri.Port.ToString();
        }
        else
        {
            var hostWithPossiblePort = rawHost;
            if (hostWithPossiblePort.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                hostWithPossiblePort = hostWithPossiblePort["http://".Length..];
            else if (hostWithPossiblePort.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                hostWithPossiblePort = hostWithPossiblePort["https://".Length..];

            var slashIndex = hostWithPossiblePort.IndexOf('/');
            if (slashIndex >= 0)
                hostWithPossiblePort = hostWithPossiblePort[..slashIndex];

            var colonIndex = hostWithPossiblePort.LastIndexOf(':');
            if (colonIndex > 0 && colonIndex < hostWithPossiblePort.Length - 1)
            {
                host = hostWithPossiblePort[..colonIndex].Trim();
                port = hostWithPossiblePort[(colonIndex + 1)..].Trim();
            }
            else
            {
                host = hostWithPossiblePort.Trim();
                port = rawPort;
            }
        }

        if (string.IsNullOrWhiteSpace(port))
            port = rawPort;

        if (string.IsNullOrWhiteSpace(host))
        {
            errorMessage = "Ingresa una IP o nombre de servidor válido.";
            return false;
        }

        if (!TryNormalizePort(port, out port, out errorMessage))
            return false;

        return TryBuildBaseUri(host, port, out _, out errorMessage);
    }

    private static string NormalizeHostOnly(string value)
    {
        var host = (value ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Ingresa una IP o nombre de servidor válido.", nameof(value));

        if (TryReadUri(host, out var uri))
            return uri.Host;

        if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            host = host["http://".Length..];
        else if (host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            host = host["https://".Length..];

        var slashIndex = host.IndexOf('/');
        if (slashIndex >= 0)
            host = host[..slashIndex];

        var colonIndex = host.LastIndexOf(':');
        if (colonIndex > 0)
            host = host[..colonIndex];

        host = host.Trim();
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Ingresa una IP o nombre de servidor válido.", nameof(value));

        return host;
    }

    private static string NormalizePort(string value)
    {
        return TryNormalizePort(value, out var port, out var errorMessage)
            ? port
            : throw new ArgumentException(errorMessage, nameof(value));
    }

    private static bool TryNormalizePort(string value, out string port, out string errorMessage)
    {
        port = (value ?? string.Empty).Trim();
        errorMessage = string.Empty;

        if (!int.TryParse(port, out var parsedPort) || parsedPort is < 1 or > 65535)
        {
            errorMessage = "Ingresa un puerto numérico entre 1 y 65535.";
            return false;
        }

        port = parsedPort.ToString();
        return true;
    }

    private static string BuildBaseUrl(string host, string port)
    {
        if (!TryBuildBaseUri(host, port, out var uri, out var errorMessage))
            throw new InvalidOperationException(errorMessage);

        return uri.ToString().TrimEnd('/');
    }

    private static bool TryBuildBaseUri(string host, string port, out Uri baseUri, out string errorMessage)
    {
        baseUri = null!;
        errorMessage = string.Empty;

        if (!TryNormalizePort(port, out var normalizedPort, out errorMessage))
            return false;

        try
        {
            var builder = new UriBuilder("http", host.Trim(), int.Parse(normalizedPort));
            if (!Uri.TryCreate(builder.Uri.ToString(), UriKind.Absolute, out var createdUri))
            {
                errorMessage = "La URL del servidor no es válida.";
                return false;
            }

            baseUri = createdUri;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or UriFormatException)
        {
            errorMessage = "La URL del servidor no es válida.";
            return false;
        }
    }

    private static bool TryReadUri(string value, out Uri uri)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out uri!))
            return Uri.TryCreate($"http://{value}", UriKind.Absolute, out uri!);

        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }
}
