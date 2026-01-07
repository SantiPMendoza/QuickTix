using QuickTix.Contracts.Validation.Attributes;
using System.ComponentModel.DataAnnotations;

namespace QuickTix.Contracts.Models.DTOs
{
    public class AdminDTO : CreateAdminDTO
    {
        public int Id { get; set; }
    }

    public class CreateAdminDTO
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [PhoneNumber]
        public string? PhoneNumber { get; set; } = null;

        [NifNie]
        public string? Nif { get; set; } = null;
    }
}
