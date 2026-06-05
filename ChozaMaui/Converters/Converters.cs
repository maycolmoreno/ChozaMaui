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

/// <summary>Devuelve true si el entero es distinto de cero.</summary>
public class IntIsNotZeroConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		=> value is int i ? i != 0 : value is System.Collections.ICollection c ? c.Count != 0 : false;

	public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		=> throw new NotImplementedException();
}

/// <summary>
/// Convierte bool a uno de dos colores pasados como "ColorVerdadero|ColorFalso" en ConverterParameter.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
	{
		var flag = value is bool b && b;
		var parts = (parameter as string)?.Split('|');
		var hex = flag ? parts?[0] : parts?[1];
		if (hex is not null)
		{
			try { return Color.FromArgb(hex); }
			catch { }
		}
		return Colors.Transparent;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		=> throw new NotImplementedException();
}

/// <summary>
/// Convierte un string hexadecimal de color (#RRGGBB o #AARRGGBB) a Microsoft.Maui.Graphics.Color.
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

/// <summary>
/// Compara value (string) con la primera parte del parámetro "match|trueHex|falseHex".
/// Devuelve Color trueHex si son iguales, falseHex si no.
/// Uso: ConverterParameter='Todas|#F97316|#F1F5F9'
/// </summary>
public class StringEqualsColorConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
	{
		var parts = (parameter as string)?.Split('|');
		if (parts?.Length >= 3)
		{
			var match    = parts[0];
			var trueHex  = parts[1];
			var falseHex = parts[2];
			var hex = string.Equals(value as string, match, StringComparison.Ordinal) ? trueHex : falseHex;
			try { return Color.FromArgb(hex); }
			catch { }
		}
		return Colors.Transparent;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		=> throw new NotImplementedException();
}

/// <summary>
/// Convierte bool a uno de dos strings según el parámetro "TextoSiVerdadero|TextoSiFalso".
/// Ejemplo: ConverterParameter='Cambiar foto|📷 Tomar foto'
/// </summary>
public class BoolToStringConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
	{
		var flag  = value is bool b && b;
		var parts = (parameter as string)?.Split('|');
		return (flag ? parts?[0] : parts?[1]) ?? string.Empty;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		=> throw new NotImplementedException();
}

/// <summary>
/// Convierte un string a ImageSource. Si es una URL absoluta HTTP/HTTPS,
/// usa UriImageSource con cache explícito; en otros casos devuelve el string
/// original para no romper rutas locales o recursos embebidos.
/// </summary>
public class StringToCachedImageSourceConverter : IValueConverter
{
	private static readonly TimeSpan CacheValidity = TimeSpan.FromDays(7);

	public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
	{
		if (value is not string source || string.IsNullOrWhiteSpace(source))
			return null;

		if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
			(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
		{
			return new UriImageSource
			{
				Uri = uri,
				CachingEnabled = true,
				CacheValidity = CacheValidity
			};
		}

		return source;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		=> throw new NotImplementedException();
}
