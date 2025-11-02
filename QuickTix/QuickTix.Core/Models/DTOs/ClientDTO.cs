using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace QuickTix.Core.Models.DTOs
{
    public class ClientDTO : CreateClientDTO
    {
        public int Id { get; set; }
    }

    public class CreateClientDTO
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}

