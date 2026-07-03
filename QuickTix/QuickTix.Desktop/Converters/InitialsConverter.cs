using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace QuickTix.Desktop.Converters
{
    /// <summary>
    /// Convierte un nombre completo en sus iniciales (máx. 2) para los
    /// avatares del rediseño Vibra. Solo presentación: no toca el dato.
    /// </summary>
    public class InitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string name || string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var initials = string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
            return initials.Length > 0 ? initials : "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
