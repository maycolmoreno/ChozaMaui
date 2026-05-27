using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

[QueryProperty(nameof(Pedido), "Pedido")]
public partial class PagoViewModel : ObservableObject
{
    private readonly SessionService _session;
    private readonly PagoComprobanteService _comprobantes;
    private readonly PagoValidationService _validation;
    private readonly PagoWorkflowService _workflow;
    private readonly INavigationService _navigation;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTimeOffset? _ultimaCargaUtc;
    private int? _pedidoCargadoId;
    private string? _rutaVistaPreviaComprobante;
    private static readonly TimeSpan VentanaMinimaRecarga = TimeSpan.FromSeconds(10);

    // ── Datos del pedido recibido ─────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Consumo))]
    [NotifyPropertyChangedFor(nameof(TotalPedido))]
    [NotifyPropertyChangedFor(nameof(ClienteNombreTexto))]
    [NotifyPropertyChangedFor(nameof(CantidadProductosTexto))]
    [NotifyPropertyChangedFor(nameof(SubtituloPantalla))]
    [NotifyPropertyChangedFor(nameof(HoraAperturaTexto))]
    private PedidoResponse? pedido;

    // ── Estado de la cuenta ───────────────────────────────────────
    [NotifyPropertyChangedFor(nameof(PuedeSubirComprobante))]
    [NotifyPropertyChangedFor(nameof(PuedeAccionarSubidaComprobante))]
    [NotifyCanExecuteChangedFor(nameof(SubirComprobanteCommand))]
    [NotifyPropertyChangedFor(nameof(SubtituloPantalla))]
    [NotifyPropertyChangedFor(nameof(HoraAperturaTexto))]
    [ObservableProperty] private CuentaResponse? cuenta;
    [ObservableProperty] private bool tieneCuenta;

