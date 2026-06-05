using ChozaMaui.ViewModels;
using ChozaMaui.Views.Controls;

namespace ChozaMaui.Views;

public partial class PosPage : ContentPage
{
    private readonly PosViewModel _vm;
    private bool _loadedLogged;
    private bool _pedidoSheetLoaded, _clienteSheetLoaded, _exitoLoaded;

    public PosPage(PosViewModel vm)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        BindingContext = _vm = vm;
        SubscribirSheetsCargaLazy();
        Loaded += (_, _) =>
        {
            if (_loadedLogged)
                return;

            _loadedLogged = true;
            System.Diagnostics.Debug.WriteLine($"[PERF][PosPage] Loaded/primer render aproximado: {sw.ElapsedMilliseconds} ms");
        };
        System.Diagnostics.Debug.WriteLine($"[PERF][PosPage] Constructor+InitializeComponent: {sw.ElapsedMilliseconds} ms");
    }

    protected override async void OnAppearing()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine($"[PERF][PosPage] OnAppearing antes de cargar datos: {sw.ElapsedMilliseconds} ms");
        await _vm.CargarSiEsNecesarioAsync();
        System.Diagnostics.Debug.WriteLine($"[PERF][PosPage] OnAppearing total con carga: {sw.ElapsedMilliseconds} ms");
    }

    private void SubscribirSheetsCargaLazy()
    {
        _vm.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(PosViewModel.MostrarPedidoSheet) when _vm.MostrarPedidoSheet && !_pedidoSheetLoaded:
                    _pedidoSheetLoaded = true;
                    PedidoSheetContainer.Children.Add(new PedidoBottomSheetView { BindingContext = _vm });
                    break;
                case nameof(PosViewModel.MostrarClienteSheet) when _vm.MostrarClienteSheet && !_clienteSheetLoaded:
                    _clienteSheetLoaded = true;
                    ClienteSheetContainer.Children.Add(new ClienteBottomSheetView { BindingContext = _vm });
                    break;
                case nameof(PosViewModel.MostrarExito) when _vm.MostrarExito && !_exitoLoaded:
                    _exitoLoaded = true;
                    ExitoContainer.Children.Add(new PosExitoView { BindingContext = _vm });
                    break;
            }
        };
    }
}
