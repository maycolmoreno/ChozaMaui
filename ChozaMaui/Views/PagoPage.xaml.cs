using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class PagoPage : ContentPage
{
    private readonly PagoViewModel _vm;

    public PagoPage(PagoViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarAsync();
    }
}
