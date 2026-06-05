using ChozaMaui.Services;
using ChozaMaui.Views;
using System.Diagnostics;

namespace ChozaMaui;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly ConnectivityService _connectivity;
    private readonly SessionService _session;
    private readonly INavigationService _navigation;
    private bool _autoLoginAttempted;

    public App(IServiceProvider services, SessionService session, INavigationService navigation, ConnectivityService connectivity)
    {
        var sw = Stopwatch.StartNew();
        Debug.WriteLine($"[PERF][App] Constructor inicio");
        InitializeComponent();
        Debug.WriteLine($"[PERF][App] InitializeComponent: {sw.ElapsedMilliseconds} ms");
        _services = services;
        _connectivity = connectivity;
        _session = session;
        _navigation = navigation;
        Debug.WriteLine($"[PERF][App] Constructor completo: {sw.ElapsedMilliseconds} ms");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // ── Ventana placeholder mínima ────────────────────────────────────────
        // LoginPage se crea en OnStart (async) para no bloquear el hilo UI aquí
        // y evitar el "No responde" de Android.
        var sw = Stopwatch.StartNew();
        var placeholder = new ContentPage { BackgroundColor = Color.FromArgb("#1a1a2e") };
        var window = new Window(new NavigationPage(placeholder) { BarBackgroundColor = Color.FromArgb("#1a1a2e") });
        Debug.WriteLine($"[PERF][App] CreateWindow placeholder: {sw.ElapsedMilliseconds} ms");
        return window;
    }

    protected override async void OnStart()
    {
        var sw = Stopwatch.StartNew();
        base.OnStart();
        if (_autoLoginAttempted) return;
        _autoLoginAttempted = true;

        // Ceder el hilo UI para que la ventana placeholder se renderice
        // antes de iniciar trabajo pesado.
        await Task.Yield();
        Debug.WriteLine($"[PERF][App] Task.Yield: {sw.ElapsedMilliseconds} ms");

        var loginSw = Stopwatch.StartNew();
        var loginPage = _services.GetRequiredService<LoginPage>();
        Debug.WriteLine($"[PERF][App] LoginPage resolve: {loginSw.ElapsedMilliseconds} ms  (total: {sw.ElapsedMilliseconds} ms)");

        if (Application.Current?.Windows.FirstOrDefault() is Window w)
            w.Page = new NavigationPage(loginPage);

        await _session.CargarSesionAsync();
        Debug.WriteLine($"[PERF][App] CargarSesionAsync: {sw.ElapsedMilliseconds} ms");

        if (_session.EstaAutenticado)
        {
            _navigation.IrAlShellSegunRol();
            Debug.WriteLine($"[PERF][App] Shell inicial asignado: {sw.ElapsedMilliseconds} ms");
        }

        _ = InicializarConectividadEnSegundoPlanoAsync();
    }

    private async Task InicializarConectividadEnSegundoPlanoAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await Task.Delay(750);
            await _connectivity.InitializeAsync();
            Debug.WriteLine($"[PERF][App] Connectivity background: {sw.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PERF][App] Connectivity background error: {ex.Message}");
        }
    }
}
