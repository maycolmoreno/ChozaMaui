using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class ClientesPage : ContentPage
{
    private readonly ClientesViewModel _vm;

    public ClientesPage(ClientesViewModel vm)
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
