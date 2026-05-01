using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChozaMaui.Models;
using ChozaMaui.Services;

namespace ChozaMaui.ViewModels;

public partial class ProductosViewModel : ObservableObject
{
    private readonly ApiService _api;

    // ── Datos ─────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ProductoResponse> productos = new();
    [ObservableProperty] private ObservableCollection<CategoriaResponse> categorias = new();

    // ── Selección / filtro ────────────────────────────────────────
    [ObservableProperty] private CategoriaResponse? categoriaFiltro;
    [ObservableProperty] private string textoBusqueda = string.Empty;
    private List<ProductoResponse> _todos = [];

    // ── Formulario producto ───────────────────────────────────────
    [ObservableProperty] private bool mostrarFormulario;
    [ObservableProperty] private bool esEdicion;
    [ObservableProperty] private ProductoResponse? productoSeleccionado;
    [ObservableProperty] private string formNombre = string.Empty;
    [ObservableProperty] private string formPrecio = string.Empty;
    [ObservableProperty] private string formStock = string.Empty;
    [ObservableProperty] private string formDescripcion = string.Empty;
    [ObservableProperty] private string formImagenUrl = string.Empty;
    [ObservableProperty] private bool formEstado = true;
    [ObservableProperty] private CategoriaResponse? formCategoria;

    // ── Pestaña activa: "Productos" | "Categorias" ────────────────
    [ObservableProperty] private bool mostrarCategorias;

    // ── Formulario categoría ──────────────────────────────────────
    [ObservableProperty] private bool mostrarFormCategoria;
    [ObservableProperty] private bool esEdicionCategoria;
    [ObservableProperty] private CategoriaResponse? categoriaSeleccionada;
    [ObservableProperty] private string formCatNombre = string.Empty;
    [ObservableProperty] private string formCatDescripcion = string.Empty;
    [ObservableProperty] private bool formCatEstado = true;

    // ── Estado UI ─────────────────────────────────────────────────
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mensaje = string.Empty;
    [ObservableProperty] private bool mensajeExito;

    public ProductosViewModel(ApiService api) => _api = api;

