using System.Globalization;

namespace sys21_campos_zukarmex.Converters
{
    /// <summary>
    /// Converter que maneja la conversión entre decimal y string con formato de separadores de miles
    /// SOLO PARA NÚMEROS ENTEROS (sin decimales)
    /// </summary>
    public class IntegerToFormattedStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                if (value is decimal decimalValue)
                {
                    // Convertir a entero (truncar decimales)
                    long integerValue = (long)Math.Truncate(decimalValue);
                    
                    // Si el valor es 0, mostrar texto vacío para mejor UX
                    if (integerValue == 0)
                        return string.Empty;
                    
                    // Formatear con separadores de miles SIN decimales (N0)
                    return integerValue.ToString("N0", CultureInfo.CurrentCulture);
                }
                
                if (value is double doubleValue)
                {
                    long integerValue = (long)Math.Truncate(doubleValue);
                    if (integerValue == 0)
                        return string.Empty;
                    
                    return integerValue.ToString("N0", CultureInfo.CurrentCulture);
                }
                
                if (value is float floatValue)
                {
                    long integerValue = (long)Math.Truncate(floatValue);
                    if (integerValue == 0)
                        return string.Empty;
                    
                    return integerValue.ToString("N0", CultureInfo.CurrentCulture);
                }

                if (value is int intValue)
                {
                    if (intValue == 0)
                        return string.Empty;
                    
                    return intValue.ToString("N0", CultureInfo.CurrentCulture);
                }

                if (value is long longValue)
                {
                    if (longValue == 0)
                        return string.Empty;
                    
                    return longValue.ToString("N0", CultureInfo.CurrentCulture);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en IntegerToFormattedStringConverter.Convert: {ex.Message}");
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

                    // Limpiar el texto removiendo separadores de miles, espacios y caracteres no numéricos
                    string cleanText = new string(stringValue.Where(char.IsDigit).ToArray());

                    // Si no hay dígitos, devolver 0
                    if (string.IsNullOrEmpty(cleanText))
                        return 0m;

                    // Intentar parsear como entero largo
                    if (long.TryParse(cleanText, out long result))
                    {
                        // Convertir a decimal para mantener compatibilidad con el ViewModel
                        return (decimal)result;
                    }

                    // Si no se puede parsear, devolver 0
                    return 0m;
                }

                return 0m;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en IntegerToFormattedStringConverter.ConvertBack: {ex.Message}");
                return 0m;
            }
        }
    }
}