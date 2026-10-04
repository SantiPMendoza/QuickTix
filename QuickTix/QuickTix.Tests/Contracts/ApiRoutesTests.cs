using System.Globalization;
using QuickTix.Contracts.Routes;

namespace QuickTix.Tests.Contracts
{
    /// <summary>
    /// Las rutas con fechas deben formatearse siempre en Gregoriano (yyyy-MM-dd) con cultura
    /// invariante: el Desktop corre con la cultura del sistema del usuario y la API espera ISO.
    /// </summary>
    public class ApiRoutesTests
    {
        [Theory]
        [InlineData("th-TH")] // calendario budista (año 2569)
        [InlineData("ar-SA")] // calendario Umm al-Qura
        [InlineData("es-ES")]
        public void CashCloseByRange_UsesGregorianInvariantDates_UnderAnyCurrentCulture(string cultureName)
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultureName);

                var route = ApiRoutes.Reports.CashCloseByRange(new DateOnly(2026, 7, 15), new DateOnly(2026, 7, 31));

                Assert.Equal("/api/Reports/cash-close?from=2026-07-15&to=2026-07-31", route);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}
