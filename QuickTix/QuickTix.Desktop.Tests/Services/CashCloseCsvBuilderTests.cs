using System.Globalization;
using System.IO;
using QuickTix.Contracts.DTOs.ReportDTOs;
using QuickTix.Contracts.Enums;
using QuickTix.Desktop.Services;

namespace QuickTix.Desktop.Tests.Services
{
    /// <summary>
    /// El CSV del arqueo (E3): formato para Excel ES, importes que cuadran con el total y
    /// protección frente a inyección de fórmulas en los campos de texto.
    /// </summary>
    public class CashCloseCsvBuilderTests
    {
        private static readonly CultureInfo Es = new("es-ES");

        private static CashCloseLineDTO Line(
            decimal unitPrice, int quantity, bool voided = false, string? reason = null,
            string concept = "Adulto laboral", string seller = "Gestor Piscina") => new()
        {
            SaleId = 1,
            LocalDateTime = new DateTime(2026, 7, 15, 23, 30, 0, DateTimeKind.Unspecified),
            VenueName = "Piscina Nalda",
            SellerName = seller,
            Concept = concept,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Subtotal = unitPrice * quantity,
            PaymentMethod = PaymentMethod.Cash,
            IsVoided = voided,
            VoidReason = reason
        };

        private static CashCloseReportDTO Report(params CashCloseLineDTO[] lines) => new()
        {
            From = new DateOnly(2026, 7, 15),
            To = new DateOnly(2026, 7, 15),
            Total = lines.Where(l => !l.IsVoided).Sum(l => l.Subtotal),
            SaleCount = lines.Count(l => !l.IsVoided),
            Lines = lines.ToList()
        };

        private static string[] Render(CashCloseReportDTO report)
        {
            using var writer = new StringWriter();
            CashCloseCsvBuilder.Write(writer, report);
            return writer.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        }

        [Fact]
        public void WriteToFile_StartsWithUtf8Bom()
        {
            var path = Path.Combine(Path.GetTempPath(), $"quicktix-arqueo-{Guid.NewGuid():N}.csv");
            try
            {
                CashCloseCsvBuilder.WriteToFile(path, Report(Line(4m, 1)));

                var bytes = File.ReadAllBytes(path);
                Assert.True(bytes.Length > 3);
                Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Write_UsesSemicolonDelimiterAndSpanishHeader()
        {
            var lines = Render(Report(Line(4m, 1)));

            Assert.Equal(
                "Fecha;Recinto;Vendedor;Concepto;Cantidad;PrecioUnitario;Subtotal;MedioPago;Anulada;MotivoAnulacion",
                lines[0]);
            Assert.Equal(10, lines[1].Split(';').Length);
        }

        [Fact]
        public void Write_FormatsAmountsWithSpanishDecimalComma()
        {
            var row = Render(Report(Line(12.5m, 1)))[1];

            Assert.Contains(";12,50;12,50;", row);
            Assert.DoesNotContain("12.50", row);
            Assert.StartsWith("15/07/2026 23:30;", row);
            Assert.Contains(";Efectivo;", row);
        }

        [Fact]
        public void Write_VoidedRow_IsMarkedWithReason_AndNonVoidedHasEmptyReason()
        {
            var lines = Render(Report(Line(4m, 2), Line(4m, 1, voided: true, reason: "Cobro duplicado")));

            Assert.EndsWith(";No;", lines[1]);
            Assert.EndsWith(";Sí;Cobro duplicado", lines[2]);
        }

        [Fact]
        public void BuildRows_SumOfNonVoidedSubtotals_EqualsReportTotal()
        {
            var report = Report(
                Line(4m, 2),
                Line(12.5m, 1),
                Line(25m, 1, voided: true, reason: "Error"));

            var rows = CashCloseCsvBuilder.BuildRows(report);

            var sum = rows.Where(r => r.Anulada == "No").Sum(r => decimal.Parse(r.Subtotal, Es));
            Assert.Equal(report.Total, sum);
            Assert.Equal(3, rows.Count); // las anuladas se exportan, pero no suman
        }

        [Theory]
        [InlineData("=1+1")]
        [InlineData("+1+1")]
        [InlineData("@SUM(A1)")]
        public void Write_ReasonStartingWithFormulaCharacter_IsEscaped(string reason)
        {
            var row = Render(Report(Line(4m, 1, voided: true, reason: reason)))[1];
            var reasonField = row[(row.IndexOf(";Sí;", StringComparison.Ordinal) + ";Sí;".Length)..];

            // El campo exportado no puede empezar por el carácter de fórmula (ni siquiera entre comillas)
            Assert.Matches("^\"?'", reasonField);
            Assert.DoesNotMatch("^\"?[=+@]", reasonField);
        }

        [Fact]
        public void Write_NegativeAmount_StaysNumeric_NotEscapedAsText()
        {
            // Hoy no hay importes negativos, pero un número debe seguir siendo número en Excel
            var row = Render(Report(Line(-5m, 1)))[1];

            Assert.Contains(";-5,00;-5,00;", row);
            Assert.DoesNotContain("'-5,00", row);
        }

        [Fact]
        public void Write_TextFieldStartingWithMinus_IsEscaped()
        {
            var row = Render(Report(Line(4m, 1, concept: "-2+3")))[1];

            Assert.DoesNotContain(";-2+3;", row);
            Assert.Contains("'-2+3", row);
        }
    }
}
