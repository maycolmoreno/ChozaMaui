using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class PagoPage : ContentPage
{
    private readonly PagoViewModel _vm;

    public PagoPage(PagoViewModel vm)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        System.Diagnostics.Debug.WriteLine($"[PERF][PagoPage] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    protected override async void OnAppearing()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        base.OnAppearing();
        await Task.Delay(120);
        await _vm.CargarSiEsNecesarioAsync();
        System.Diagnostics.Debug.WriteLine($"[PERF][PagoPage] OnAppearing+Cargar: {sw.ElapsedMilliseconds} ms");
    }
}
