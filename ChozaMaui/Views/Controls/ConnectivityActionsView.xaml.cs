using System.Windows.Input;
using ChozaMaui.Services;

namespace ChozaMaui.Views.Controls;

public partial class ConnectivityActionsView : ContentView
{
    public static readonly BindableProperty RefreshCommandProperty = BindableProperty.Create(
        nameof(RefreshCommand),
        typeof(ICommand),
        typeof(ConnectivityActionsView),
        default(ICommand),
        propertyChanged: OnRefreshCommandChanged);

    public ICommand? RefreshCommand
    {
        get => (ICommand?)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }

    public bool HasRefreshCommand => RefreshCommand is not null;

    public ConnectivityService? Connectivity { get; private set; }

    public ConnectivityActionsView()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        var connectivity = Handler?.MauiContext?.Services?.GetService<ConnectivityService>();
        if (connectivity is null || ReferenceEquals(Connectivity, connectivity))
            return;

        Connectivity = connectivity;
        OnPropertyChanged(nameof(Connectivity));
    }

    private static void OnRefreshCommandChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        if (bindable is ConnectivityActionsView view)
            view.OnPropertyChanged(nameof(HasRefreshCommand));
    }

    private async void OnVerifyTapped(object? sender, TappedEventArgs e)
    {
        if (Connectivity is null)
            return;

        await Connectivity.RefreshStatusAsync();
    }

    private void OnRefreshTapped(object? sender, TappedEventArgs e)
    {
        if (RefreshCommand?.CanExecute(null) == true)
            RefreshCommand.Execute(null);
    }
}