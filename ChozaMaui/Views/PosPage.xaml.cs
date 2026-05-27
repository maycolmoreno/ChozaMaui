using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class PosPage : ContentPage
{
    private readonly PosViewModel _vm;

    public PosPage(PosViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarSiEsNecesarioAsync();
    }
}
