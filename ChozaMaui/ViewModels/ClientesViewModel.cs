using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class ClientesViewModel : ObservableObject
{
    private readonly ApiService _api;

    // ── Lista ──────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ClienteResponse> clientes = new();
    [ObservableProperty] private ClienteResponse? clienteSeleccionado;

    // ── Formulario (crear / editar) ────────────────────────────────
    [ObservableProperty] private bool mostrarFormulario;
    [ObservableProperty] private bool esEdicion;
    [ObservableProperty] private string formNombre = string.Empty;
    [ObservableProperty] private string formCedula = string.Empty;
    [ObservableProperty] private string formTelefono = string.Empty;
    [ObservableProperty] private string formDireccion = string.Empty;
    [ObservableProperty] private string formEmail = string.Empty;
    [ObservableProperty] private bool formEstado = true;

    // ── Estado ────────────────────────────────────────────────────
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private bool mensajeExito;

    // ── Búsqueda ──────────────────────────────────────────────────
    [ObservableProperty] private string textoBusqueda = string.Empty;
    private List<ClienteResponse> _todos = [];

    public ClientesViewModel(ApiService api) => _api = api;

    // ─────────────────────────────────────────────────────────────
    // Cargar
    // ─────────────────────────────────────────────────────────────
    [RelayCommand]
    public async Task CargarAsync()
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            _todos = await _api.GetClientesAsync();
            AplicarFiltro();
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    partial void OnTextoBusquedaChanged(string value) => AplicarFiltro();

    private void AplicarFiltro()
    {
        var filtrado = string.IsNullOrWhiteSpace(TextoBusqueda)
            ? _todos
            : _todos.Where(c =>
                c.Nombre.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase) ||
                c.Cedula.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase)).ToList();

        Clientes = new ObservableCollection<ClienteResponse>(filtrado);
    }

    // ─────────────────────────────────────────────────────────────
    // Formulario – Nuevo
    // ─────────────────────────────────────────────────────────────
    [RelayCommand]
    public void AbrirNuevo()
    {
        EsEdicion = false;
        ClienteSeleccionado = null;
        LimpiarFormulario();
        MostrarFormulario = true;
        Mensaje = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────
    // Formulario – Editar
    // ─────────────────────────────────────────────────────────────
    [RelayCommand]
    public void AbrirEdicion(ClienteResponse cliente)
    {
        EsEdicion = true;
        ClienteSeleccionado = cliente;
        FormNombre    = cliente.Nombre;
        FormCedula    = cliente.Cedula;
        FormTelefono  = cliente.Telefono ?? string.Empty;
        FormDireccion = cliente.Direccion ?? string.Empty;
        FormEmail     = cliente.Email ?? string.Empty;
        FormEstado    = cliente.Estado;
        MostrarFormulario = true;
        Mensaje = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────
    // Cancelar formulario
    // ─────────────────────────────────────────────────────────────
    [RelayCommand]
    public void CancelarFormulario()
    {
        MostrarFormulario = false;
        LimpiarFormulario();
    }

    // ─────────────────────────────────────────────────────────────
    // Guardar (crear o actualizar)
    // ─────────────────────────────────────────────────────────────
    [RelayCommand]
    public async Task GuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(FormNombre) || string.IsNullOrWhiteSpace(FormCedula))
        {
            Mensaje = "Nombre y cédula son obligatorios.";
            MensajeExito = false;
            return;
        }

        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var req = new ClienteRequest
            {
                Nombre    = FormNombre.Trim(),
                Cedula    = FormCedula.Trim(),
                Telefono  = string.IsNullOrWhiteSpace(FormTelefono)  ? null : FormTelefono.Trim(),
                Direccion = string.IsNullOrWhiteSpace(FormDireccion) ? null : FormDireccion.Trim(),
                Email     = string.IsNullOrWhiteSpace(FormEmail)     ? null : FormEmail.Trim(),
                Estado    = FormEstado
            };

            if (EsEdicion && ClienteSeleccionado is not null)
                await _api.ActualizarClienteAsync(ClienteSeleccionado.Idcliente, req);
            else
                await _api.CrearClienteAsync(req);

            Mensaje = EsEdicion ? "Cliente actualizado." : "Cliente creado.";
            MensajeExito = true;
            MostrarFormulario = false;
            LimpiarFormulario();
            await CargarAsync();
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
            MensajeExito = false;
        }
        finally { IsBusy = false; }
    }

    // ─────────────────────────────────────────────────────────────
    // Eliminar
    // ─────────────────────────────────────────────────────────────
    [RelayCommand]
    public async Task EliminarAsync(ClienteResponse cliente)
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            await _api.EliminarClienteAsync(cliente.Idcliente);
            Mensaje = "Cliente eliminado.";
            MensajeExito = true;
            await CargarAsync();
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
            MensajeExito = false;
        }
        finally { IsBusy = false; }
    }

    // ─────────────────────────────────────────────────────────────
    private void LimpiarFormulario()
    {
        FormNombre = FormCedula = FormTelefono = FormDireccion = FormEmail = string.Empty;
        FormEstado = true;
    }
}
