using QuickTix.Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuickTix.Contracts.DTOs.SaleDTOs.Ticket
{
    public class SellTicketsBatchDTO
    {
        [Required]
        public int VenueId { get; set; }

        [Required]
        public int ManagerId { get; set; }

        public int? ClientId { get; set; }

        [Required]
        [MinLength(1)]
        public List<SellTicketLineDTO> Lines { get; set; } = new();
    }

}
