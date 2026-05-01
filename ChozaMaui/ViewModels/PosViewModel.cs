using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class PosViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SessionService _session;

    // ── Listas base ────────────────────────────────────────────────────
    public ObservableCollection<MesaResponse> Mesas { get; } = [];
    public ObservableCollection<CategoriaResponse> Categorias { get; } = [];
    public ObservableCollection<ProductoResponse> Productos { get; } = [];
    public ObservableCollection<ItemCarrito> Carrito { get; } = [];

    // ── Selecciones ───────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MesaSeleccionadaTexto))]
    private MesaResponse? mesaSeleccionada;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClienteTexto))]
    private ClienteResponse? clienteSeleccionado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalTexto))]
    private double total;

    [ObservableProperty] private string observaciones = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private bool mostrarMensaje;
    [ObservableProperty] private bool mensajeEsError;

    // ── Cámara ────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneFoto))]
    private FotoAdjunta? fotoAdjunta;

    public bool TieneFoto => FotoAdjunta is not null;
    public string FotoTexto => FotoAdjunta is null ? "Adjuntar foto" : $"📷 {FotoAdjunta.NombreArchivo}";

    [ObservableProperty] private bool mostrarCarrito;

    public string MesaSeleccionadaTexto =>
        MesaSeleccionada is null ? "Sin mesa" : MesaSeleccionada.Etiqueta;

    public string ClienteTexto =>
        ClienteSeleccionado is null ? "👤 Asignar cliente" : $"👤 {ClienteSeleccionado.Nombre}  ·  {ClienteSeleccionado.Cedula}";

    public int TotalItems => Carrito.Sum(i => i.Cantidad);
    public string TotalTexto => $"${Total:0.00}";
    public string BienvenidaTexto => $"Bienvenido, {_session.NombreCompleto ?? _session.Username}";

    public PosViewModel(ApiService api, SessionService session)
    {
        _api = api;
        _session = session;
    }

    // ── Carga inicial ─────────────────────────────────────────────────

    [RelayCommand]
    public async Task CargarDatosAsync()
    {
        IsBusy = true;
        try
        {
            var mesasTask = _api.GetTodasMesasAsync();
            var catTask = _api.GetCategoriasActivasAsync();
            var prodTask = _api.GetProductosActivosAsync();

            await Task.WhenAll(mesasTask, catTask, prodTask);

            Mesas.Clear();
            foreach (var m in mesasTask.Result) Mesas.Add(m);

            Categorias.Clear();
            foreach (var c in catTask.Result) Categorias.Add(c);

            Productos.Clear();
            foreach (var p in prodTask.Result) Productos.Add(p);
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error cargando datos: {ex.Message}", error: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Filtro por categoría ──────────────────────────────────────────

    [RelayCommand]
    public async Task FiltrarPorCategoriaAsync(CategoriaResponse categoria)
    {
        IsBusy = true;
        try
        {
            var lista = await _api.GetProductosPorCategoriaAsync(categoria.Idcategoria);
            Productos.Clear();
            foreach (var p in lista) Productos.Add(p);
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error filtrando: {ex.Message}", error: true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task MostrarTodosAsync()
    {
        IsBusy = true;
        try
        {
            var lista = await _api.GetProductosActivosAsync();
            Productos.Clear();
            foreach (var p in lista) Productos.Add(p);
        }
        finally { IsBusy = false; }
    }

    // ── Carrito ───────────────────────────────────────────────────────

    [RelayCommand]
    public async Task AgregarAlCarritoAsync(ProductoResponse producto)
    {
        if (producto.StockActual <= 0)
        {
            await MostrarMensajeAsync($"{producto.Nombre} no tiene stock disponible.", error: true);
            return;
        }

        var existente = Carrito.FirstOrDefault(i => i.Producto.Idproducto == producto.Idproducto);
        if (existente is not null)
        {
            if (existente.Cantidad >= producto.StockActual)
            {
                await MostrarMensajeAsync(
                    $"Solo hay {producto.StockActual} unidad(es) disponibles de {producto.Nombre}.",
                    error: true);
                return;
            }

            existente.Cantidad++;
        }
        else
        {
            Carrito.Add(new ItemCarrito { Producto = producto, Cantidad = 1 });
        }
        RecalcularTotal();
    }

    [RelayCommand]
    public void QuitarDelCarrito(ItemCarrito item)
    {
        if (item.Cantidad > 1)
        {
            item.Cantidad--;
        }
        else
        {
            Carrito.Remove(item);
        }
        RecalcularTotal();
    }

    [RelayCommand]
    public void LimpiarCarrito()
    {
        Carrito.Clear();
        Total = 0;
        FotoAdjunta = null;
        OnPropertyChanged(nameof(TotalItems));
    }

    private void RecalcularTotal()
    {
        Total = Carrito.Sum(i => i.Subtotal);
        OnPropertyChanged(nameof(TotalItems));
    }

    [RelayCommand]
    public async Task SeleccionarMesaAsync(MesaResponse mesa)
    {
        if (!mesa.Estado)
        {
            await Shell.Current.DisplayAlertAsync(
                "Mesa ocupada",
                $"La mesa #{mesa.Numero} está actualmente ocupada. No puedes asignarle un nuevo pedido.",
                "Entendido");
            return;
        }
        MesaSeleccionada = mesa;
    }

    [RelayCommand]
    public void ToggleCarrito() => MostrarCarrito = !MostrarCarrito;

    [RelayCommand]
    public async Task BuscarClienteAsync()
    {
        IsBusy = true;
        List<ClienteResponse> clientes;
        try
        {
            clientes = await _api.GetClientesAsync();
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error cargando clientes: {ex.Message}", error: true);
            return;
        }
        finally
        {
            // IsBusy se libera aquí, antes del ActionSheet, para no bloquear la UI
            IsBusy = false;
        }

        if (clientes.Count == 0)
        {
            await MostrarMensajeAsync("No hay clientes registrados.", error: true);
            return;
        }

        var opciones = clientes.Select(c => $"{c.Nombre}  ({c.Cedula})").ToArray();
        var elegida = await Shell.Current.DisplayActionSheetAsync(
            "Seleccionar cliente", "Cancelar", null, opciones);

        if (elegida is null || elegida == "Cancelar") return;

        var idx = Array.IndexOf(opciones, elegida);
        if (idx >= 0) ClienteSeleccionado = clientes[idx];
    }

    [RelayCommand]
    public void QuitarCliente() => ClienteSeleccionado = null;

    // ── Cámara (sensor del dispositivo) ──────────────────────────────

    [RelayCommand]
    public async Task TomarFotoAsync()
    {
        try
        {
            // Verificar soporte
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await MostrarMensajeAsync("Cámara no disponible en este dispositivo.", error: true);
                return;
            }

            var permiso = await Permissions.RequestAsync<Permissions.Camera>();
            if (permiso != PermissionStatus.Granted)
            {
                await MostrarMensajeAsync("Permiso de cámara denegado.", error: true);
                return;
            }

            var foto = await MediaPicker.Default.CapturePhotoAsync();
            if (foto is null) return;

            // Guardar en almacenamiento local de la app
            var carpeta = FileSystem.AppDataDirectory;
            var nombre = $"foto_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var destino = Path.Combine(carpeta, nombre);

            using var origen = await foto.OpenReadAsync();
            using var archivo = File.OpenWrite(destino);
            await origen.CopyToAsync(archivo);

            FotoAdjunta = new FotoAdjunta { RutaLocal = destino };
            OnPropertyChanged(nameof(FotoTexto));
            await MostrarMensajeAsync("Foto guardada correctamente.", error: false);
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error con cámara: {ex.Message}", error: true);
        }
    }

    [RelayCommand]
    public void QuitarFoto()
    {
        FotoAdjunta = null;
        OnPropertyChanged(nameof(FotoTexto));
    }

    // ── Enviar pedido ─────────────────────────────────────────────────

    [RelayCommand]
    public async Task EnviarPedidoAsync()
    {
        if (MesaSeleccionada is null)
        {
            await MostrarMensajeAsync("Selecciona una mesa antes de enviar.", error: true);
            return;
        }
        if (Carrito.Count == 0)
        {
            await MostrarMensajeAsync("El carrito está vacío.", error: true);
            return;
        }

        var errorStock = await ValidarStockCarritoAsync();
        if (errorStock is not null)
        {
            await MostrarMensajeAsync(errorStock, error: true);
            return;
        }

        IsBusy = true;
        try
        {
            // Incluir referencia a foto en las observaciones si existe
            var obs = Observaciones;
            if (FotoAdjunta is not null)
                obs = string.IsNullOrWhiteSpace(obs)
                    ? $"[Foto: {FotoAdjunta.NombreArchivo}]"
                    : $"{obs} [Foto: {FotoAdjunta.NombreArchivo}]";

            var request = new PedidoRequest
            {
                Fecha = DateTime.Now.ToString("s"),  // asignado justo al enviar
                IdUsuario = _session.UserId,
                IdMesa = MesaSeleccionada.Idmesa,
                IdCliente = ClienteSeleccionado?.Idcliente ?? 0,
                Observaciones = obs,
                Detalles = Carrito.Select(i => new PedidoDetalleRequest
                {
                    IdProducto = i.Producto.Idproducto,
                    Cantidad = i.Cantidad,
                    PrecioUnitario = i.Producto.Precio
                }).ToList()
            };

            var pedido = await _api.CrearPedidoAsync(request);

            // Vincular pedido a una cuenta abierta (crear si no existe)
            int idCliente = ClienteSeleccionado?.Idcliente ?? 0;
            var cuentasAbiertas = await _api.ObtenerCuentasAbiertasAsync();
            var cuentaExistente = cuentasAbiertas
                .FirstOrDefault(c => c.Mesa?.Idmesa == MesaSeleccionada.Idmesa
                                  && c.Cliente?.Idcliente == idCliente);

            if (cuentaExistente is not null)
            {
                await _api.AgregarPedidoACuentaAsync(cuentaExistente.Idcuenta, pedido.Idpedido);
            }
            else
            {
                var nuevaCuenta = await _api.CrearCuentaAsync(MesaSeleccionada.Idmesa, idCliente, pedido.Total);
                await _api.AgregarPedidoACuentaAsync(nuevaCuenta.Idcuenta, pedido.Idpedido);
            }

            LimpiarCarrito();
            MostrarCarrito = false;
            MesaSeleccionada = null;
            ClienteSeleccionado = null;
            Observaciones = string.Empty;
            await CargarDatosAsync();
            await MostrarMensajeAsync($"Pedido #{pedido.Idpedido} creado correctamente.", error: false);
        }
        catch (Exception ex)
        {
            await MostrarMensajeAsync($"Error al enviar pedido: {ex.Message}", error: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private async Task MostrarMensajeAsync(string texto, bool error)
    {
        MensajeEsError = error;
        Mensaje = texto;
        MostrarMensaje = true;
        await Task.Delay(3000);
        MostrarMensaje = false;
        Mensaje = string.Empty;
    }

    private async Task<string?> ValidarStockCarritoAsync()
    {
        try
        {
            var productosActuales = await _api.GetProductosActivosAsync();
            foreach (var item in Carrito)
            {
                var productoActual = productosActuales
                    .FirstOrDefault(p => p.Idproducto == item.Producto.Idproducto);

                if (productoActual is null || !productoActual.Estado)
                    return $"{item.Producto.Nombre} ya no está disponible.";

                if (item.Cantidad > productoActual.StockActual)
                    return $"Stock insuficiente para {productoActual.Nombre}. Disponible: {productoActual.StockActual}.";
            }

            return null;
        }
        catch (Exception ex)
        {
            return $"No se pudo validar el stock: {ex.Message}";
        }
    }
}
