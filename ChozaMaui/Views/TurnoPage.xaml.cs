using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class TurnoPage : ContentPage
{
    private readonly TurnoViewModel _vm;

    public TurnoPage(TurnoViewModel vm)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        System.Diagnostics.Debug.WriteLine($"[PERF][TurnoPage] Constructor+InitializeComponent: {sw.ElapsedMilliseconds} ms");
    }

    protected override async void OnAppearing()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        base.OnAppearing();
        await Task.Delay(120);
        await _vm.CargarSiEsNecesarioAsync();
        System.Diagnostics.Debug.WriteLine($"[PERF][TurnoPage] OnAppearing total: {sw.ElapsedMilliseconds} ms");
    }
}
