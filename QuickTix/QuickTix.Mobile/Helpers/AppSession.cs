using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Mobile.Helpers
{
    public interface IAppSession
    {
        int VenueId { get; }
        int ManagerId { get; }
        int ClientId { get; }
        string? UserId { get; }
        string? Email { get; }
        string? Role { get; }

        void Clear();
        void LoadFromToken(string jwt);
    }

    public sealed class AppSession : IAppSession
    {
        public int VenueId { get; private set; }
        public int ManagerId { get; private set; }
        public int ClientId { get; private set; }
        public string? UserId { get; private set; }
        public string? Email { get; private set; }
        public string? Role { get; private set; }

        public void LoadFromToken(string jwt)
        {
            var claims = JwtClaimReader.Read(jwt);

            // Nombres recomendados (ajusta si tus claims se llaman distinto)
            VenueId = JwtClaimReader.GetInt(claims, "venueId", "VenueId");
            ManagerId = JwtClaimReader.GetInt(claims, "managerId", "ManagerId");
            ClientId = JwtClaimReader.GetInt(claims, "clientId", "ClientId");

            // Estándar típico
            UserId = JwtClaimReader.GetString(claims,
                "sub",
                "nameid",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

            Email = JwtClaimReader.GetString(claims,
                "email",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

            Role = JwtClaimReader.GetString(claims,
                "role",
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
        }

        public void Clear()
        {
            VenueId = 0;
            ManagerId = 0;
            ClientId = 0;
            UserId = null;
            Email = null;
            Role = null;
        }
    }
}
