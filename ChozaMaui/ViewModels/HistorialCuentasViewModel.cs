using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class HistorialCuentasViewModel : ObservableObject
{
    private readonly HistorialCuentasCobroService _cobroService;
    private readonly HistorialCuentasClienteService _clienteService;
    private readonly HistorialCuentasLoadService _loadService;
    private readonly HistorialCuentasPresentationService _presentation;
    private readonly SessionService _session;
    private readonly INavigationService _navigation;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTimeOffset? _ultimaCargaUtc;
    private static readonly TimeSpan VentanaMinimaRecarga = TimeSpan.FromSeconds(10);

    // ── Datos ─────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<CuentaResponse> cuentas = new();
    private List<CuentaResponse> _todas = [];

    // ── Filtros ───────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TabPendientesActivo))]
    [NotifyPropertyChangedFor(nameof(TabCobradasActivo))]
    [NotifyPropertyChangedFor(nameof(TabTodasActivo))]
    private string filtroEstado = "ABIERTA";
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
        CuentaDetalleEsAbierta = value?.Estado == "ABIERTA";
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
    public List<string> OpcionesEstado { get; } = ["ABIERTA", "COBRADAS", "TODAS", "ANULADA"];

    // Control por rol
    public bool EsCajero => _session.Rol == "CAJERO";
    public bool PuedeCobrarCuenta => EsCajero && CuentaDetalleEsAbierta;
    public int TotalPendientes => _todas.Count(_presentation.EsCuentaPendiente);
    public int TotalCobradas => _todas.Count(_presentation.EsCuentaCobrada);
    public bool TabPendientesActivo => FiltroEstado == "ABIERTA";
    public bool TabCobradasActivo => FiltroEstado == "COBRADAS";
    public bool TabTodasActivo => FiltroEstado == "TODAS";

    public HistorialCuentasViewModel(SessionService session, HistorialCuentasPresentationService presentation, HistorialCuentasClienteService clienteService, HistorialCuentasCobroService cobroService, HistorialCuentasLoadService loadService, INavigationService navigation)
    {
        _session = session;
        _presentation = presentation;
        _clienteService = clienteService;
        _cobroService = cobroService;
        _loadService = loadService;
        _navigation = navigation;
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
        }
    }

    [RelayCommand]
    public async Task AplicarFechasAsync()
    {
        await CargarAsync();
    }

    [RelayCommand]
    private void MostrarPendientes() => FiltroEstado = "ABIERTA";

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
            MostrarDetalle = false;
            await _navigation.GoToAsync("pago",
                new Dictionary<string, object> { { "Pedido", pedido } });
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
