using System.Globalization;

namespace sys21_campos_zukarmex.Behaviors
{
    /// <summary>
    /// Behavior que aplica formato de separadores de miles a un Entry numérico
    /// Permite entrada de números enteros y decimales con formato visual de miles
    /// </summary>
    public class ThousandsSeparatorBehavior : Behavior<Entry>
    {
        private bool _isUpdating = false;
        private Entry? _entry;

        protected override void OnAttachedTo(Entry bindable)
        {
            base.OnAttachedTo(bindable);
            _entry = bindable;
            bindable.TextChanged += OnTextChanged;
            bindable.Focused += OnFocused;
            bindable.Unfocused += OnUnfocused;
        }

        protected override void OnDetachingFrom(Entry bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.TextChanged -= OnTextChanged;
            bindable.Focused -= OnFocused;
            bindable.Unfocused -= OnUnfocused;
            _entry = null;
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_isUpdating || _entry == null)
                return;

            _isUpdating = true;

            try
            {
                string newText = e.NewTextValue ?? string.Empty;
                
                // Si el texto está vacío, permitirlo
                if (string.IsNullOrEmpty(newText))
                {
                    return;
                }

                // Remover todos los caracteres que no sean dígitos, puntos o comas
                string cleanText = new string(newText.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                
                // Si no hay texto limpio, limpiar el campo
                if (string.IsNullOrEmpty(cleanText))
                {
                    _entry.Text = string.Empty;
                    return;
                }

                // Intentar parsear el número
                if (decimal.TryParse(cleanText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number))
                {
                    // Formatear con separadores de miles
                    string formattedText = number.ToString("N", CultureInfo.CurrentCulture);
                    
                    // Solo actualizar si el texto ha cambiado
                    if (_entry.Text != formattedText)
                    {
                        int cursorPosition = _entry.CursorPosition;
                        int lengthDifference = formattedText.Length - newText.Length;
                        
                        _entry.Text = formattedText;
                        
                        // Ajustar la posición del cursor
                        int newCursorPosition = Math.Max(0, Math.Min(formattedText.Length, cursorPosition + lengthDifference));
                        _entry.CursorPosition = newCursorPosition;
                    }
                }
                else
                {
                    // Si no se puede parsear, restaurar el texto anterior
                    _entry.Text = e.OldTextValue ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ThousandsSeparatorBehavior: {ex.Message}");
                // En caso de error, restaurar el texto anterior
                _entry.Text = e.OldTextValue ?? string.Empty;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void OnFocused(object? sender, FocusEventArgs e)
        {
            if (_entry == null)
                return;

            try
            {
                // Al enfocar, convertir el formato visual al número sin formato para facilitar la edición
                string currentText = _entry.Text ?? string.Empty;
                
                if (!string.IsNullOrEmpty(currentText))
                {
                    // Remover separadores de miles y mantener solo el número
                    string cleanText = currentText.Replace(",", "").Replace(" ", "");
                    
                    if (decimal.TryParse(cleanText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number))
                    {
                        _isUpdating = true;
                        _entry.Text = number.ToString(CultureInfo.InvariantCulture);
                        _isUpdating = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnFocused: {ex.Message}");
            }
        }

        private void OnUnfocused(object? sender, FocusEventArgs e)
        {
            if (_entry == null)
                return;

            try
            {
                // Al perder el foco, aplicar el formato con separadores de miles
                string currentText = _entry.Text ?? string.Empty;
                
                if (!string.IsNullOrEmpty(currentText))
                {
                    if (decimal.TryParse(currentText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number))
                    {
                        _isUpdating = true;
                        _entry.Text = number.ToString("N", CultureInfo.CurrentCulture);
                        _isUpdating = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnUnfocused: {ex.Message}");
            }
        }
    }
}