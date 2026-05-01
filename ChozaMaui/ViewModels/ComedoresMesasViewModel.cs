using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class ComedoresMesasViewModel : ObservableObject
{
    private readonly ApiService _api;

    // ── Datos ─────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ComedorResponse> comedores = new();
    [ObservableProperty] private ObservableCollection<MesaResponse> mesas = new();
    private List<MesaResponse> _todasMesas = [];

    // ── Pestaña activa ────────────────────────────────────────────
    [ObservableProperty] private bool mostrarMesas;

    // ── Filtro de comedor para mesas ──────────────────────────────
    [ObservableProperty] private ComedorResponse? comedorFiltro;
    partial void OnComedorFiltroChanged(ComedorResponse? value) => AplicarFiltroMesas();

    // ══════════════════════════════════════════════════════════════
    // Formulario COMEDOR
    // ══════════════════════════════════════════════════════════════
    [ObservableProperty] private bool mostrarFormComedor;
    [ObservableProperty] private bool esEdicionComedor;
    [ObservableProperty] private ComedorResponse? comedorSeleccionado;
    [ObservableProperty] private string formCNombre = string.Empty;
    [ObservableProperty] private string formCDescripcion = string.Empty;
    [ObservableProperty] private bool formCEstado = true;

    // ══════════════════════════════════════════════════════════════
    // Formulario MESA
    // ══════════════════════════════════════════════════════════════
    [ObservableProperty] private bool mostrarFormMesa;
    [ObservableProperty] private bool esEdicionMesa;
    [ObservableProperty] private MesaResponse? mesaSeleccionada;
    [ObservableProperty] private string formMNumero = string.Empty;
    [ObservableProperty] private string formMCapacidad = string.Empty;
    [ObservableProperty] private bool formMEstado = true;
    [ObservableProperty] private ComedorResponse? formMComedor;

    // ── Estado UI ─────────────────────────────────────────────────
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private bool mensajeExito;

    public ComedoresMesasViewModel(ApiService api) => _api = api;

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
            var tC = _api.GetComedoresAsync();
            var tM = _api.ObtenerMesasAsync();
            await Task.WhenAll(tC, tM);

            Comedores = new ObservableCollection<ComedorResponse>(tC.Result);
            _todasMesas = tM.Result;
            AplicarFiltroMesas();
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; MensajeExito = false; }
        finally { IsBusy = false; }
    }

    // ═══════════════════════════════════════════════════════════════
    // Pestañas
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand] public void VerComedores() { MostrarMesas = false; ComedorFiltro = null; }
    [RelayCommand] public void VerMesas() => MostrarMesas = true;

    private void AplicarFiltroMesas()
    {
        var lista = ComedorFiltro is null
            ? _todasMesas
            : _todasMesas.Where(m => m.Idcomedor == ComedorFiltro.Idcomedor).ToList();
        Mesas = new ObservableCollection<MesaResponse>(lista);
    }

    // ═══════════════════════════════════════════════════════════════
    // CRUD COMEDORES
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand]
    public void AbrirNuevoComedor()
    {
        EsEdicionComedor = false;
        ComedorSeleccionado = null;
        FormCNombre = FormCDescripcion = string.Empty;
        FormCEstado = true;
        MostrarFormComedor = true;
        Mensaje = string.Empty;
    }

    [RelayCommand]
    public void AbrirEdicionComedor(ComedorResponse c)
    {
        EsEdicionComedor = true;
        ComedorSeleccionado = c;
        FormCNombre = c.Nombre;
        FormCDescripcion = c.Descripcion ?? string.Empty;
        FormCEstado = c.Estado;
        MostrarFormComedor = true;
        Mensaje = string.Empty;
    }

    [RelayCommand] public void CancelarFormComedor() { MostrarFormComedor = false; }

    [RelayCommand]
    public async Task GuardarComedorAsync()
    {
        if (string.IsNullOrWhiteSpace(FormCNombre))
        {
            Mensaje = "El nombre del comedor es obligatorio.";
            MensajeExito = false;
            return;
        }
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var req = new ComedorRequest
            {
                Nombre      = FormCNombre.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(FormCDescripcion) ? null : FormCDescripcion.Trim(),
                Estado      = FormCEstado
            };
            if (EsEdicionComedor && ComedorSeleccionado is not null)
                await _api.ActualizarComedorAsync(ComedorSeleccionado.Idcomedor, req);
            else
                await _api.CrearComedorAsync(req);

            Mensaje = EsEdicionComedor ? "Comedor actualizado." : "Comedor creado.";
            MensajeExito = true;
            MostrarFormComedor = false;
            await CargarAsync();
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; MensajeExito = false; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task EliminarComedorAsync(ComedorResponse c)
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            await _api.EliminarComedorAsync(c.Idcomedor);
            Mensaje = "Comedor eliminado.";
            MensajeExito = true;
            await CargarAsync();
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; MensajeExito = false; }
        finally { IsBusy = false; }
    }

    // ═══════════════════════════════════════════════════════════════
    // CRUD MESAS
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand]
    public void AbrirNuevaMesa()
    {
        EsEdicionMesa = false;
        MesaSeleccionada = null;
        FormMNumero = FormMCapacidad = string.Empty;
        FormMEstado = true;
        FormMComedor = null;
        MostrarFormMesa = true;
        Mensaje = string.Empty;
    }

    [RelayCommand]
    public void AbrirEdicionMesa(MesaResponse m)
    {
        EsEdicionMesa = true;
        MesaSeleccionada = m;
        FormMNumero   = m.Numero.ToString();
        FormMCapacidad = m.Capacidad.ToString();
        FormMEstado   = m.Estado;
        FormMComedor  = Comedores.FirstOrDefault(c => c.Idcomedor == m.Idcomedor);
        MostrarFormMesa = true;
        Mensaje = string.Empty;
    }

    [RelayCommand] public void CancelarFormMesa() { MostrarFormMesa = false; }

    [RelayCommand]
    public async Task GuardarMesaAsync()
    {
        if (!int.TryParse(FormMNumero, out var numero) || numero <= 0)
        {
            Mensaje = "Número de mesa inválido.";
            MensajeExito = false;
            return;
        }
        if (!int.TryParse(FormMCapacidad, out var cap) || cap <= 0)
        {
            Mensaje = "Capacidad inválida.";
            MensajeExito = false;
            return;
        }
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var req = new MesaUpdateRequest
            {
                Numero    = numero,
                Capacidad = cap,
                Estado    = FormMEstado,
                Idcomedor = FormMComedor?.Idcomedor
            };
            if (EsEdicionMesa && MesaSeleccionada is not null)
                await _api.ActualizarMesaAsync(MesaSeleccionada.Idmesa, req);
            else
                await _api.CrearMesaAsync(req);

            Mensaje = EsEdicionMesa ? "Mesa actualizada." : "Mesa creada.";
            MensajeExito = true;
            MostrarFormMesa = false;
            await CargarAsync();
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; MensajeExito = false; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task EliminarMesaAsync(MesaResponse m)
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            await _api.EliminarMesaAsync(m.Idmesa);
            Mensaje = "Mesa eliminada.";
            MensajeExito = true;
            await CargarAsync();
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; MensajeExito = false; }
        finally { IsBusy = false; }
    }
}
