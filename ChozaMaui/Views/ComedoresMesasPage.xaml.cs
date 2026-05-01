using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class ComedoresMesasPage : ContentPage
{
    private readonly ComedoresMesasViewModel _vm;

    public ComedoresMesasPage(ComedoresMesasViewModel vm)
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
