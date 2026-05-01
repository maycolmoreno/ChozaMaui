using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class HistorialCuentasViewModel : ObservableObject
{
    private readonly ApiService _api;

    // ── Datos ─────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<CuentaResponse> cuentas = new();
    private List<CuentaResponse> _todas = [];

    // ── Filtros ───────────────────────────────────────────────────
    [ObservableProperty] private string filtroEstado = "TODAS";
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
        => CuentaDetalleEsAbierta = value?.Estado == "ABIERTA";

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
        if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
        {
            ClientesFiltrados = new();
            SinResultadosCliente = false;
            return;
        }
        var lower = termino.ToLowerInvariant();
        var lista = _todosClientes
            .Where(c => c.Nombre.ToLowerInvariant().Contains(lower) ||
                        (c.Cedula != null && c.Cedula.Contains(termino)))
            .Take(20).ToList();
        ClientesFiltrados = new ObservableCollection<ClienteResponse>(lista);
        SinResultadosCliente = lista.Count == 0;
    }

    // Opciones de filtro de estado
    public List<string> OpcionesEstado { get; } = ["TODAS", "ABIERTA", "CERRADA", "ANULADA"];

    public HistorialCuentasViewModel(ApiService api) => _api = api;

    // ═══════════════════════════════════════════════════════════════
    // Carga
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand]
    public async Task CargarAsync()
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            _todas = await _api.GetTodasCuentasAsync();
            RecalcularEstadisticas();
            AplicarFiltro();
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task AplicarFechasAsync()
    {
        await CargarAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // Filtrado
    // ═══════════════════════════════════════════════════════════════
    private void AplicarFiltro()
    {
        var lista = _todas.AsEnumerable();

        // Filtro por estado
        if (FiltroEstado != "TODAS")
            lista = lista.Where(c => c.Estado.Equals(FiltroEstado, StringComparison.OrdinalIgnoreCase));

        // Filtro por fecha
        lista = lista.Where(c =>
            c.FechaApertura.HasValue &&
            c.FechaApertura.Value.Date >= FechaDesde.Date &&
            c.FechaApertura.Value.Date <= FechaHasta.Date);

        // Búsqueda por mesa o cliente
        if (!string.IsNullOrWhiteSpace(TextoBusqueda))
        {
            lista = lista.Where(c =>
                c.MesaTexto.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase) ||
                c.ClienteTexto.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase) ||
                c.Idcuenta.ToString().Contains(TextoBusqueda));
        }

        // Ordenar por fecha desc
        lista = lista.OrderByDescending(c => c.FechaApertura);

        Cuentas = new ObservableCollection<CuentaResponse>(lista);
    }

    private void RecalcularEstadisticas()
    {
        TotalCuentas    = _todas.Count;
        CuentasAbiertas = _todas.Count(c => c.Estado == "ABIERTA");
        CuentasCerradas = _todas.Count(c => c.Estado == "CERRADA");
        TotalFacturado  = _todas.Where(c => c.Estado == "CERRADA").Sum(c => c.Total);
    }

    // ═══════════════════════════════════════════════════════════════
    // Detalle
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand]
    public void VerDetalle(CuentaResponse cuenta)
    {
        CuentaDetalle = cuenta;
        MostrarDetalle = true;
        MostrarBuscadorCliente = false;
        MostrarFormNuevoCliente = false;
    }

    [RelayCommand]
    public void CerrarDetalle()
    {
        MostrarDetalle = false;
        CuentaDetalle = null;
        MostrarBuscadorCliente = false;
        MostrarFormNuevoCliente = false;
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
            try { _todosClientes = await _api.GetClientesAsync(); }
            catch { _todosClientes = []; }
            finally { IsBusy = false; }
        }
        TextoBusquedaCliente = string.Empty;
        ClientesFiltrados = new();
        SinResultadosCliente = false;
        ErrorCliente = string.Empty;
        MostrarFormNuevoCliente = false;
        MostrarBuscadorCliente = true;
    }

    [RelayCommand]
    private void CerrarBuscadorCliente()
    {
        MostrarBuscadorCliente = false;
        MostrarFormNuevoCliente = false;
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
            var actualizada = await _api.AsignarClienteCuentaAsync(CuentaDetalle.Idcuenta, cliente.Idcliente);
            var idx = _todas.FindIndex(c => c.Idcuenta == actualizada.Idcuenta);
            if (idx >= 0) _todas[idx] = actualizada;
            CuentaDetalle = actualizada;
            AplicarFiltro();
            MostrarBuscadorCliente = false;
            MostrarFormNuevoCliente = false;
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

    [RelayCommand]
    private async Task GuardarNuevoClienteAsync()
    {
        ErrorCliente = string.Empty;
        if (string.IsNullOrWhiteSpace(NuevoNombre))
        { ErrorCliente = "El nombre es obligatorio."; return; }
        if (!Regex.IsMatch(NuevaCedula.Trim(), @"^\d{10,13}$"))
        { ErrorCliente = "La cédula debe tener entre 10 y 13 dígitos."; return; }
        if (!string.IsNullOrEmpty(NuevoTelefono) && !Regex.IsMatch(NuevoTelefono.Trim(), @"^\d{10}$"))
        { ErrorCliente = "El teléfono debe tener 10 dígitos."; return; }

        IsBusy = true;
        try
        {
            var nuevo = await _api.CrearClienteAsync(new ClienteRequest
            {
                Nombre   = NuevoNombre.Trim(),
                Cedula   = NuevaCedula.Trim(),
                Telefono = string.IsNullOrEmpty(NuevoTelefono) ? null : NuevoTelefono.Trim(),
                Estado   = true
            });
            _todosClientes.Add(nuevo);
            await SeleccionarClienteAsync(nuevo);
        }
        catch (Exception ex) { ErrorCliente = $"Error al crear cliente: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
