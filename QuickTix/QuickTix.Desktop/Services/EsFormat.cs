using QuickTix.Contracts.Enums;

namespace QuickTix.Desktop.Services
{
    /// <summary>
    /// Formatos de presentación en es-ES compartidos por la pantalla «Cierre de caja» y su CSV.
    /// Se formatea en C# (no con StringFormat de XAML) porque WPF enlaza por defecto con en-US.
    /// </summary>
    public static class EsFormat
    {
        /// <summary>Cultura española usada en toda la presentación del arqueo.</summary>
        public static readonly CultureInfo Culture = new("es-ES");

        /// <summary>Importe para pantalla: «1.234,50 €».</summary>
        public static string Money(decimal value) => value.ToString("N2", Culture) + " €";

        /// <summary>Fecha y hora local para pantalla y CSV: «15/07/2026 23:30».</summary>
        public static string DateTimeLocal(DateTime value) =>
            value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

        /// <summary>Fecha corta: «15/07/2026».</summary>
        public static string Date(DateOnly value) =>
            value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        /// <summary>Nombre en español del medio de pago (la API lo serializa como número).</summary>
        public static string PaymentMethodName(PaymentMethod method) => method switch
        {
            PaymentMethod.Cash => "Efectivo",
            PaymentMethod.Card => "Tarjeta",
            PaymentMethod.Bizum => "Bizum",
            _ => method.ToString()
        };
    }
}
