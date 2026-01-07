using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using QuickTix.Contracts.Validation.Attributes;

namespace QuickTix.Contracts.Models.DTOs
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

        [PhoneNumber]
        public string? PhoneNumber { get; set; } = null;

        [NifNie]
        public string? Nif { get; set; } = null;
    }
}

