using System.Globalization;

namespace Steel_structures_nodes_public_project.Maui.Converters;

/// <summary>
/// Инвертирует bool: true → false, false → true.
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}
