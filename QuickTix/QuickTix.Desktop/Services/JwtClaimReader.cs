using System.Text.Json;

namespace QuickTix.Desktop.Services
{
    /// <summary>
    /// Utilidad para leer claims del payload de un JWT sin validar la firma.
    /// Implementación autocontenida (Base64Url + System.Text.Json) para no añadir
    /// dependencias de paquetes JWT al cliente Desktop (las refs a DAL/API están
    /// pendientes de eliminarse — ver ARCHITECTURE.md § Layer Contracts).
    /// Equivalente funcional al JwtClaimReader del cliente Mobile.
    /// </summary>
    public static class JwtClaimReader
    {
        /// <summary>
        /// Parsea el payload de un JWT y devuelve un diccionario de claims (Type -> Value).
        /// Si una claim aparece varias veces (p.ej. role), se conserva la primera.
        /// Devuelve un diccionario vacío si el token no es parseable.
        /// </summary>
        /// <param name="jwt">Token JWT emitido por la API.</param>
        /// <returns>Diccionario de claims con el primer valor por tipo.</returns>
        public static IReadOnlyDictionary<string, string> Read(string jwt)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2)
                    return result;

                var payloadJson = DecodeBase64Url(parts[1]);
                using var doc = JsonDocument.Parse(payloadJson);

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    // Claims multivalor (arrays) → se toma el primer elemento
                    var value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.Array => prop.Value.EnumerateArray().FirstOrDefault().ToString(),
                        _ => prop.Value.ToString()
                    };

                    if (!result.ContainsKey(prop.Name))
                        result[prop.Name] = value;
                }
            }
            catch
            {
                // Token malformado: se devuelve vacío y el llamador decide (managerId = 0)
                result.Clear();
            }

            return result;
        }

        /// <summary>
        /// Obtiene un entero desde un conjunto de posibles claves de claim.
        /// Si ninguna existe o no es parseable, devuelve 0.
        /// </summary>
        /// <param name="claims">Diccionario de claims.</param>
        /// <param name="keys">Claves candidatas a buscar.</param>
        /// <returns>Valor entero o 0 si no se encuentra.</returns>
        public static int GetInt(IReadOnlyDictionary<string, string> claims, params string[] keys)
        {
            foreach (var k in keys)
            {
                if (claims.TryGetValue(k, out var v) && int.TryParse(v, out var parsed))
                    return parsed;
            }

            return 0;
        }

        /// <summary>
        /// Decodifica una cadena Base64Url (alfabeto URL-safe y sin padding) a texto UTF-8.
        /// </summary>
        /// <param name="input">Segmento Base64Url del JWT.</param>
        /// <returns>Texto decodificado.</returns>
        private static string DecodeBase64Url(string input)
        {
            var base64 = input.Replace('-', '+').Replace('_', '/');

            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            var bytes = Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
