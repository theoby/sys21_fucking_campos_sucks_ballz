using System.Globalization;

namespace sys21_campos_zukarmex.Behaviors
{
    /// <summary>
    /// Simplified behavior que aplica formato de separadores de miles SOLO al perder el foco
    /// SOLO PERMITE NÚMEROS ENTEROS (sin decimales)
    /// Esta versión evita completamente los problemas de cursor positioning en Android
    /// </summary>
    public class IntegerThousandsSeparatorBehaviorSimple : Behavior<Entry>
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

                // SOLO PERMITIR DÍGITOS - NO puntos ni comas para decimales
                string cleanText = new string(newText.Where(char.IsDigit).ToArray());
                
                // Si no hay texto limpio, limpiar el campo
                if (string.IsNullOrEmpty(cleanText))
                {
                    _entry.Text = string.Empty;
                    return;
                }

                // SIMPLE APPROACH: Only filter invalid characters, don't format until unfocus
                // This completely avoids cursor positioning issues
                if (_entry.Text != cleanText)
                {
                    _entry.Text = cleanText;
                    // Don't mess with cursor position - let it stay where the user expects
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en IntegerThousandsSeparatorBehaviorSimple: {ex.Message}");
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
                    // Remover separadores de miles y mantener solo los dígitos
                    string cleanText = new string(currentText.Where(char.IsDigit).ToArray());
                    
                    if (!string.IsNullOrEmpty(cleanText))
                    {
                        _isUpdating = true;
                        _entry.Text = cleanText;
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
                    // Solo dígitos para números enteros
                    string cleanText = new string(currentText.Where(char.IsDigit).ToArray());
                    
                    if (long.TryParse(cleanText, out long number))
                    {
                        _isUpdating = true;
                        // N0 = formato numérico sin decimales
                        _entry.Text = number.ToString("N0", CultureInfo.CurrentCulture);
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