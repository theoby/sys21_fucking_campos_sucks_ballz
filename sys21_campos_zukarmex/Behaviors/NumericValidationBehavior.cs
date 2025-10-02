using System.Linq;

namespace sys21_campos_zukarmex.Behaviors
{
    public class NumericValidationBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            base.OnAttachedTo(entry);
            entry.TextChanged += OnEntryTextChanged;
        }
        protected override void OnDetachingFrom(Entry entry)
        {
            base.OnDetachingFrom(entry);
            entry.TextChanged -= OnEntryTextChanged;
        }
        private void OnEntryTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entry)
                return;

            if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                entry.Text = string.Empty;
                return;
            }

            string textoFiltrado = new string(e.NewTextValue.Where(char.IsDigit).ToArray());

            if (entry.Text != textoFiltrado)
            {
                entry.Text = textoFiltrado;
            }
        }
    }
}