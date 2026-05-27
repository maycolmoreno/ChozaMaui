using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class MesaDetallePage : ContentPage
{
    private readonly MesaDetalleViewModel _vm;

    public MesaDetallePage(MesaDetalleViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarSiEsNecesarioAsync();
    }
}
