using ChozaMaui.ViewModels;

namespace ChozaMaui.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        BindingContext = vm;
        System.Diagnostics.Debug.WriteLine($"[PERF][LoginPage] Constructor+InitializeComponent: {sw.ElapsedMilliseconds} ms");
    }
}
