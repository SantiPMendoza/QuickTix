using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Contracts.Routes
{
    public static class ApiRoutes
    {
        public static class User
        {
            public const string Login = "/api/User/login";
            public const string ChangePassword = "/api/User/change-password";
        }
    }
}
