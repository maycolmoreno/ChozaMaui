using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class MapaPage : ContentPage
{
    private readonly MapaViewModel _vm;

    public MapaPage(MapaViewModel vm)
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
