﻿using System.Globalization;
using System.Windows.Data;

namespace Rees.Wpf.Converters;

/// <summary>
///     A converter to convert any object to a string. Effectively calls .ToString() on the object if it is non-null.
///     This is particularly useful when binding to a ToolTip, because ToolTip is a System.Object, not a String, so the
///     standard StringFormat binding
///     parameter is ignored.
/// </summary>
public class ConvertToStringConverter : IValueConverter
{
    /// <summary>
    ///     Converts a value to a string.
    ///     If null is given, null is returned.
    /// </summary>
    /// <returns>
    ///     A converted value. If the method returns null, the valid null value is used.
    /// </returns>
    /// <param name="value">The value produced by the binding source.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return null;
        }

        if (parameter is null)
        {
            return value.ToString();
        }

        if (value is DateTime dateTimeValue)
        {
            return dateTimeValue.ToString(parameter.ToString());
        }

        if (value is int or long or ushort or uint or ulong)
        {
            return System.Convert.ToInt64(value).ToString(parameter.ToString());
        }

        return value is decimal or double or float
            ? System.Convert.ToDouble(value).ToString(parameter.ToString())
            : (object?)value.ToString();
    }

    /// <summary>
    ///     Not Supported in this implementation.
    /// </summary>
    /// <exception cref="System.NotSupportedException"></exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
