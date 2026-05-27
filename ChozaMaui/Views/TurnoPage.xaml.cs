using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class TurnoPage : ContentPage
{
    private readonly TurnoViewModel _vm;

    public TurnoPage(TurnoViewModel vm)
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