    // ═══════════════════════════════════════════════════════════════
    // Carga inicial
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand]
    public async Task CargarAsync()
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var tareaProds = _api.GetTodosProductosAsync();
            var tareaCats  = _api.GetTodasCategoriasAsync();
            await Task.WhenAll(tareaProds, tareaCats);

            _todos = tareaProds.Result;
            Categorias = new ObservableCollection<CategoriaResponse>(tareaCats.Result);
            AplicarFiltro();
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; MensajeExito = false; }
        finally { IsBusy = false; }
    }

    // ═══════════════════════════════════════════════════════════════
    // Filtrado
    // ═══════════════════════════════════════════════════════════════
    partial void OnTextoBusquedaChanged(string value) => AplicarFiltro();
    partial void OnCategoriaFiltroChanged(CategoriaResponse? value) => AplicarFiltro();

    private void AplicarFiltro()
    {
        var lista = _todos.AsEnumerable();
        if (CategoriaFiltro is not null)
            lista = lista.Where(p => p.CategoriaId == CategoriaFiltro.Idcategoria);
        if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            lista = lista.Where(p => p.Nombre.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase));
        Productos = new ObservableCollection<ProductoResponse>(lista);
    }

    [RelayCommand] public void LimpiarFiltro() { CategoriaFiltro = null; TextoBusqueda = string.Empty; }

    // ═══════════════════════════════════════════════════════════════
    // Pestaña
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand] public void VerProductos() => MostrarCategorias = false;
    [RelayCommand] public void VerCategorias() => MostrarCategorias = true;

    // ═══════════════════════════════════════════════════════════════
    // Formulario PRODUCTO
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand]
    public void AbrirNuevoProducto()
    {
        EsEdicion = false;
        ProductoSeleccionado = null;
        LimpiarFormProducto();
        MostrarFormulario = true;
        Mensaje = string.Empty;
    }

    [RelayCommand]
    public void AbrirEdicionProducto(ProductoResponse p)
    {
        EsEdicion = true;
        ProductoSeleccionado = p;
        FormNombre     = p.Nombre;
        FormPrecio     = p.Precio.ToString("F2");
        FormStock      = p.StockActual.ToString();
        FormDescripcion = p.Descripcion;
        FormImagenUrl  = p.ImagenUrl ?? string.Empty;
        FormEstado     = p.Estado;
        FormCategoria  = Categorias.FirstOrDefault(c => c.Idcategoria == p.CategoriaId);
        MostrarFormulario = true;
        Mensaje = string.Empty;
    }

    [RelayCommand] public void CancelarFormulario() { MostrarFormulario = false; LimpiarFormProducto(); }

    [RelayCommand]
    public async Task GuardarProductoAsync()
    {
        if (string.IsNullOrWhiteSpace(FormNombre) || FormCategoria is null)
        {
            Mensaje = "Nombre y categoría son obligatorios.";
            MensajeExito = false;
            return;
        }
        if (!double.TryParse(FormPrecio.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var precio) || precio <= 0)
        {
            Mensaje = "Precio inválido.";
            MensajeExito = false;
            return;
        }
        if (!int.TryParse(FormStock, out var stock) || stock < 0)
        {
            Mensaje = "Stock inválido.";
            MensajeExito = false;
            return;
        }

        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var req = new ProductoRequest
            {
                Nombre      = FormNombre.Trim(),
                Precio      = precio,
                StockActual = stock,
                Descripcion = FormDescripcion.Trim(),
                ImagenUrl   = string.IsNullOrWhiteSpace(FormImagenUrl) ? null : FormImagenUrl.Trim(),
                Estado      = FormEstado,
                CategoriaId = FormCategoria.Idcategoria
            };
            if (EsEdicion && ProductoSeleccionado is not null)
                await _api.ActualizarProductoAsync(ProductoSeleccionado.Idproducto, req);
            else
                await _api.CrearProductoAsync(req);

            Mensaje = EsEdicion ? "Producto actualizado." : "Producto creado.";
            MensajeExito = true;
            MostrarFormulario = false;
            LimpiarFormProducto();
            await CargarAsync();
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; MensajeExito = false; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task EliminarProductoAsync(ProductoResponse p)
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            await _api.EliminarProductoAsync(p.Idproducto);
            Mensaje = "Producto eliminado.";
            MensajeExito = true;
            await CargarAsync();
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; MensajeExito = false; }
        finally { IsBusy = false; }
    }

    // ═══════════════════════════════════════════════════════════════
    // Formulario CATEGORÍA
    // ═══════════════════════════════════════════════════════════════
    [RelayCommand]
    public void AbrirNuevaCategoria()
    {
        EsEdicionCategoria = false;
        CategoriaSeleccionada = null;
        FormCatNombre = FormCatDescripcion = string.Empty;
        FormCatEstado = true;
        MostrarFormCategoria = true;
        Mensaje = string.Empty;
    }

    [RelayCommand]
    public void AbrirEdicionCategoria(CategoriaResponse c)
    {
        EsEdicionCategoria = true;
        CategoriaSeleccionada = c;
        FormCatNombre = c.Nombre;
        FormCatDescripcion = c.Descripcion;
        FormCatEstado = c.Estado;
        MostrarFormCategoria = true;
        Mensaje = string.Empty;
    }

    [RelayCommand] public void CancelarFormCategoria() { MostrarFormCategoria = false; }

    [RelayCommand]
    public async Task GuardarCategoriaAsync()
    {
        if (string.IsNullOrWhiteSpace(FormCatNombre) || string.IsNullOrWhiteSpace(FormCatDescripcion))
        {
            Mensaje = "Nombre y descripción son obligatorios.";
            MensajeExito = false;
            return;
        }
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            var req = new CategoriaRequest
            {
                Nombre      = FormCatNombre.Trim(),
                Descripcion = FormCatDescripcion.Trim(),
                Estado      = FormCatEstado
            };
            if (EsEdicionCategoria && CategoriaSeleccionada is not null)
                await _api.ActualizarCategoriaAsync(CategoriaSeleccionada.Idcategoria, req);
            else
                await _api.CrearCategoriaAsync(req);

            Mensaje = EsEdicionCategoria ? "Categoría actualizada." : "Categoría creada.";
            MensajeExito = true;
            MostrarFormCategoria = false;
            await CargarAsync();
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; MensajeExito = false; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task EliminarCategoriaAsync(CategoriaResponse c)
    {
        IsBusy = true;
        Mensaje = string.Empty;
        try
        {
            await _api.EliminarCategoriaAsync(c.Idcategoria);
            Mensaje = "Categoría eliminada.";
            MensajeExito = true;
            await CargarAsync();
        }
        catch (Exception ex) { Mensaje = $"Error: {ex.Message}"; MensajeExito = false; }
        finally { IsBusy = false; }
    }

    // ═══════════════════════════════════════════════════════════════
    private void LimpiarFormProducto()
    {
        FormNombre = FormPrecio = FormStock = FormDescripcion = FormImagenUrl = string.Empty;
        FormEstado = true;
        FormCategoria = null;
    }
}
