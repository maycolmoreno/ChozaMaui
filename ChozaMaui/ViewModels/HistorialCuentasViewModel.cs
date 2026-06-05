using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class HistorialCuentasViewModel : ObservableObject
{
    private readonly RoleCapabilityService _capabilities;
    private readonly HistorialCuentasCobroService _cobroService;
    private readonly HistorialCuentasClienteService _clienteService;
    private readonly HistorialCuentasLoadService _loadService;
    private readonly HistorialCuentasPresentationService _presentation;
    private readonly SessionService _session;
    private readonly INavigationService _navigation;
    private readonly NotificationService _notifications;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTimeOffset? _ultimaCargaUtc;
    private static readonly TimeSpan VentanaMinimaRecarga = TimeSpan.FromSeconds(10);

    // ── Datos ─────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<CuentaResponse> cuentas = new();
    private List<CuentaResponse> _todas = [];

    // ── Header del usuario ────────────────────────────────────────
    [ObservableProperty] private string inicialUsuario = "U";
    [ObservableProperty] private string nombreUsuario = "Usuario";
    [ObservableProperty] private string rolUsuario = "Usuario";
    [ObservableProperty] private string headerKpi1Titulo = "Pendientes";
    [ObservableProperty] private string headerKpi1Valor = "0";
    [ObservableProperty] private string headerKpi2Titulo = "Cobradas";
    [ObservableProperty] private string headerKpi2Valor = "0";
    [ObservableProperty] private string headerKpi3Titulo = "Total";
    [ObservableProperty] private string headerKpi3Valor = "$0";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneAlertasHeader))]
    private int totalAlertasHeader;
    public bool TieneAlertasHeader => TotalAlertasHeader > 0;

    // ── Filtros ───────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TabPendientesActivo))]
    [NotifyPropertyChangedFor(nameof(TabCobradasActivo))]
    [NotifyPropertyChangedFor(nameof(TabTodasActivo))]
    private string filtroEstado = CuentaEstados.Abierta;
    [ObservableProperty] private DateTime fechaDesde = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime fechaHasta = DateTime.Today;
    [ObservableProperty] private string textoBusqueda = string.Empty;

    partial void OnFiltroEstadoChanged(string value) => AplicarFiltro();
    partial void OnTextoBusquedaChanged(string value) => AplicarFiltro();

    // ── Estadísticas rápidas ──────────────────────────────────────
    [ObservableProperty] private int totalCuentas;
    [ObservableProperty] private int cuentasAbiertas;
    [ObservableProperty] private int cuentasCerradas;
    [ObservableProperty] private double totalFacturado;

    // ── Detalle expandido ─────────────────────────────────────────
    [ObservableProperty] private CuentaResponse? cuentaDetalle;
    [ObservableProperty] private bool mostrarDetalle;
    [ObservableProperty] private bool cuentaDetalleEsAbierta;

    partial void OnCuentaDetalleChanged(CuentaResponse? value)
    {
        CuentaDetalleEsAbierta = value?.Estado == CuentaEstados.Abierta;
        OnPropertyChanged(nameof(PuedeCobrarCuenta));
    }

    // ── Estado UI ─────────────────────────────────────────────────
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;

    // ── Buscador de cliente ───────────────────────────────────────
    private List<ClienteResponse> _todosClientes = [];
    [ObservableProperty] private bool mostrarBuscadorCliente;
    [ObservableProperty] private bool mostrarFormNuevoCliente;
    [ObservableProperty] private string textoBusquedaCliente = string.Empty;
    [ObservableProperty] private ObservableCollection<ClienteResponse> clientesFiltrados = new();
    [ObservableProperty] private bool sinResultadosCliente;
    // Nuevo cliente
    [ObservableProperty] private string nuevoNombre = string.Empty;
    [ObservableProperty] private string nuevaCedula = string.Empty;
    [ObservableProperty] private string nuevoTelefono = string.Empty;
    [ObservableProperty] private string errorCliente = string.Empty;

    partial void OnTextoBusquedaClienteChanged(string value) => FiltrarClientes(value);

    private void FiltrarClientes(string termino)
    {
        var lista = _clienteService.FiltrarClientes(_todosClientes, termino);
        ReemplazarItems(ClientesFiltrados, lista);
        SinResultadosCliente = lista.Count == 0;
    }

    // Opciones de filtro de estado
    public List<string> OpcionesEstado { get; } = [CuentaEstados.Abierta, "COBRADAS", "TODAS", CuentaEstados.Anulada];

    // Control por rol
    public bool PuedeCobrarCuenta => _capabilities.PuedeCobrarCuenta(_session.Rol) && CuentaDetalleEsAbierta;
    public int TotalPendientes => _todas.Count(_presentation.EsCuentaPendiente);
    public int TotalCobradas => _todas.Count(_presentation.EsCuentaCobrada);
    public bool TabPendientesActivo => FiltroEstado == CuentaEstados.Abierta;
    public bool TabCobradasActivo => FiltroEstado == "COBRADAS";
    public bool TabTodasActivo => FiltroEstado == "TODAS";

    public HistorialCuentasViewModel(RoleCapabilityService capabilities, SessionService session, HistorialCuentasPresentationService presentation, HistorialCuentasClienteService clienteService, HistorialCuentasCobroService cobroService, HistorialCuentasLoadService loadService, INavigationService navigation, NotificationService notifications)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _capabilities = capabilities;
        _session = session;
        _presentation = presentation;
        _clienteService = clienteService;
        _cobroService = cobroService;
        _loadService = loadService;
        _navigation = navigation;
        _notifications = notifications;
        ActualizarHeaderOperativo();
        System.Diagnostics.Debug.WriteLine($"[PERF][HistorialCuentasViewModel] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    // ═══════════════════════════════════════════════════════════════
    // Carga
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand]
    public async Task CargarAsync()
    {
        await CargarInternoAsync(force: true);
    }

    public Task CargarSiEsNecesarioAsync()
        => CargarInternoAsync(force: false);

    private async Task CargarInternoAsync(bool force)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        if (!await _refreshLock.WaitAsync(0))
            return;

        if (!force && _ultimaCargaUtc is not null && DateTimeOffset.UtcNow - _ultimaCargaUtc < VentanaMinimaRecarga)
        {
            _refreshLock.Release();
            return;
        }

        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var resultado = await _loadService.CargarAsync(_session.Rol);
            if (resultado.RequiereAperturaCaja)
            {
                Mensaje = resultado.Mensaje;
                await _navigation.GoToAsync("turnos");
                return;
            }

            _todas = resultado.Cuentas.ToList();
            RecalcularEstadisticas();
            AplicarFiltro();
            _ultimaCargaUtc = DateTimeOffset.UtcNow;
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; }
        finally
        {
            IsBusy = false;
            _refreshLock.Release();
            System.Diagnostics.Debug.WriteLine($"[PERF][HistorialCuentasViewModel] CargarInternoAsync(force={force}): {sw.ElapsedMilliseconds} ms");
        }
    }

    [RelayCommand]
    public async Task AplicarFechasAsync()
    {
        await CargarAsync();
    }

    [RelayCommand]
    private void MostrarPendientes() => FiltroEstado = CuentaEstados.Abierta;

    [RelayCommand]
    private void MostrarCobradas() => FiltroEstado = "COBRADAS";

    [RelayCommand]
    private void MostrarTodas() => FiltroEstado = "TODAS";

    // ═══════════════════════════════════════════════════════════════
    // Filtrado
    // ═══════════════════════════════════════════════════════════════
    private void AplicarFiltro()
    {
        var lista = _presentation.Filtrar(
            _todas,
            FiltroEstado,
            FechaDesde,
            FechaHasta,
            TextoBusqueda);

        ReemplazarItems(Cuentas, lista);
    }

    private static void ReemplazarItems<T>(ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }

    private void RecalcularEstadisticas()
    {
        var stats = _presentation.CalcularStats(_todas);
        TotalCuentas = stats.TotalCuentas;
        CuentasAbiertas = stats.CuentasAbiertas;
        CuentasCerradas = stats.CuentasCerradas;
        TotalFacturado = stats.TotalFacturado;
        OnPropertyChanged(nameof(TotalPendientes));
        OnPropertyChanged(nameof(TotalCobradas));
        ActualizarHeaderOperativo();
    }

    // ═══════════════════════════════════════════════════════════════
    // Detalle
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand]
    public void VerDetalle(CuentaResponse cuenta)
    {
        CuentaDetalle = cuenta;
        MostrarDetalle = true;
        CerrarModalesCliente();
    }

    [RelayCommand]
    public void CerrarDetalle()
    {
        MostrarDetalle = false;
        CuentaDetalle = null;
        CerrarModalesCliente();
    }

    // ═══════════════════════════════════════════════════════════════
    // Buscador de cliente
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task AbrirBuscadorClienteAsync()
    {
        if (_todosClientes.Count == 0)
        {
            IsBusy = true;
            try { _todosClientes = await _clienteService.CargarClientesAsync(); }
            catch { _todosClientes = []; }
            finally { IsBusy = false; }
        }
        TextoBusquedaCliente = string.Empty;
        ClientesFiltrados.Clear();
        SinResultadosCliente = false;
        ErrorCliente = string.Empty;
        MostrarFormNuevoCliente = false;
        MostrarBuscadorCliente = true;
    }

    [RelayCommand]
    private void CerrarBuscadorCliente()
    {
        CerrarModalesCliente();
        TextoBusquedaCliente = string.Empty;
    }

    [RelayCommand]
    private async Task SeleccionarClienteAsync(ClienteResponse cliente)
    {
        if (CuentaDetalle is null) return;
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var actualizada = await _clienteService.AsignarClienteAsync(CuentaDetalle.Idcuenta, cliente.Idcliente);
            var idx = _todas.FindIndex(c => c.Idcuenta == actualizada.Idcuenta);
            if (idx >= 0) _todas[idx] = actualizada;
            CuentaDetalle = actualizada;
            AplicarFiltro();
            CerrarModalesCliente();
            Mensaje = $"Cliente '{cliente.Nombre}' asignado correctamente.";
        }
        catch (Exception ex) { Mensaje = $"Error al asignar cliente: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void AbrirFormNuevoCliente()
    {
        NuevoNombre = TextoBusquedaCliente;
        NuevaCedula = string.Empty;
        NuevoTelefono = string.Empty;
        ErrorCliente = string.Empty;
        MostrarFormNuevoCliente = true;
    }

    [RelayCommand]
    private void CerrarFormNuevoCliente() => MostrarFormNuevoCliente = false;

    private void CerrarModalesCliente()
    {
        MostrarBuscadorCliente = false;
        MostrarFormNuevoCliente = false;
    }

    [RelayCommand]
    private async Task GuardarNuevoClienteAsync()
    {
        var validacion = _clienteService.ValidarNuevoCliente(NuevoNombre, NuevaCedula, NuevoTelefono);
        ErrorCliente = validacion.Error;
        if (!validacion.EsValido)
            return;

        IsBusy = true;
        try
        {
            var nuevo = await _clienteService.CrearClienteAsync(NuevoNombre, NuevaCedula, NuevoTelefono);
            _todosClientes.Add(nuevo);
            await SeleccionarClienteAsync(nuevo);
        }
        catch (Exception ex) { ErrorCliente = $"Error al crear cliente: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    // ═══════════════════════════════════════════════════════════════
    // Pago (cajero)
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task CobrarCuentaAsync(CuentaResponse cuenta)
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var pedido = await _cobroService.ObtenerPedidoParaCobroAsync(cuenta);
            if (pedido.Estado is not (PedidoEstados.Completado or PedidoEstados.Entregado))
            {
                Mensaje = "El pedido aún no fue entregado al cliente.";
                return;
            }
            MostrarDetalle = false;
            await _navigation.GoToAsync("pago",
                new Dictionary<string, object> { { "Pedido", pedido } });
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    // ═══════════════════════════════════════════════════════════════
    // Header operativo
    // ═══════════════════════════════════════════════════════════════
    private void ActualizarHeaderOperativo()
    {
        var nombre = _session.NombreCompleto ?? _session.Username ?? "Usuario";
        NombreUsuario = nombre;
        RolUsuario = FormatearRol(_session.Rol);
        InicialUsuario = CrearIniciales(nombre);
        TotalAlertasHeader = _notifications.Historial.Count(n => !n.Leida);

        // KPIs: pendientes, cobradas, total facturado
        var pendientes = _todas.Count(c => c.Estado == CuentaEstados.Abierta);
        var cobradas = _todas.Count(c => c.Estado != CuentaEstados.Abierta);
        var total = _todas.Where(c => c.Estado != CuentaEstados.Abierta).Sum(c => c.Total);
        HeaderKpi1Titulo = "Pendientes";
        HeaderKpi1Valor = pendientes.ToString();
        HeaderKpi2Titulo = "Cobradas";
        HeaderKpi2Valor = cobradas.ToString();
        HeaderKpi3Titulo = "Total";
        HeaderKpi3Valor = $"${total:F0}";
    }

    [RelayCommand]
    private Task IrNotificacionesAsync()
        => _navigation.GoToAsync("notificaciones");

    private static string FormatearRol(string? rol) => (rol ?? string.Empty).ToUpperInvariant() switch
    {
        "CAJERO" => "Cajero",
        "CAMARERO" => "Camarero",
        "COCINA" => "Cocina",
        "ADMIN" => "Admin",
        _ => rol ?? "Usuario"
    };

    private static string CrearIniciales(string nombre)
    {
        var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length >= 2
            ? $"{partes[0][0]}{partes[1][0]}".ToUpperInvariant()
            : nombre.Length > 0 ? nombre[0].ToString().ToUpperInvariant() : "U";
    }
}
