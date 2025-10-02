using System.Globalization;

namespace sys21_campos_zukarmex.Behaviors
{
    /// <summary>
    /// Behavior que aplica formato de separadores de miles a un Entry numérico
    /// SOLO PERMITE NÚMEROS ENTEROS (sin decimales)
    /// </summary>
    public class IntegerThousandsSeparatorBehavior : Behavior<Entry>
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

                // Intentar parsear como número entero
                if (long.TryParse(cleanText, out long number))
                {
                    // Formatear con separadores de miles (formato sin decimales)
                    string formattedText = number.ToString("N0", CultureInfo.CurrentCulture);
                    
                    // Solo actualizar si el texto ha cambiado
                    if (_entry.Text != formattedText)
                    {
                        // Store cursor position info BEFORE changing text
                        int oldCursorPosition = _entry.CursorPosition;
                        string oldText = e.OldTextValue ?? string.Empty;
                        
                        // Update text first
                        _entry.Text = formattedText;
                        
                        // COMPLETELY AVOID cursor positioning on Android to prevent exception
                        // Use a delayed approach that's safer for cross-platform compatibility
                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                // Wait a minimal time for UI to update
                                await Task.Delay(1);
                                
                                if (_entry != null && !_isUpdating)
                                {
                                    // Calculate safer cursor position
                                    int newCursorPosition = CalculateSafeCursorPosition(oldText, formattedText, oldCursorPosition);
                                    
                                    // Multiple safety checks for Android
                                    if (newCursorPosition >= 0 && 
                                        newCursorPosition < formattedText.Length && // STRICTLY less than, not equal
                                        _entry.Text == formattedText) // Ensure text hasn't changed in the meantime
                                    {
                                        _entry.CursorPosition = newCursorPosition;
                                    }
                                    else
                                    {
                                        // Fallback: position at end but safely
                                        int safeEndPosition = Math.Max(0, formattedText.Length - 1);
                                        if (safeEndPosition >= 0 && safeEndPosition < formattedText.Length)
                                        {
                                            _entry.CursorPosition = safeEndPosition;
                                        }
                                        // If even that fails, don't set cursor position at all
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Silently handle any cursor positioning errors
                                System.Diagnostics.Debug.WriteLine($"Error en cursor positioning: {ex.Message}");
                            }
                        });
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
                System.Diagnostics.Debug.WriteLine($"Error en IntegerThousandsSeparatorBehavior: {ex.Message}");
                // En caso de error, restaurar el texto anterior
                _entry.Text = e.OldTextValue ?? string.Empty;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private int CalculateSafeCursorPosition(string oldText, string newText, int oldPosition)
        {
            try
            {
                // Count digits before cursor in old text
                int digitsBeforeCursor = 0;
                for (int i = 0; i < Math.Min(oldPosition, oldText.Length); i++)
                {
                    if (char.IsDigit(oldText[i]))
                        digitsBeforeCursor++;
                }
                
                // Find corresponding position in new text
                int digitCount = 0;
                for (int i = 0; i < newText.Length; i++)
                {
                    if (char.IsDigit(newText[i]))
                    {
                        digitCount++;
                        if (digitCount >= digitsBeforeCursor)
                        {
                            // Return position AFTER this digit, but ensure it's within bounds
                            int position = Math.Min(i + 1, newText.Length - 1);
                            return Math.Max(0, position);
                        }
                    }
                }
                
                // Fallback: return safe end position
                return Math.Max(0, newText.Length - 1);
            }
            catch
            {
                // If anything goes wrong, return safe position
                return Math.Max(0, Math.Min(newText.Length - 1, 0));
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
                    
                    if (long.TryParse(cleanText, out long number))
                    {
                        _isUpdating = true;
                        _entry.Text = number.ToString();
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