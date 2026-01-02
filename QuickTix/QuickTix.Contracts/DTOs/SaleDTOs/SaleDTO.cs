using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuickTix.Contracts.Models.DTOs.SaleDTOs
{
    // ============================
    // 🔹 SaleDTO principal
    // ============================
    public class SaleDTO
    {
        public int Id { get; set; }

        [Required]
        public int VenueId { get; set; }

        [Required]
        public int ManagerId { get; set; }

        public DateTime Date { get; set; }

        // 🔹 Detalle de ítems vendidos (tickets, suscripciones, etc.)
        public ICollection<SaleItemDTO> Items { get; set; } = new List<SaleItemDTO>();

        public decimal TotalAmount => Items.Sum(i => i.Subtotal);
    }

    // ============================
    // 🔹 DTO para crear una venta
    // ============================
    public class CreateSaleDTO
    {
        [Required]
        public int VenueId { get; set; }

        [Required]
        public int ManagerId { get; set; }

        // 🔹 Detalle requerido para crear la venta
        [MinLength(1, ErrorMessage = "Debe incluir al menos un ítem de venta.")]
        public ICollection<CreateSaleItemDTO> Items { get; set; } = new List<CreateSaleItemDTO>();
    }
}