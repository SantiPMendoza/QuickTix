using System.ComponentModel.DataAnnotations;
using QuickTix.Contracts.Validation.Attributes;

namespace QuickTix.Contracts.DTOs.SaleDTOs
{
    /// <summary>
    /// Petición para anular (lógicamente) una venta. El motivo es obligatorio.
    /// </summary>
    public class VoidSaleDTO
    {
        /// <summary>Longitud máxima del motivo (coincide con la columna VoidReason).</summary>
        public const int ReasonMaxLength = 200;

        [Required(ErrorMessage = "El motivo de la anulación es obligatorio.")]
        // Se mide el texto recortado, igual que el repositorio (que hace Trim antes de comprobar).
        [TrimmedMaxLength(ReasonMaxLength, ErrorMessage = "El motivo no puede superar los 200 caracteres.")]
        public string Reason { get; set; } = string.Empty;
    }
}
