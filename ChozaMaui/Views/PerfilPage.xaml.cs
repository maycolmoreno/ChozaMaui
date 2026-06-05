using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class PerfilPage : ContentPage
{
    public PerfilPage(PerfilViewModel vm)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        BindingContext = vm;
        System.Diagnostics.Debug.WriteLine($"[PERF][PerfilPage] Constructor+InitializeComponent: {sw.ElapsedMilliseconds} ms");
    }

    protected override void OnAppearing()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        base.OnAppearing();
        if (BindingContext is PerfilViewModel vm)
            vm.CargarPerfilCommand.Execute(null);
        System.Diagnostics.Debug.WriteLine($"[PERF][PerfilPage] OnAppearing: {sw.ElapsedMilliseconds} ms");
    }
}
