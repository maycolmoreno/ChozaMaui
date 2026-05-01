namespace ChozaMaui.Converters;

/// <summary>Devuelve true si el string no es null ni vacío.</summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => !string.IsNullOrEmpty(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Invierte un bool.</summary>
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>Devuelve true si el valor no es null.</summary>
public class NotNullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Devuelve true si la colección (ICollection) tiene 0 elementos.</summary>
public class IntIsZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is int i ? i == 0 : value is System.Collections.ICollection c ? c.Count == 0 : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Convierte un string hexadecimal de color (#RRGGBB o #AARRGGBB) a Microsoft.Maui.Graphics.Color.
/// Permite que los modelos devuelvan strings en lugar de Color, respetando SRP.
/// </summary>
public class HexToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try { return Color.FromArgb(hex); }
            catch { /* fallback */ }
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}
