using Microsoft.EntityFrameworkCore;
using QuickTix.Contracts.DTOs.ReportDTOs;
using QuickTix.Contracts.Enums;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Time;
using QuickTix.DAL.Data;
using System.Text;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de solo lectura del cierre de caja (arqueo).
    ///
    /// NO cachea: un arqueo debe reflejar exactamente lo que hay en base de datos en ese
    /// momento (una venta o una anulación de hace un segundo tiene que contar).
    ///
    /// El rango son días LOCALES de Madrid, inclusivos, convertidos a un intervalo UTC
    /// [inicio, fin) con <see cref="LocalBusinessDay"/> (misma definición de "día" que el Panel).
    /// Los importes (decimal) se suman EN MEMORIA: el provider de SQLite de los tests no
    /// traduce agregados decimales, y convertir a double falsearía el dinero.
    /// </summary>
    public class CashCloseRepository : ICashCloseRepository
    {
        // Tope del rango (inclusive) para que un arqueo no traiga media base de datos
        private const int MaxRangeDays = 366;

        // Vendedor de las ventas sin gestor (registradas por un admin)
        private const string AdministrationSeller = "Administración";

        private readonly ApplicationDbContext _context;

        public CashCloseRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<CashCloseReportDTO> GetReportAsync(DateOnly from, DateOnly to)
        {
            if (from > to)
                throw new ArgumentException("La fecha inicial no puede ser posterior a la final.");

            if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
                throw new ArgumentException($"El rango no puede superar los {MaxRangeDays} días.");

            var (startUtc, endUtcExclusive) = LocalBusinessDay.ToUtcRange(from, to);

            // Proyección compacta por línea de venta, partiendo de SaleItems para que EF
            // genere JOINs simples (sin APPLY, que SQLite no soporta).
            var rows = await _context.SaleItems
                .AsNoTracking()
                .Where(i => i.Sale.Date >= startUtc && i.Sale.Date < endUtcExclusive)
                .Select(i => new
                {
                    i.SaleId,
                    i.Sale.Date,
                    i.Sale.VenueId,
                    VenueName = i.Sale.Venue.Name,
                    i.Sale.ManagerId,
                    ManagerName = i.Sale.Manager != null ? i.Sale.Manager.Name : null,
                    i.Sale.PaymentMethod,
                    i.Sale.VoidedAt,
                    i.Sale.VoidReason,
                    TicketType = i.Ticket != null ? (TicketType?)i.Ticket.Type : null,
                    TicketContext = i.Ticket != null ? (TicketContext?)i.Ticket.Context : null,
                    SubscriptionCategory = i.Subscription != null ? (SubscriptionCategory?)i.Subscription.Category : null,
                    SubscriptionDuration = i.Subscription != null ? (SubscriptionDuration?)i.Subscription.Duration : null,
                    i.Quantity,
                    i.UnitPrice
                })
                .ToListAsync();

            var lines = rows
                .Select(r => new
                {
                    r.SaleId,
                    r.Date,
                    r.VenueId,
                    r.VenueName,
                    r.ManagerId,
                    SellerName = r.ManagerName ?? AdministrationSeller,
                    r.PaymentMethod,
                    IsVoided = r.VoidedAt != null,
                    r.VoidReason,
                    IsSubscription = r.SubscriptionCategory != null,
                    Concept = BuildConcept(r.TicketType, r.TicketContext, r.SubscriptionCategory, r.SubscriptionDuration),
                    r.Quantity,
                    r.UnitPrice,
                    Subtotal = r.UnitPrice * r.Quantity
                })
                .ToList();

            var valid = lines.Where(l => !l.IsVoided).ToList();
            var voided = lines.Where(l => l.IsVoided).ToList();

            var byVenue = valid
                .GroupBy(l => new { l.VenueId, l.VenueName })
                .OrderBy(g => g.Key.VenueName)
                .Select(g => new CashCloseVenueDTO
                {
                    VenueName = g.Key.VenueName,
                    Total = g.Sum(l => l.Subtotal),
                    BySeller = g
                        .GroupBy(l => new { l.ManagerId, l.SellerName })
                        .OrderBy(s => s.Key.SellerName)
                        .Select(s => new CashCloseSellerDTO
                        {
                            SellerName = s.Key.SellerName,
                            Total = s.Sum(l => l.Subtotal),
                            SaleCount = s.Select(l => l.SaleId).Distinct().Count()
                        })
                        .ToList()
                })
                .ToList();

            var byConcept = valid
                .GroupBy(l => new { l.Concept, l.IsSubscription })
                .OrderBy(g => g.Key.IsSubscription)
                .ThenBy(g => g.Key.Concept)
                .Select(g => new CashCloseConceptDTO
                {
                    Concept = g.Key.Concept,
                    IsSubscription = g.Key.IsSubscription,
                    Quantity = g.Sum(l => l.Quantity),
                    Total = g.Sum(l => l.Subtotal)
                })
                .ToList();

            var byPaymentMethod = valid
                .GroupBy(l => l.PaymentMethod)
                .OrderBy(g => g.Key)
                .Select(g => new CashClosePaymentMethodDTO
                {
                    PaymentMethod = g.Key,
                    Total = g.Sum(l => l.Subtotal)
                })
                .ToList();

            // Una línea del informe por venta + concepto + precio unitario: una venta de 20
            // entradas iguales son 20 SaleItems de cantidad 1 y no tiene sentido listarlas sueltas.
            var reportLines = lines
                .GroupBy(l => new { l.SaleId, l.Concept, l.UnitPrice })
                .Select(g =>
                {
                    var first = g.First();
                    return new CashCloseLineDTO
                    {
                        SaleId = g.Key.SaleId,
                        LocalDateTime = LocalBusinessDay.ToLocalDateTime(first.Date),
                        VenueName = first.VenueName,
                        SellerName = first.SellerName,
                        Concept = g.Key.Concept,
                        Quantity = g.Sum(l => l.Quantity),
                        UnitPrice = g.Key.UnitPrice,
                        Subtotal = g.Sum(l => l.Subtotal),
                        PaymentMethod = first.PaymentMethod,
                        IsVoided = first.IsVoided,
                        VoidReason = first.VoidReason
                    };
                })
                .OrderBy(l => l.LocalDateTime)
                .ThenBy(l => l.SaleId)
                .ThenBy(l => l.Concept)
                .ToList();

            return new CashCloseReportDTO
            {
                From = from,
                To = to,
                Total = valid.Sum(l => l.Subtotal),
                SaleCount = valid.Select(l => l.SaleId).Distinct().Count(),
                VoidedTotal = voided.Sum(l => l.Subtotal),
                VoidedCount = voided.Select(l => l.SaleId).Distinct().Count(),
                ByVenue = byVenue,
                ByConcept = byConcept,
                ByPaymentMethod = byPaymentMethod,
                Lines = reportLines
            };
        }

        // Texto legible del concepto: "Entrada Niño laboral", "Abono Familia numerosa temporada".
        private static string BuildConcept(
            TicketType? ticketType, TicketContext? ticketContext,
            SubscriptionCategory? category, SubscriptionDuration? duration)
        {
            if (category != null)
                return $"Abono {Humanize(category.Value.ToString())} {Humanize(duration?.ToString() ?? string.Empty).ToLowerInvariant()}".TrimEnd();

            var concept = $"Entrada {Humanize(ticketType?.ToString() ?? string.Empty)}".TrimEnd();
            return ticketContext == TicketContext.InvitadoAbonado
                ? $"{concept} (invitado de abonado)"
                : concept;
        }

        // "NiñoLaboral" -> "Niño laboral": separa por mayúsculas y deja solo la primera en mayúscula.
        private static string Humanize(string pascalCase)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < pascalCase.Length; i++)
            {
                var c = pascalCase[i];
                if (i > 0 && char.IsUpper(c))
                    sb.Append(' ').Append(char.ToLowerInvariant(c));
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
