using ChozaMaui.Services;
using ChozaMaui.Views;

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
        InitializeComponent();
        _services = services;
        _connectivity = connectivity;
        _session = session;
        _navigation = navigation;
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new NavigationPage(_services.GetRequiredService<LoginPage>()));

    protected override async void OnStart()
    {
        base.OnStart();
        if (_autoLoginAttempted) return;
        _autoLoginAttempted = true;

        await _session.CargarSesionAsync();
        await _connectivity.InitializeAsync();
        if (_session.EstaAutenticado)
            _navigation.IrAlShellSegunRol();
    }
}
