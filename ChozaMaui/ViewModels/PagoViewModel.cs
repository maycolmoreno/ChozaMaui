using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

[QueryProperty(nameof(Pedido), "Pedido")]
public partial class PagoViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SessionService _session;

    // ── Datos del pedido recibido ─────────────────────────────────
    [ObservableProperty] private PedidoResponse? pedido;

    // ── Estado de la cuenta ───────────────────────────────────────
    [ObservableProperty] private CuentaResponse? cuenta;
    [ObservableProperty] private bool tieneCuenta;

    // ── Formulario de pago ────────────────────────────────────────
    [ObservableProperty] private string montoStr = string.Empty;
    [ObservableProperty] private string metodoSeleccionado = "EFECTIVO";
    [ObservableProperty] private string referencia = string.Empty;

    // ── Resultado ─────────────────────────────────────────────────
    [ObservableProperty] private PagoResponse? ultimoPago;
    [ObservableProperty] private bool pagoRegistrado;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;

    // Métodos de pago disponibles
    public List<string> MetodosPago { get; } = new() { "EFECTIVO", "TARJETA", "TRANSFERENCIA" };

    // Totales calculados
    public double TotalPedido  => Pedido?.Total ?? 0;
    public double TotalPagado  => UltimoPago?.TotalPagadoCuenta ?? 0;
    public double Saldo        => UltimoPago?.SaldoPendienteCuenta ?? TotalPedido;
    public bool   PagadoCompleto => Saldo <= 0;

    partial void OnPedidoChanged(PedidoResponse? value)
    {
        OnPropertyChanged(nameof(TotalPedido));
        OnPropertyChanged(nameof(Saldo));
        MontoStr = value?.Total.ToString("F2") ?? string.Empty;
    }

    partial void OnUltimoPagoChanged(PagoResponse? value)
    {
        OnPropertyChanged(nameof(TotalPagado));
        OnPropertyChanged(nameof(Saldo));
        OnPropertyChanged(nameof(PagadoCompleto));
    }

    public PagoViewModel(ApiService api, SessionService session)
    {
        _api = api;
        _session = session;
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (Pedido is null) return;
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            // Buscar si ya existe una cuenta abierta para este pedido/mesa
            var cuentas = await _api.ObtenerCuentasAbiertasAsync();
            Cuenta = cuentas.FirstOrDefault(c => c.Mesa?.Idmesa == Pedido.Mesa?.Idmesa);
            TieneCuenta = Cuenta is not null;
        }
        catch { /* cuenta aún no creada */ }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task PagarAsync()
    {
        if (Pedido is null) return;

        var raw = MontoStr.Replace(",", ".");
        if (!double.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var monto) || monto <= 0)
        {
            Mensaje = "Ingrese un monto válido.";
            return;
        }

        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            // 1. Crear cuenta si no existe
            if (Cuenta is null)
            {
                if (Pedido.Cliente is null)
                {
                    Mensaje = "El pedido no tiene cliente asignado. Asigna un cliente antes de pagar.";
                    return;
                }
                int idCliente = Pedido.Cliente.Idcliente;
                int idMesa    = Pedido.Mesa?.Idmesa ?? 0;
                Cuenta = await _api.CrearCuentaAsync(idMesa, idCliente, Pedido.Total);
                TieneCuenta = true;
                // 2. Asociar el pedido a la cuenta
                await _api.AgregarPedidoACuentaAsync(Cuenta.Idcuenta, Pedido.Idpedido);
            }

            // 3. Registrar pago
            var usuario = _session.Username ?? "desconocido";
            UltimoPago = await _api.RegistrarPagoAsync(
                Cuenta.Idcuenta, monto, MetodoSeleccionado, usuario,
                string.IsNullOrWhiteSpace(Referencia) ? null : Referencia);

            PagoRegistrado = true;

            // 4. Si saldo = 0, cerrar cuenta automáticamente
            if (PagadoCompleto)
            {
                await _api.CerrarCuentaAsync(Cuenta.Idcuenta);
                Mensaje = "¡Pago completado! Cuenta cerrada.";
            }
            else
            {
                Mensaje = $"Pago registrado. Saldo pendiente: ${Saldo:F2}";
            }
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task Volver() =>
        await Shell.Current.GoToAsync("..");
}
