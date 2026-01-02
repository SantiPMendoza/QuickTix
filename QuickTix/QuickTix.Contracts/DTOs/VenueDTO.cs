using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace QuickTix.Contracts.Models.DTOs
{
    public class VenueDTO : CreateVenueDTO
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateVenueDTO
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string Location { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }
    }
}


