using System.IdentityModel.Tokens.Jwt;

namespace QuickTix.Mobile.Helpers
{
    /// <summary>
    /// Utilidad para leer claims de un token JWT sin validar la firma.
    /// Se usa para extraer valores de sesión del token ya emitido por la API.
    /// </summary>
    public static class JwtClaimReader
    {
        /// <summary>
        /// Parsea un JWT y devuelve un diccionario de claims (Type -&gt; Value).
        /// </summary>
        /// <param name="jwt">Token JWT.</param>
        /// <returns>Diccionario de claims con el primer valor por tipo.</returns>
        public static IReadOnlyDictionary<string, string> Read(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            // Si hay claims duplicadas (p.ej. role), se conserva la primera.
            return token.Claims
                .GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => g.First().Value);
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
        /// Obtiene un string desde un conjunto de posibles claves de claim.
        /// Si ninguna existe o el valor es vacío, devuelve null.
        /// </summary>
        /// <param name="claims">Diccionario de claims.</param>
        /// <param name="keys">Claves candidatas a buscar.</param>
        /// <returns>Valor encontrado o null.</returns>
        public static string? GetString(IReadOnlyDictionary<string, string> claims, params string[] keys)
        {
            foreach (var k in keys)
            {
                if (claims.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    return v;
            }

            return null;
        }
    }
}
