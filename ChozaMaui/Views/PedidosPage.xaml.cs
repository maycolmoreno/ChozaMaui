using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class PedidosPage : ContentPage
{
    private readonly PedidosViewModel _vm;

    public PedidosPage(PedidosViewModel vm)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        BindingContext = _vm = vm;
        System.Diagnostics.Debug.WriteLine($"[PERF][PedidosPage] Constructor+InitializeComponent: {sw.ElapsedMilliseconds} ms");
    }

    protected override async void OnAppearing()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine($"[PERF][PedidosPage] OnAppearing antes polling: {sw.ElapsedMilliseconds} ms");
        await _vm.IniciarPollingAsync();
        System.Diagnostics.Debug.WriteLine($"[PERF][PedidosPage] OnAppearing total con polling: {sw.ElapsedMilliseconds} ms");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.DetenerPolling();
    }
}
