using QuickTix.Contracts.Enums;
using QuickTix.Core.Models.Entities.Price;

namespace QuickTix.Core.Services
{
    public static class PriceMapValidator
    {
        public static List<string> ValidateCompleteness(
            IEnumerable<VenueTicketPrice> ticketPrices,
            IEnumerable<VenueSubscriptionPrice> subscriptionPrices)
        {
            var errors = new List<string>();

            var ticketSet = new HashSet<(TicketType, TicketContext)>(
                ticketPrices.Select(x => (x.Type, x.Context)));

            foreach (var type in Enum.GetValues<TicketType>())
                foreach (var ctx in Enum.GetValues<TicketContext>())
                    if (!ticketSet.Contains((type, ctx)))
                        errors.Add($"Falta precio Ticket: {type}/{ctx}.");

            var subSet = new HashSet<(SubscriptionCategory, SubscriptionDuration)>(
                subscriptionPrices.Select(x => (x.Category, x.Duration)));

            foreach (var cat in Enum.GetValues<SubscriptionCategory>())
                foreach (var dur in Enum.GetValues<SubscriptionDuration>())
                    if (!subSet.Contains((cat, dur)))
                        errors.Add($"Falta precio Abono: {cat}/{dur}.");

            return errors;
        }

        public static void ValidateNonNegativePrices(
            IEnumerable<VenueTicketPrice> ticketPrices,
            IEnumerable<VenueSubscriptionPrice> subscriptionPrices)
        {
            if (ticketPrices.Any(x => x.Price < 0) || subscriptionPrices.Any(x => x.Price < 0))
                throw new ArgumentOutOfRangeException(nameof(ticketPrices), "El precio no puede ser negativo.");
        }
    }
}
