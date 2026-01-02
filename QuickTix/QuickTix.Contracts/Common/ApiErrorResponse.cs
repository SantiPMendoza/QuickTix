using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Contracts.Common
{
    public sealed class ApiErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
