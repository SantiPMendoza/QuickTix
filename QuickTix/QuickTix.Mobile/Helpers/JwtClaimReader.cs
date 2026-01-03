using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;

namespace QuickTix.Mobile.Helpers
{
    public static class JwtClaimReader
    {
        public static IReadOnlyDictionary<string, string> Read(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            // Si hay claims duplicadas (role), aquí nos quedamos con la primera. Para role ya capturamos string simple.
            return token.Claims
                .GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => g.First().Value);
        }

        public static int GetInt(IReadOnlyDictionary<string, string> claims, params string[] keys)
        {
            foreach (var k in keys)
            {
                if (claims.TryGetValue(k, out var v) && int.TryParse(v, out var parsed))
                    return parsed;
            }
            return 0;
        }

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