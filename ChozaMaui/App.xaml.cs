using ChozaMaui.Views;

namespace ChozaMaui;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new NavigationPage(_services.GetRequiredService<LoginPage>()));
}
