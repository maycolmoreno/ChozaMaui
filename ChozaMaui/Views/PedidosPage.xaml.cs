using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class PedidosPage : ContentPage
{
    private readonly PedidosViewModel _vm;

    public PedidosPage(PedidosViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.IniciarPollingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.DetenerPolling();
    }
}
