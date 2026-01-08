using System;

namespace QuickTix.Mobile.Helpers
{
    public interface IAppSession
    {
        int VenueId { get; }
        string VenueName { get; }
        int ManagerId { get; }
        int ClientId { get; }
        string? UserId { get; }
        string? Email { get; }
        string? Role { get; }
        string? Name { get; }

        void Clear();
        void LoadFromToken(string jwt);
    }

    public sealed class AppSession : IAppSession
    {
        public int VenueId { get; private set; }
        public string VenueName { get; private set; } = string.Empty;
        public int ManagerId { get; private set; }
        public int ClientId { get; private set; }
        public string? UserId { get; private set; }
        public string? Email { get; private set; }
        public string? Role { get; private set; }
        public string? Name { get; private set; } // Nuevo

        public void LoadFromToken(string jwt)
        {
            var claims = JwtClaimReader.Read(jwt);

            VenueId = JwtClaimReader.GetInt(claims, "venueId", "VenueId");
            ManagerId = JwtClaimReader.GetInt(claims, "managerId", "ManagerId");
            ClientId = JwtClaimReader.GetInt(claims, "clientId", "ClientId");

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

            Name = JwtClaimReader.GetString(claims,
                "name",
                "unique_name",
                "given_name",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        }

        public void Clear()
        {
            VenueId = 0;
            ManagerId = 0;
            ClientId = 0;
            UserId = null;
            Email = null;
            Role = null;
            Name = null;
        }
    }
}
