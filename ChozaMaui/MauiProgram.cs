using ChozaMaui.Services;
using ChozaMaui.ViewModels;
using ChozaMaui.Views;
using Microsoft.Extensions.Logging;

#if ANDROID
using ChozaMaui.Platforms.Android.Services;
#endif

namespace ChozaMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // ── Servicios de infraestructura ──────────────────────────────
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<RoleCapabilityService>();
        builder.Services.AddTransient<AuthHandler>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<ReceiptPdfService>();
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<SessionCacheService>();
        builder.Services.AddSingleton<PedidoCuentaWorkflowService>();
        builder.Services.AddSingleton<OrderWorkflowService>();
        builder.Services.AddSingleton<PendingOrderService>();
        builder.Services.AddSingleton<ConnectivityService>();
        builder.Services.AddSingleton<StompWebSocketService>();
        builder.Services.AddTransient<LiveRefreshCoordinator>();
        builder.Services.AddTransient<PagoWorkflowService>();
        builder.Services.AddTransient<PagoComprobanteService>();
        builder.Services.AddTransient<PagoValidationService>();
        builder.Services.AddTransient<HistorialCuentasPresentationService>();
        builder.Services.AddTransient<HistorialCuentasClienteService>();
        builder.Services.AddTransient<HistorialCuentasCobroService>();
        builder.Services.AddTransient<HistorialCuentasLoadService>();
        builder.Services.AddTransient<PosOrderWorkflowService>();
        builder.Services.AddTransient<PosClientService>();
        builder.Services.AddTransient<PosCatalogService>();
        builder.Services.AddTransient<PosDataService>();
        builder.Services.AddTransient<MesaStateService>();
        builder.Services.AddTransient<PosDraftService>();
        builder.Services.AddTransient<PosMediaService>();
        builder.Services.AddTransient<PosOrderStateService>();
        builder.Services.AddTransient<PhotoCaptureService>();
        builder.Services.AddTransient<PedidoPresentationService>();
        builder.Services.AddTransient<MapaPresentationService>();
        builder.Services.AddTransient<MesaDetailWorkflowService>();
        builder.Services.AddTransient<TurnoWorkflowService>();

        // Compresor de imágenes (implementación específica de plataforma)
#if ANDROID
        builder.Services.AddSingleton<IImageCompressorService, ImageCompressorService>();
#endif

        // HttpClient nombrado con AuthHandler y timeout de 15s.
        // Los servicios por dominio resuelven la URL del servidor en runtime.
        builder.Services.AddHttpClient("ChozaApi", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(15);
        }).AddHttpMessageHandler<AuthHandler>();
        builder.Services.AddTransient(sp =>
        {
            var settings = sp.GetRequiredService<SettingsService>();
            if (!settings.TryGetBaseUri(out var baseUri, out var errorMessage))
                throw new InvalidOperationException(errorMessage);

            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ChozaApi");
            client.BaseAddress = baseUri;
            return new CuentaApiService(client);
        });
        builder.Services.AddTransient<CajaApiService>();
        builder.Services.AddTransient(sp =>
        {
            var settings = sp.GetRequiredService<SettingsService>();
            if (!settings.TryGetBaseUri(out var baseUri, out var errorMessage))
                throw new InvalidOperationException(errorMessage);

            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ChozaApi");
            client.BaseAddress = baseUri;
            return new PedidoApiService(client);
        });
        builder.Services.AddTransient(sp =>
        {
            var settings = sp.GetRequiredService<SettingsService>();
            if (!settings.TryGetBaseUri(out var baseUri, out var errorMessage))
                throw new InvalidOperationException(errorMessage);

            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ChozaApi");
            client.BaseAddress = baseUri;
            return new PagoApiService(client);
        });
        builder.Services.AddTransient(sp =>
        {
            var settings = sp.GetRequiredService<SettingsService>();
            if (!settings.TryGetBaseUri(out var baseUri, out var errorMessage))
                throw new InvalidOperationException(errorMessage);

            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ChozaApi");
            client.BaseAddress = baseUri;
            return new ProductoApiService(client);
        });
        builder.Services.AddTransient(sp =>
        {
            var settings = sp.GetRequiredService<SettingsService>();
            if (!settings.TryGetBaseUri(out var baseUri, out var errorMessage))
                throw new InvalidOperationException(errorMessage);

            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ChozaApi");
            client.BaseAddress = baseUri;
            return new MesaApiService(client);
        });
        builder.Services.AddTransient(sp =>
        {
            var settings = sp.GetRequiredService<SettingsService>();
            if (!settings.TryGetBaseUri(out var baseUri, out var errorMessage))
                throw new InvalidOperationException(errorMessage);

            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ChozaApi");
            client.BaseAddress = baseUri;
            return new ClienteApiService(client);
        });
        builder.Services.AddTransient(sp =>
        {
            var settings = sp.GetRequiredService<SettingsService>();
            if (!settings.TryGetBaseUri(out var baseUri, out var errorMessage))
                throw new InvalidOperationException(errorMessage);

            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ChozaApi");
            client.BaseAddress = baseUri;
            return new ReporteApiService(client);
        });
        builder.Services.AddTransient<UsuarioApiService>();
        builder.Services.AddSingleton<ServerConnectionService>();

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
        builder.Services.AddTransient<HistorialCuentasViewModel>();
        builder.Services.AddTransient<NotificacionesViewModel>();

        // ── Páginas ───────────────────────────────────────────────────
        // Páginas del Shell: Transient para evitar reutilizar handlers/contextos
        // Android ya destruidos entre login, logout y cambios de raíz.
        builder.Services.AddTransient<AppShell>();
        builder.Services.AddTransient<AppShellCajero>();

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
        builder.Services.AddTransient<HistorialCuentasPage>();
        builder.Services.AddTransient<NotificacionesPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
