using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class PerfilPage : ContentPage
{
    public PerfilPage(PerfilViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PerfilViewModel vm)
            vm.CargarPerfilCommand.Execute(null);
    }
}
