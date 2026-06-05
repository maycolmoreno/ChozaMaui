using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class MesaDetallePage : ContentPage
{
    private readonly MesaDetalleViewModel _vm;

    public MesaDetallePage(MesaDetalleViewModel vm)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        System.Diagnostics.Debug.WriteLine($"[PERF][MesaDetallePage] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    protected override async void OnAppearing()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        base.OnAppearing();
        await Task.Delay(120);
        await _vm.CargarSiEsNecesarioAsync();
        System.Diagnostics.Debug.WriteLine($"[PERF][MesaDetallePage] OnAppearing+Cargar: {sw.ElapsedMilliseconds} ms");
    }
}
