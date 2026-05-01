using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class HistorialCuentasPage : ContentPage
{
    private readonly HistorialCuentasViewModel _vm;

    public HistorialCuentasPage(HistorialCuentasViewModel vm)
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
