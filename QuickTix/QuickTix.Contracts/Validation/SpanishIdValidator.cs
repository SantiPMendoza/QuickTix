namespace QuickTix.Contracts.Validation
{
    public static class SpanishIdValidator
    {
        private const string NifLetters = "TRWAGMYFPDXBNJZSQVHLCKE";

        public static bool IsValidNifOrNie(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            var v = value.Trim().ToUpperInvariant();

            // NIF
            if (RegexCatalog.Nif().IsMatch(v))
            {
                if (!int.TryParse(v[..8], out var number)) return false;
                var letter = v[8];
                return NifLetters[number % 23] == letter;
            }

            // NIE
            if (RegexCatalog.Nie().IsMatch(v))
            {
                var prefixDigit = v[0] switch
                {
                    'X' => '0',
                    'Y' => '1',
                    'Z' => '2',
                    _ => '?'
                };

                if (prefixDigit == '?') return false;

                var numberString = $"{prefixDigit}{v.Substring(1, 7)}";
                if (!int.TryParse(numberString, out var number)) return false;

                var letter = v[8];
                return NifLetters[number % 23] == letter;
            }

            return false;
        }
    }
}
