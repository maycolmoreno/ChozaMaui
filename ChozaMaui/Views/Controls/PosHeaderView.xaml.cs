using System.Windows.Input;

namespace ChozaMaui.Views.Controls;

public partial class PosHeaderView : ContentView
{
    public static readonly BindableProperty InicialesUsuarioProperty =
        BindableProperty.Create(nameof(InicialesUsuario), typeof(string), typeof(PosHeaderView), "U");

    public static readonly BindableProperty NombreUsuarioProperty =
        BindableProperty.Create(nameof(NombreUsuario), typeof(string), typeof(PosHeaderView), "Usuario");

    public static readonly BindableProperty RolUsuarioProperty =
        BindableProperty.Create(nameof(RolUsuario), typeof(string), typeof(PosHeaderView), "Usuario");

    public static readonly BindableProperty Kpi1TituloProperty =
        BindableProperty.Create(nameof(Kpi1Titulo), typeof(string), typeof(PosHeaderView), string.Empty);

    public static readonly BindableProperty Kpi1ValorProperty =
        BindableProperty.Create(nameof(Kpi1Valor), typeof(string), typeof(PosHeaderView), string.Empty);

    public static readonly BindableProperty Kpi2TituloProperty =
        BindableProperty.Create(nameof(Kpi2Titulo), typeof(string), typeof(PosHeaderView), string.Empty);

    public static readonly BindableProperty Kpi2ValorProperty =
        BindableProperty.Create(nameof(Kpi2Valor), typeof(string), typeof(PosHeaderView), string.Empty);

    public static readonly BindableProperty Kpi3TituloProperty =
        BindableProperty.Create(nameof(Kpi3Titulo), typeof(string), typeof(PosHeaderView), string.Empty);

    public static readonly BindableProperty Kpi3ValorProperty =
        BindableProperty.Create(nameof(Kpi3Valor), typeof(string), typeof(PosHeaderView), string.Empty);

    public static readonly BindableProperty TotalAlertasProperty =
        BindableProperty.Create(nameof(TotalAlertas), typeof(int), typeof(PosHeaderView), 0,
            propertyChanged: (bindable, _, _) => ((PosHeaderView)bindable).OnPropertyChanged(nameof(TieneAlertas)));

    public static readonly BindableProperty NotificacionesCommandProperty =
        BindableProperty.Create(nameof(NotificacionesCommand), typeof(ICommand), typeof(PosHeaderView));

    public static readonly BindableProperty RefreshCommandProperty =
        BindableProperty.Create(nameof(RefreshCommand), typeof(ICommand), typeof(PosHeaderView));

    public static readonly BindableProperty BackCommandProperty =
        BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(PosHeaderView));

    public static readonly BindableProperty MostrarVolverProperty =
        BindableProperty.Create(nameof(MostrarVolver), typeof(bool), typeof(PosHeaderView), false);

    public PosHeaderView()
    {
        InitializeComponent();
    }

    public string InicialesUsuario
    {
        get => (string)GetValue(InicialesUsuarioProperty);
        set => SetValue(InicialesUsuarioProperty, value);
    }

    public string NombreUsuario
    {
        get => (string)GetValue(NombreUsuarioProperty);
        set => SetValue(NombreUsuarioProperty, value);
    }

    public string RolUsuario
    {
        get => (string)GetValue(RolUsuarioProperty);
        set => SetValue(RolUsuarioProperty, value);
    }

    public string Kpi1Titulo
    {
        get => (string)GetValue(Kpi1TituloProperty);
        set => SetValue(Kpi1TituloProperty, value);
    }

    public string Kpi1Valor
    {
        get => (string)GetValue(Kpi1ValorProperty);
        set => SetValue(Kpi1ValorProperty, value);
    }

    public string Kpi2Titulo
    {
        get => (string)GetValue(Kpi2TituloProperty);
        set => SetValue(Kpi2TituloProperty, value);
    }

    public string Kpi2Valor
    {
        get => (string)GetValue(Kpi2ValorProperty);
        set => SetValue(Kpi2ValorProperty, value);
    }

    public string Kpi3Titulo
    {
        get => (string)GetValue(Kpi3TituloProperty);
        set => SetValue(Kpi3TituloProperty, value);
    }

    public string Kpi3Valor
    {
        get => (string)GetValue(Kpi3ValorProperty);
        set => SetValue(Kpi3ValorProperty, value);
    }

    public int TotalAlertas
    {
        get => (int)GetValue(TotalAlertasProperty);
        set => SetValue(TotalAlertasProperty, value);
    }

    public bool TieneAlertas => TotalAlertas > 0;

    public ICommand? NotificacionesCommand
    {
        get => (ICommand?)GetValue(NotificacionesCommandProperty);
        set => SetValue(NotificacionesCommandProperty, value);
    }

    public ICommand? RefreshCommand
    {
        get => (ICommand?)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }

    public ICommand? BackCommand
    {
        get => (ICommand?)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    public bool MostrarVolver
    {
        get => (bool)GetValue(MostrarVolverProperty);
        set => SetValue(MostrarVolverProperty, value);
    }
}
