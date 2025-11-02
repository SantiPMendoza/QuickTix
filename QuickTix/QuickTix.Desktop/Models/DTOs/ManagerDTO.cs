using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace QuickTix.Desktop.Models.DTOs
{
    public class ManagerDTO : CreateManagerDTO
    {
        public int Id { get; set; }
        public string VenueName { get; set; } = null!;

    }

    public class CreateManagerDTO
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public int VenueId { get; set; }
    }
}
