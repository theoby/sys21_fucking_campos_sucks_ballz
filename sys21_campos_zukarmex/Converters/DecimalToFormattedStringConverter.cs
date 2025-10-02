using System.Globalization;

namespace sys21_campos_zukarmex.Converters
{
    /// <summary>
    /// Converter que maneja la conversión entre decimal y string con formato de separadores de miles
    /// Usado para bindings bidireccionales con formato visual
    /// </summary>
    public class DecimalToFormattedStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                if (value is decimal decimalValue)
                {
                    // Si el valor es 0, mostrar texto vacío para mejor UX
                    if (decimalValue == 0)
                        return string.Empty;
                    
                    // Formatear con separadores de miles según la cultura actual
                    return decimalValue.ToString("N", CultureInfo.CurrentCulture);
                }
                
                if (value is double doubleValue)
                {
                    decimal convertedValue = (decimal)doubleValue;
                    if (convertedValue == 0)
                        return string.Empty;
                    
                    return convertedValue.ToString("N", CultureInfo.CurrentCulture);
                }
                
                if (value is float floatValue)
                {
                    decimal convertedValue = (decimal)floatValue;
                    if (convertedValue == 0)
                        return string.Empty;
                    
                    return convertedValue.ToString("N", CultureInfo.CurrentCulture);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en DecimalToFormattedStringConverter.Convert: {ex.Message}");
                return string.Empty;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                if (value is string stringValue)
                {
                    // Si está vacío, devolver 0
                    if (string.IsNullOrWhiteSpace(stringValue))
                        return 0m;

                    // Limpiar el texto removiendo separadores de miles y espacios
                    string cleanText = stringValue
                        .Replace(",", "")
                        .Replace(" ", "")
                        .Replace(culture.NumberFormat.NumberGroupSeparator, "")
                        .Trim();

                    // Intentar parsear como decimal
                    if (decimal.TryParse(cleanText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal result))
                    {
                        return result;
                    }

                    // Si no se puede parsear con InvariantCulture, intentar con la cultura actual
                    if (decimal.TryParse(cleanText, NumberStyles.AllowDecimalPoint, culture, out decimal resultCulture))
                    {
                        return resultCulture;
                    }

                    // Si no se puede parsear, devolver 0
                    return 0m;
                }

                return 0m;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en DecimalToFormattedStringConverter.ConvertBack: {ex.Message}");
                return 0m;
            }
        }
    }
}