    // ── Formulario de pago ────────────────────────────────────────
    [ObservableProperty] private string montoStr = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsMetodoEfectivo))]
    [NotifyPropertyChangedFor(nameof(EsMetodoTarjeta))]
    [NotifyPropertyChangedFor(nameof(EsMetodoTransferencia))]
    [NotifyPropertyChangedFor(nameof(EsMetodoOtro))]
    private string metodoSeleccionado = "EFECTIVO";
    [ObservableProperty] private string referencia = string.Empty;

    // ── Monto recibido y cambio (efectivo) ────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Cambio))]
    [NotifyPropertyChangedFor(nameof(HayCambio))]
    [NotifyPropertyChangedFor(nameof(FaltaPorRecibir))]
    [NotifyPropertyChangedFor(nameof(TieneFaltanteEfectivo))]
    [NotifyPropertyChangedFor(nameof(PuedeCobrar))]
    private double montoRecibido;

    // ── Resultado pago ────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Saldo))]
    [NotifyPropertyChangedFor(nameof(PagadoCompleto))]
    [NotifyPropertyChangedFor(nameof(PuedeSubirComprobante))]
    [NotifyPropertyChangedFor(nameof(PuedeAccionarSubidaComprobante))]
    [NotifyCanExecuteChangedFor(nameof(SubirComprobanteCommand))]
    private PagoResponse? ultimoPago;
    [ObservableProperty] private bool pagoRegistrado;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeCobrar))]
    private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalCobro))]
    [NotifyPropertyChangedFor(nameof(Cambio))]
    [NotifyPropertyChangedFor(nameof(HayCambio))]
    [NotifyPropertyChangedFor(nameof(FaltaPorRecibir))]
    [NotifyPropertyChangedFor(nameof(TieneFaltanteEfectivo))]
    [NotifyPropertyChangedFor(nameof(Saldo))]
    [NotifyPropertyChangedFor(nameof(PuedeCobrar))]
    [NotifyPropertyChangedFor(nameof(NotaMinimoEfectivo))]
    private double saldoPendienteActual;

    // ── Comprobante: captura ──────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneComprobante))]
    [NotifyPropertyChangedFor(nameof(PuedeAccionarSubidaComprobante))]
    [NotifyCanExecuteChangedFor(nameof(SubirComprobanteCommand))]
    private string? rutaArchivoComprobante;
    [ObservableProperty] private ImageSource? imagenComprobante;

    // ── Comprobante: estados de subida ────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeSubirComprobante))]
    [NotifyPropertyChangedFor(nameof(PuedeAccionarSubidaComprobante))]
    [NotifyPropertyChangedFor(nameof(PuedeCobrar))]
    [NotifyCanExecuteChangedFor(nameof(SubirComprobanteCommand))]
    private bool subiendoComprobante;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeSubirComprobante))]
    [NotifyPropertyChangedFor(nameof(PuedeAccionarSubidaComprobante))]
    [NotifyPropertyChangedFor(nameof(PuedeCobrar))]
    [NotifyCanExecuteChangedFor(nameof(SubirComprobanteCommand))]
    private bool comprobanteSubido;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayErrorComprobante))]
    private string errorComprobante = string.Empty;
    [ObservableProperty] private int    intentosSubida;
    [ObservableProperty] private ComprobanteResponse? ultimoComprobante;

    // ── Propiedades derivadas ─────────────────────────────────────
    public bool TieneComprobante       => !string.IsNullOrEmpty(RutaArchivoComprobante);
        public bool PuedeAccionarSubidaComprobante => TieneComprobante
                                                      && !SubiendoComprobante
                                                      && !ComprobanteSubido;
    public bool PuedeSubirComprobante  => TieneComprobante
                                          && Cuenta is not null
                                          && UltimoPago is not null
                                          && !SubiendoComprobante
                                          && !ComprobanteSubido;
    public bool HayErrorComprobante    => !string.IsNullOrEmpty(ErrorComprobante);

    public List<string> MetodosPago { get; } = ["EFECTIVO", "TARJETA", "TRANSFERENCIA", "OTRO"];
    public bool EsMetodoEfectivo      => MetodoSeleccionado == "EFECTIVO";
    public bool EsMetodoTarjeta       => MetodoSeleccionado == "TARJETA";
    public bool EsMetodoTransferencia => MetodoSeleccionado == "TRANSFERENCIA";
    public bool EsMetodoOtro          => MetodoSeleccionado == "OTRO";
    public bool TieneMontoRecibido    => EsMetodoEfectivo;

    public double Consumo     => Pedido?.Subtotal ?? 0;
    public double TotalPedido => Pedido?.Total ?? 0;
    public double TotalCobro  => SaldoPendienteActual > 0 ? SaldoPendienteActual : TotalPedido;
    public double Cambio            => MontoRecibido > TotalCobro
                                       ? Math.Round(MontoRecibido - TotalCobro, 2) : 0;
    public bool   HayCambio         => Cambio > 0;
    public double FaltaPorRecibir   => EsMetodoEfectivo && MontoRecibido < TotalCobro
                                       ? Math.Round(TotalCobro - MontoRecibido, 2) : 0;
    public bool   TieneFaltanteEfectivo => FaltaPorRecibir > 0;
    public double Saldo             => UltimoPago?.SaldoPendienteCuenta ?? TotalCobro;
    public bool   PagadoCompleto    => PagoRegistrado && Saldo <= 0;
    public bool   PuedeCobrar       => !IsBusy
                                       && Pedido is not null
                                       && TotalCobro > 0
                                       && (!EsMetodoEfectivo || MontoRecibido >= TotalCobro)
                                       && (!EsMetodoTransferencia || TieneComprobante);
    public string SubtituloPantalla
    {
        get
        {
            var mesaTexto = Pedido?.Mesa is not null ? $"Mesa {Pedido.Mesa.Numero}" : "Cobro";
            return Cuenta is not null ? $"{mesaTexto} • Cuenta #{Cuenta.Idcuenta}" : mesaTexto;
        }
    }
    public string ClienteNombreTexto => Pedido?.Cliente?.Nombre ?? "Sin cliente";
    public string HoraAperturaTexto
    {
        get
        {
            var fecha = Cuenta?.FechaApertura ?? Pedido?.Fecha;
            return fecha?.ToString("hh:mm tt") ?? "--:-- --";
        }
    }
    public string CantidadProductosTexto => Pedido is null
                                            ? "0 productos"
                                            : Pedido.CantidadProductos == 1
                                                ? "1 producto"
                                                : $"{Pedido.CantidadProductos} productos";
    public string NotaMinimoEfectivo => $"Debe recibir al menos ${TotalCobro:0.00}";
    public string TextoBotonCobro   => EsMetodoTransferencia
                                       ? "COBRAR Y SUBIR COMPROBANTE"
                                       : "COBRAR Y CERRAR MESA";

    partial void OnPedidoChanged(PedidoResponse? value)
    {
        _ultimaCargaUtc = null;
        _pedidoCargadoId = null;
        ReiniciarEstadoPago(value);
        RefrescarEstadoCobro();
        OnPropertyChanged(nameof(PagadoCompleto));
        MontoStr = value?.Total.ToString("F2") ?? string.Empty;
        MontoRecibido = value?.Total ?? 0;
    }

    private void ReiniciarEstadoPago(PedidoResponse? pedidoActual)
    {
        LimpiarComprobanteTemporalActual();
        Cuenta = null;
        TieneCuenta = false;
        UltimoPago = null;
        PagoRegistrado = false;
        Referencia = string.Empty;
        RutaArchivoComprobante = null;
        ImagenComprobante = null;
        SubiendoComprobante = false;
        ComprobanteSubido = false;
        ErrorComprobante = string.Empty;
        IntentosSubida = 0;
        UltimoComprobante = null;
        Mensaje = string.Empty;
        MetodoSeleccionado = "EFECTIVO";
        SaldoPendienteActual = pedidoActual?.Total ?? 0;
    }

    partial void OnSaldoPendienteActualChanged(double value)
    {
        if (!PagoRegistrado && value > 0)
            MontoStr = value.ToString("F2");
    }

    partial void OnMetodoSeleccionadoChanged(string value)
    {
        if (value == "EFECTIVO" && MontoRecibido <= 0 && TotalPedido > 0)
            MontoRecibido = TotalPedido;

        OnPropertyChanged(nameof(TieneMontoRecibido));
        RefrescarEstadoCobro();
        OnPropertyChanged(nameof(TextoBotonCobro));
    }

    partial void OnPagoRegistradoChanged(bool value)
    {
        OnPropertyChanged(nameof(PagadoCompleto));
    }

    partial void OnRutaArchivoComprobanteChanged(string? value)
    {
        ReiniciarEstadoComprobante();
        RefrescarEstadoComprobante();
        OnPropertyChanged(nameof(PuedeCobrar));
    }

    public PagoViewModel(SessionService session, PagoWorkflowService workflow,
                         PagoComprobanteService comprobantes, PagoValidationService validation,
                         INavigationService navigation)
    {
        _session = session;
        _workflow = workflow;
        _comprobantes = comprobantes;
        _validation = validation;
        _navigation = navigation;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COMMANDS
    // ─────────────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task CargarAsync()
    {
        await CargarInternoAsync(force: true);
    }

    public Task CargarSiEsNecesarioAsync()
        => CargarInternoAsync(force: false);

    private async Task CargarInternoAsync(bool force)
    {
        if (Pedido is null) return;
        if (!await _refreshLock.WaitAsync(0))
            return;

        var cambioDePedido = _pedidoCargadoId != Pedido.Idpedido;
        if (!force && !cambioDePedido && _ultimaCargaUtc is not null && DateTimeOffset.UtcNow - _ultimaCargaUtc < VentanaMinimaRecarga)
        {
            _refreshLock.Release();
            return;
        }

        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var contexto = await _workflow.CargarContextoAsync(Pedido);
            Cuenta = contexto.cuenta;
            TieneCuenta = Cuenta is not null;
            SaldoPendienteActual = contexto.saldoPendiente;
            _pedidoCargadoId = Pedido.Idpedido;
            _ultimaCargaUtc = DateTimeOffset.UtcNow;
        }
        catch { /* cuenta aún no creada */ }
        finally
        {
            IsBusy = false;
            _refreshLock.Release();
        }
    }

    [RelayCommand]
    public async Task PagarAsync()
    {
        if (Pedido is null) return;
        var pedidoActual = Pedido;
        var parseoMonto = _validation.ParsearMonto(MontoStr);
        if (!parseoMonto.EsValido)
        {
            Mensaje = parseoMonto.Mensaje;
            return;
        }

        var monto = parseoMonto.Monto;

        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var usuario = _session.Username ?? "desconocido";
            var cobro = await _workflow.RegistrarCobroAsync(
                pedidoActual,
                Cuenta,
                Math.Min(monto, TotalCobro),
                MetodoSeleccionado,
                usuario,
                Referencia);

            Cuenta = cobro.Cuenta;
            TieneCuenta = true;
            SaldoPendienteActual = cobro.Pago.SaldoPendienteCuenta;
            UltimoPago = cobro.Pago;
            PagoRegistrado = true;
            if (cobro.PagoCompleto)
            {
                var cierre = await _workflow.CerrarMesaCobradaAsync(cobro.Cuenta, pedidoActual);
                pedidoActual.Mesa = cierre.MesaLiberada;
                NotificarPedidoActualizado();
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
    public void SeleccionarMetodo(string metodo) => MetodoSeleccionado = metodo;

    /// <summary>
    /// Flujo completo: registrar pago + subir comprobante (si es TRANSFERENCIA) + cerrar mesa.
    /// </summary>
    [RelayCommand]
    public async Task CobrarYCerrarMesaAsync()
    {
        if (Pedido is null) return;
        var pedidoActual = Pedido;

        var validacion = _validation.ValidarCobro(new PagoCobroValidationInput(
            true,
            TotalCobro,
            EsMetodoEfectivo,
            MontoRecibido,
            FaltaPorRecibir,
            EsMetodoTransferencia,
            TieneComprobante));

        if (!validacion.EsValido)
        {
            Mensaje = validacion.Mensaje;
            return;
        }

        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            if (TotalCobro <= 0)
            {
                Mensaje = "Esta cuenta ya no tiene saldo pendiente.";
                return;
            }

            var usuario = _session.Username ?? "desconocido";
            CuentaResponse? cuentaActual = Cuenta;
            if (UltimoPago is null || UltimoPago.SaldoPendienteCuenta > 0 || cuentaActual is null)
            {
                var cobro = await _workflow.RegistrarCobroAsync(
                    pedidoActual,
                    Cuenta,
                    TotalCobro,
                    MetodoSeleccionado,
                    usuario,
                    Referencia);

                cuentaActual = cobro.Cuenta;
                Cuenta = cobro.Cuenta;
                TieneCuenta = true;
                SaldoPendienteActual = cobro.Pago.SaldoPendienteCuenta;
                UltimoPago = cobro.Pago;
            }

            if (cuentaActual is null)
                return;

            // Subir comprobante si es TRANSFERENCIA y hay foto capturada
            if (MetodoSeleccionado == "TRANSFERENCIA" && TieneComprobante)
            {
                var pagoActual = UltimoPago;
                if (pagoActual is null)
                    return;

                var subidaExitosa = await SubirComprobanteInternoAsync(cuentaActual.Idcuenta, pagoActual.Idpago, usuario);
                if (!subidaExitosa)
                {
                    PagoRegistrado = true;
                    Mensaje = "Pago registrado, pero el comprobante no se subió a Dropbox. Reintenta la subida antes de cerrar la mesa.";
                    return;
                }
            }

            var cierre = await _workflow.CerrarMesaCobradaAsync(cuentaActual, pedidoActual);
            pedidoActual.Mesa = cierre.MesaLiberada;
            NotificarPedidoActualizado();
            PagoRegistrado = true;
            Mensaje = EsMetodoTransferencia
                ? "¡Mesa cobrada! Comprobante guardado en Dropbox."
                : HayCambio
                    ? $"¡Mesa cobrada y liberada! Cambio a entregar: ${Cambio:F2}"
                    : "¡Mesa cobrada y liberada exitosamente!";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    /// <summary>
    /// Captura foto con la cámara, la comprime y actualiza la preview.
    /// </summary>
    [RelayCommand]
    public async Task CapturarComprobanteAsync()
    {
        try
        {
            var archivo = await _comprobantes.CapturarDesdeCamaraAsync();
            if (archivo is null) return;

            AplicarArchivoComprobante(archivo);
        }
        catch (FeatureNotSupportedException)
        {
            await Shell.Current.DisplayAlertAsync("No disponible",
                "La cámara no está disponible en este dispositivo.", "OK");
        }
        catch (PermissionException)
        {
            await Shell.Current.DisplayAlertAsync("Permiso denegado",
                "Se necesita permiso de cámara para tomar el comprobante.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error",
                $"No se pudo capturar la foto: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Subida manual del comprobante (permite reintentar si falla).
    /// Requiere que ya exista un <see cref="UltimoPago"/> registrado.
    /// </summary>
        [RelayCommand]
    public async Task SubirComprobanteAsync()
    {
            if (!PuedeAccionarSubidaComprobante)
                return;

        if (UltimoPago is null || Cuenta is null)
        {
                ErrorComprobante = "Primero registra el pago con el boton verde y luego sube el comprobante.";
            Mensaje = ErrorComprobante;
            return;
        }

        var usuario = _session.Username ?? "desconocido";
            var subidaExitosa = await SubirComprobanteInternoAsync(Cuenta.Idcuenta, UltimoPago.Idpago, usuario);
            if (subidaExitosa)
            {
                Mensaje = "Comprobante subido correctamente.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(ErrorComprobante))
                Mensaje = ErrorComprobante;
    }

    /// <summary>
    /// Permite al cajero seleccionar una foto existente de la galería.
    /// </summary>
    [RelayCommand]
    public async Task SeleccionarFotoGaleriaAsync()
    {
        try
        {
            var archivo = await _comprobantes.SeleccionarDesdeGaleriaAsync();
            if (archivo is null) return;

            AplicarArchivoComprobante(archivo);
        }
        catch (PermissionException)
        {
            await Shell.Current.DisplayAlertAsync("Permiso denegado",
                "Se necesita permiso de galería para seleccionar el comprobante.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error",
                $"No se pudo seleccionar la foto: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public Task Volver() => _navigation.GoToAsync("..");

    private void RefrescarEstadoCobro()
    {
        OnPropertyChanged(nameof(TotalCobro));
        OnPropertyChanged(nameof(Saldo));
        OnPropertyChanged(nameof(Cambio));
        OnPropertyChanged(nameof(HayCambio));
        OnPropertyChanged(nameof(FaltaPorRecibir));
        OnPropertyChanged(nameof(TieneFaltanteEfectivo));
        OnPropertyChanged(nameof(PuedeCobrar));
        OnPropertyChanged(nameof(NotaMinimoEfectivo));
    }

    private void RefrescarEstadoComprobante()
    {
        OnPropertyChanged(nameof(TieneComprobante));
           OnPropertyChanged(nameof(PuedeAccionarSubidaComprobante));
        OnPropertyChanged(nameof(PuedeSubirComprobante));
    }

    private void AplicarArchivoComprobante(PagoComprobanteArchivo archivo)
    {
        LimpiarComprobanteTemporalActual();
        RutaArchivoComprobante = archivo.RutaArchivo;
        _rutaVistaPreviaComprobante = archivo.RutaVistaPrevia;
        ImagenComprobante = archivo.VistaPrevia;
    }

    private void NotificarPedidoActualizado()
    {
        OnPropertyChanged(nameof(Pedido));
        OnPropertyChanged(nameof(Consumo));
        OnPropertyChanged(nameof(TotalPedido));
        OnPropertyChanged(nameof(ClienteNombreTexto));
        OnPropertyChanged(nameof(CantidadProductosTexto));
        OnPropertyChanged(nameof(SubtituloPantalla));
        OnPropertyChanged(nameof(HoraAperturaTexto));
    }

    private void ReiniciarEstadoComprobante()
    {
        ComprobanteSubido = false;
        ErrorComprobante = string.Empty;
        IntentosSubida = 0;
        UltimoComprobante = null;
    }

    private void LimpiarComprobanteTemporalActual()
    {
        _comprobantes.LimpiarArchivoTemporal(RutaArchivoComprobante, _rutaVistaPreviaComprobante);
        _rutaVistaPreviaComprobante = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LÓGICA INTERNA
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<bool> SubirComprobanteInternoAsync(int idCuenta, int idPago, string usuario)
    {
        if (string.IsNullOrEmpty(RutaArchivoComprobante)) return false;

        SubiendoComprobante = true;
        ErrorComprobante    = string.Empty;
        IntentosSubida++;

        try
        {
            var resultado = await _comprobantes.SubirAsync(
                idCuenta,
                idPago,
                RutaArchivoComprobante,
                usuario,
                IntentosSubida);

            if (!resultado.Exitoso)
            {
                ErrorComprobante = resultado.Error ?? "No se pudo subir el comprobante.";
                return false;
            }

            UltimoComprobante = resultado.Comprobante;
            ComprobanteSubido = true;
            ErrorComprobante  = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            ErrorComprobante = $"Error al subir comprobante: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[Comprobante] Error: {ex}");
        }
        finally
        {
            SubiendoComprobante = false;
        }

        return false;
    }

}
