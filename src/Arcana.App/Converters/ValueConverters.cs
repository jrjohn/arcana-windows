using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Arcana.App.Converters;

/// <summary>
/// Converts null to visibility.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

/// <summary>
/// Converts boolean to visibility.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

/// <summary>
/// Converts inverse boolean to visibility.
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return true;
    }
}

/// <summary>
/// Converts string empty/null to visibility.
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Formats numbers with thousand separators (N0 format).
/// </summary>
public class NumberFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null) return string.Empty;

        var format = parameter as string ?? "N0";

        return value switch
        {
            decimal d => d.ToString(format),
            double dbl => dbl.ToString(format),
            float f => f.ToString(format),
            int i => i.ToString(format),
            long l => l.ToString(format),
            _ => value.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Formats DateTime values.
/// </summary>
public class DateFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null) return string.Empty;

        var format = parameter as string ?? "yyyy/MM/dd";

        return value switch
        {
            DateTime dt => dt.ToString(format),
            DateTimeOffset dto => dto.ToString(format),
            _ => value.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Formats bytes with suffix.
/// </summary>
public class BytesFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null) return string.Empty;

        long bytes = value switch
        {
            long l => l,
            int i => i,
            double d => (long)d,
            _ => 0
        };

        return $"{bytes:N0} bytes";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts status enum values to localized strings.
/// Usage: ConverterParameter = "order.status" or "payment.status"
/// </summary>
public class StatusLocalizationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null) return string.Empty;

        var prefix = parameter as string ?? "order.status";
        var statusKey = value.ToString()?.ToLowerInvariant() ?? string.Empty;
        var localizationKey = $"{prefix}.{statusKey}";

        try
        {
            var localization = App.Services.GetService(typeof(Arcana.Plugins.Contracts.LocalizationService))
                as Arcana.Plugins.Contracts.LocalizationService;

            if (localization != null)
            {
                var localizedValue = localization.Get(localizationKey);
                // If the key is not found, return the original value
                if (!string.IsNullOrEmpty(localizedValue) && localizedValue != localizationKey)
                {
                    return localizedValue;
                }
            }
        }
        catch
        {
            // Fallback to original value if localization fails
        }

        return value.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
