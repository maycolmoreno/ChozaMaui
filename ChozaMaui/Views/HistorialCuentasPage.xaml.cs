using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class HistorialCuentasPage : ContentPage
{
    private readonly HistorialCuentasViewModel _vm;

    public HistorialCuentasPage(HistorialCuentasViewModel vm)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        System.Diagnostics.Debug.WriteLine($"[PERF][HistorialCuentasPage] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    protected override async void OnAppearing()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        base.OnAppearing();
        await Task.Delay(120);
        await _vm.CargarSiEsNecesarioAsync();
        System.Diagnostics.Debug.WriteLine($"[PERF][HistorialCuentasPage] OnAppearing+Cargar: {sw.ElapsedMilliseconds} ms");
    }
}
