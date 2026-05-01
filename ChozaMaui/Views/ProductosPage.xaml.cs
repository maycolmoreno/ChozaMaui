using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class ProductosPage : ContentPage
{
    private readonly ProductosViewModel _vm;

    public ProductosPage(ProductosViewModel vm)
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
