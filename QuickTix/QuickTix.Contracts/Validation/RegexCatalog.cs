using System.Text.RegularExpressions;

namespace QuickTix.Contracts.Validation
{
    public static partial class RegexCatalog
    {
        // NIF: 8 dígitos + letra (sin I, O, U)
        [GeneratedRegex(@"^\d{8}[A-HJ-NP-TV-Z]$", RegexOptions.CultureInvariant)]
        public static partial Regex Nif();

        // NIE: X/Y/Z + 7 dígitos + letra (sin I, O, U)
        [GeneratedRegex(@"^[XYZ]\d{7}[A-HJ-NP-TV-Z]$", RegexOptions.CultureInvariant)]
        public static partial Regex Nie();

        // Teléfono: exactamente 9 dígitos
        [GeneratedRegex(@"^\d{9}$", RegexOptions.CultureInvariant)]
        public static partial Regex Phone9Digits();
    }
}
