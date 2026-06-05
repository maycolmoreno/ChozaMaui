namespace ChozaMaui.Services;

/// <summary>Guarda datos de sesión (token, usuario) en SecureStorage.</summary>
public class SessionService
{
    private const string TokenKey = "jwt_token";
    private const string UserIdKey = "user_id";
    private const string UsernameKey = "username";
    private const string NombreKey = "nombre_completo";
    private const string RolKey = "rol";

    public string? Token { get; private set; }
    public int UserId { get; private set; }
    public string? Username { get; private set; }
    public string? NombreCompleto { get; private set; }
    public string? Rol { get; private set; }
    public bool EstaAutenticado => !string.IsNullOrEmpty(Token);

    public SessionService()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Debug.WriteLine($"[PERF][SessionService] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    public async Task GuardarSesionAsync(string token, int userId, string username, string nombre, string rol)
    {
        Token = token;
        UserId = userId;
        Username = username;
        NombreCompleto = nombre;
        Rol = rol;

        await SecureStorage.Default.SetAsync(TokenKey, token);
        await SecureStorage.Default.SetAsync(UserIdKey, userId.ToString());
        await SecureStorage.Default.SetAsync(UsernameKey, username);
        await SecureStorage.Default.SetAsync(NombreKey, nombre);
        await SecureStorage.Default.SetAsync(RolKey, rol);
    }

    public async Task CargarSesionAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Token = await SecureStorage.Default.GetAsync(TokenKey);
        var id = await SecureStorage.Default.GetAsync(UserIdKey);
        UserId = int.TryParse(id, out var parsed) ? parsed : 0;
        Username = await SecureStorage.Default.GetAsync(UsernameKey);
        NombreCompleto = await SecureStorage.Default.GetAsync(NombreKey);
        Rol = await SecureStorage.Default.GetAsync(RolKey);
        System.Diagnostics.Debug.WriteLine($"[PERF][SessionService] CargarSesionAsync: {sw.ElapsedMilliseconds} ms");
    }

    public void CerrarSesion()
    {
        Token = null;
        UserId = 0;
        Username = null;
        NombreCompleto = null;
        Rol = null;
        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(UserIdKey);
        SecureStorage.Default.Remove(UsernameKey);
        SecureStorage.Default.Remove(NombreKey);
        SecureStorage.Default.Remove(RolKey);
    }
}
