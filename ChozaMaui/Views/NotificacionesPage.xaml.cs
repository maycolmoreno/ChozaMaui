using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class NotificacionesPage : ContentPage
{
    public NotificacionesPage(NotificacionesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is NotificacionesViewModel vm)
            vm.CargarCommand.Execute(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is NotificacionesViewModel vm)
            vm.Detener();
    }
}
