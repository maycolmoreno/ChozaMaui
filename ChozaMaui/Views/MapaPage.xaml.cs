using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class MapaPage : ContentPage
{
    private readonly MapaViewModel _vm;

    public MapaPage(MapaViewModel vm)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        System.Diagnostics.Debug.WriteLine($"[PERF][MapaPage] Constructor+InitializeComponent: {sw.ElapsedMilliseconds} ms");
    }

    protected override async void OnAppearing()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine($"[PERF][MapaPage] OnAppearing antes polling: {sw.ElapsedMilliseconds} ms");
        await _vm.IniciarPollingAsync();
        System.Diagnostics.Debug.WriteLine($"[PERF][MapaPage] OnAppearing total con polling: {sw.ElapsedMilliseconds} ms");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.DetenerPolling();
    }
}
