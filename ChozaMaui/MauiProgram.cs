using ChozaMaui.Services;
using ChozaMaui.ViewModels;
using ChozaMaui.Views;
using Microsoft.Extensions.Logging;

namespace ChozaMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // ── Servicios de infraestructura ──────────────────────────────
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddTransient<AuthHandler>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<ReceiptPdfService>();

        // HttpClient tipado con AuthHandler y timeout de 15s
        builder.Services.AddHttpClient<ApiService>(c =>
        {
            c.BaseAddress = new Uri(ApiService.BaseUrl);
            c.Timeout = TimeSpan.FromSeconds(15);
        }).AddHttpMessageHandler<AuthHandler>();

        // ── ViewModels ────────────────────────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<PosViewModel>();
        builder.Services.AddTransient<PedidosViewModel>();
        builder.Services.AddTransient<PedidoDetalleViewModel>();
        builder.Services.AddTransient<MapaViewModel>();
        builder.Services.AddTransient<PerfilViewModel>();
        builder.Services.AddTransient<TurnoViewModel>();
        builder.Services.AddTransient<MesaDetalleViewModel>();
        builder.Services.AddTransient<PagoViewModel>();
        builder.Services.AddTransient<AdminViewModel>();
        builder.Services.AddTransient<ClientesViewModel>();
        builder.Services.AddTransient<ProductosViewModel>();
        builder.Services.AddTransient<ComedoresMesasViewModel>();
        builder.Services.AddTransient<HistorialCuentasViewModel>();

        // ── Páginas ───────────────────────────────────────────────────
        // Páginas del Shell: Singleton para que MAUI no las recree en cada tab
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<AppShellCajero>();

        // Páginas de navegación: Transient (se crean por cada navegación)
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<PosPage>();
        builder.Services.AddTransient<PedidosPage>();
        builder.Services.AddTransient<PedidoDetallePage>();
        builder.Services.AddTransient<MapaPage>();
        builder.Services.AddTransient<PerfilPage>();
        builder.Services.AddTransient<TurnoPage>();
        builder.Services.AddTransient<MesaDetallePage>();
        builder.Services.AddTransient<PagoPage>();
        builder.Services.AddTransient<AdminPage>();
        builder.Services.AddTransient<ClientesPage>();
        builder.Services.AddTransient<ProductosPage>();
        builder.Services.AddTransient<ComedoresMesasPage>();
        builder.Services.AddTransient<HistorialCuentasPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
