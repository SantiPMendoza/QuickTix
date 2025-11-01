using System.ComponentModel.DataAnnotations;

namespace QuickTix.Core.Models.DTOs
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
    }
}
