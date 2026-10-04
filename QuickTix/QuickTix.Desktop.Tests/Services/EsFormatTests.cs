using QuickTix.Contracts.Enums;
using QuickTix.Desktop.Services;

namespace QuickTix.Desktop.Tests.Services
{
    /// <summary>Formatos es-ES compartidos por la pantalla «Cierre de caja» y su CSV.</summary>
    public class EsFormatTests
    {
        [Theory]
        [InlineData(12.5, "12,50 €")]
        [InlineData(0, "0,00 €")]
        [InlineData(12345.5, "12.345,50 €")]
        public void Money_UsesSpanishSeparatorsAndEuroSign(double amount, string expected)
        {
            Assert.Equal(expected, EsFormat.Money((decimal)amount));
        }

        [Fact]
        public void DateTimeLocal_IsDayMonthYearAndTwentyFourHourClock()
        {
            var value = new DateTime(2026, 7, 15, 23, 30, 0, DateTimeKind.Unspecified);

            Assert.Equal("15/07/2026 23:30", EsFormat.DateTimeLocal(value));
        }

        [Fact]
        public void Date_IsDayMonthYear()
        {
            Assert.Equal("05/01/2026", EsFormat.Date(new DateOnly(2026, 1, 5)));
        }

        [Theory]
        [InlineData(PaymentMethod.Cash, "Efectivo")]
        [InlineData(PaymentMethod.Card, "Tarjeta")]
        [InlineData(PaymentMethod.Bizum, "Bizum")]
        public void PaymentMethodName_IsSpanish(PaymentMethod method, string expected)
        {
            Assert.Equal(expected, EsFormat.PaymentMethodName(method));
        }
    }
}
