using System.ComponentModel.DataAnnotations;

namespace QuickTix.Desktop.Models.DTOs
{
    public class AdminDTO : CreateAdminDTO
    {
        public int Id { get; set; }
    }

    public class CreateAdminDTO
    {
        [Required]
        public string Name { get; set; } = null!;

        public string? Nif { get; set; }

        public string? PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}
