using QuickTix.Contracts.Enums;

namespace QuickTix.Contracts.DTOs.ReportDTOs
{
    /// <summary>
    /// Informe de cierre de caja (arqueo) de un rango de días locales (Europe/Madrid).
    /// Las ventas anuladas quedan fuera de TODOS los totales y agrupaciones: solo aparecen
    /// en <see cref="Lines"/> (marcadas) y en <see cref="VoidedTotal"/>/<see cref="VoidedCount"/>.
    /// </summary>
    public class CashCloseReportDTO
    {
        /// <summary>Primer día local incluido.</summary>
        public DateOnly From { get; set; }

        /// <summary>Último día local incluido.</summary>
        public DateOnly To { get; set; }

        /// <summary>Total cobrado (ventas no anuladas).</summary>
        public decimal Total { get; set; }

        /// <summary>Número de ventas no anuladas.</summary>
        public int SaleCount { get; set; }

        /// <summary>Importe de las ventas anuladas del rango (no suma en <see cref="Total"/>).</summary>
        public decimal VoidedTotal { get; set; }

        /// <summary>Número de ventas anuladas del rango.</summary>
        public int VoidedCount { get; set; }

        public List<CashCloseVenueDTO> ByVenue { get; set; } = new();
        public List<CashCloseConceptDTO> ByConcept { get; set; } = new();
        public List<CashClosePaymentMethodDTO> ByPaymentMethod { get; set; } = new();

        /// <summary>Todas las líneas del rango (anuladas incluidas, marcadas), ordenadas por fecha.</summary>
        public List<CashCloseLineDTO> Lines { get; set; } = new();
    }

    /// <summary>Total de un recinto, desglosado por vendedor.</summary>
    public class CashCloseVenueDTO
    {
        public string VenueName { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public List<CashCloseSellerDTO> BySeller { get; set; } = new();
    }

    /// <summary>Total de un vendedor (gestor, o «Administración» si la venta no tiene gestor).</summary>
    public class CashCloseSellerDTO
    {
        public string SellerName { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public int SaleCount { get; set; }
    }

    /// <summary>Total de un concepto (tipo de entrada o categoría/duración de abono).</summary>
    public class CashCloseConceptDTO
    {
        public string Concept { get; set; } = string.Empty;
        public bool IsSubscription { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>Total por medio de pago.</summary>
    public class CashClosePaymentMethodDTO
    {
        public PaymentMethod PaymentMethod { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>Línea de venta del arqueo (una por venta y concepto/precio unitario).</summary>
    public class CashCloseLineDTO
    {
        public int SaleId { get; set; }

        /// <summary>Fecha y hora local de Madrid (sin zona).</summary>
        public DateTime LocalDateTime { get; set; }

        public string VenueName { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public string Concept { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public bool IsVoided { get; set; }
        public string? VoidReason { get; set; }
    }
}
