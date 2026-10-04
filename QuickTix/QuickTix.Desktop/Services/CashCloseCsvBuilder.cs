using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using QuickTix.Contracts.DTOs.ReportDTOs;

namespace QuickTix.Desktop.Services
{
    /// <summary>
    /// Fila del CSV de arqueo: todo ya formateado como texto es-ES, una por línea del informe.
    /// </summary>
    public sealed record CashCloseCsvRow(
        string Fecha,
        string Recinto,
        string Vendedor,
        string Concepto,
        int Cantidad,
        string PrecioUnitario,
        string Subtotal,
        string MedioPago,
        string Anulada,
        string MotivoAnulacion);

    /// <summary>
    /// Convierte un <see cref="CashCloseReportDTO"/> en el CSV del arqueo (E3). Lógica pura y sin
    /// dependencias de WPF para poder probarla más adelante.
    /// <para>
    /// Formato pensado para Excel en español: UTF-8 con BOM, delimitador «;» y números con coma
    /// decimal sin separador de millares. Las líneas anuladas se exportan marcadas con «Sí» y su
    /// motivo; NO suman en el total del arqueo (para cuadrar: sumar Subtotal donde Anulada = «No»).
    /// No se añade fila de total para que la columna Subtotal siga siendo un dato homogéneo
    /// (filtros, tablas dinámicas y SUMA no cuentan el total dos veces).
    /// </para>
    /// </summary>
    public static class CashCloseCsvBuilder
    {
        /// <summary>Cabeceras en el orden de las columnas.</summary>
        public static readonly IReadOnlyList<string> Headers =
        [
            "Fecha", "Recinto", "Vendedor", "Concepto", "Cantidad", "PrecioUnitario",
            "Subtotal", "MedioPago", "Anulada", "MotivoAnulacion"
        ];

        /// <summary>Nombre de fichero por defecto: arqueo_yyyyMMdd_yyyyMMdd.csv.</summary>
        public static string DefaultFileName(DateOnly from, DateOnly to) =>
            $"arqueo_{from.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}_{to.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.csv";

        /// <summary>Mapea cada línea del informe (anuladas incluidas) a su fila de CSV, en el mismo orden.</summary>
        public static List<CashCloseCsvRow> BuildRows(CashCloseReportDTO report) =>
            report.Lines.Select(l => new CashCloseCsvRow(
                Fecha: EsFormat.DateTimeLocal(l.LocalDateTime),
                Recinto: l.VenueName,
                Vendedor: l.SellerName,
                Concepto: l.Concept,
                Cantidad: l.Quantity,
                PrecioUnitario: Amount(l.UnitPrice),
                Subtotal: Amount(l.Subtotal),
                MedioPago: EsFormat.PaymentMethodName(l.PaymentMethod),
                Anulada: l.IsVoided ? "Sí" : "No",
                MotivoAnulacion: l.IsVoided ? l.VoidReason ?? string.Empty : string.Empty)).ToList();

        /// <summary>Escribe cabecera y filas con CsvHelper en el writer indicado.</summary>
        public static void Write(TextWriter writer, CashCloseReportDTO report)
        {
            var config = new CsvConfiguration(EsFormat.Culture)
            {
                Delimiter = ";",
                // El motivo lo teclea un admin: un «=» o «+» inicial se interpretaría como fórmula en Excel.
                InjectionOptions = InjectionOptions.Escape
            };

            using var csv = new CsvWriter(writer, config, leaveOpen: true);

            foreach (var header in Headers)
                csv.WriteField(header);
            csv.NextRecord();

            foreach (var row in BuildRows(report))
            {
                csv.WriteField(row.Fecha);
                csv.WriteField(row.Recinto);
                csv.WriteField(row.Vendedor);
                csv.WriteField(row.Concepto);
                csv.WriteField(row.Cantidad);
                csv.WriteField(row.PrecioUnitario);
                csv.WriteField(row.Subtotal);
                csv.WriteField(row.MedioPago);
                csv.WriteField(row.Anulada);
                csv.WriteField(row.MotivoAnulacion);
                csv.NextRecord();
            }

            csv.Flush();
        }

        /// <summary>Guarda el CSV en disco: UTF-8 con BOM (Excel ES lo necesita para las tildes).</summary>
        public static void WriteToFile(string path, CashCloseReportDTO report)
        {
            using var writer = new StreamWriter(path, append: false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            Write(writer, report);
        }

        // Dos decimales fijos con coma, sin separador de millares (Excel ES lo lee como número).
        private static string Amount(decimal value) => value.ToString("F2", EsFormat.Culture);
    }
}
