using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class NotificacionesPage : ContentPage
{
    public NotificacionesPage(NotificacionesViewModel vm)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        BindingContext = vm;
        System.Diagnostics.Debug.WriteLine($"[PERF][NotificacionesPage] Constructor: {sw.ElapsedMilliseconds} ms");
    }

    protected override async void OnAppearing()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        base.OnAppearing();
        await Task.Delay(120);
        if (BindingContext is NotificacionesViewModel vm)
        {
            vm.Iniciar();
            vm.CargarCommand.Execute(null);
        }
        System.Diagnostics.Debug.WriteLine($"[PERF][NotificacionesPage] OnAppearing+Cargar: {sw.ElapsedMilliseconds} ms");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is NotificacionesViewModel vm)
            vm.Detener();
    }
}
