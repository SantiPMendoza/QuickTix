using Microsoft.Win32;
using QuickTix.Contracts.DTOs.ReportDTOs;

namespace QuickTix.Desktop.ViewModels
{
    // ===== Filas ya formateadas para la vista (el XAML solo pinta texto) =====

    /// <summary>Total de un recinto con sus vendedores.</summary>
    public sealed record CashCloseVenueRow(string VenueName, string TotalText, IReadOnlyList<CashCloseSellerRow> Sellers);

    /// <summary>Total de un vendedor (gestor o «Administración»).</summary>
    public sealed record CashCloseSellerRow(string SellerName, string SalesText, string TotalText);

    /// <summary>Total de un concepto.</summary>
    public sealed record CashCloseConceptRow(string Concept, int Quantity, string TotalText);

    /// <summary>Total de un medio de pago.</summary>
    public sealed record CashClosePaymentRow(string MethodName, string TotalText);

    /// <summary>Línea de venta del arqueo.</summary>
    public sealed record CashCloseLineRow(
        string DateText, string VenueName, string SellerName, string Concept,
        int Quantity, string UnitPriceText, string SubtotalText, string PaymentText, string VoidReason);

    /// <summary>
    /// ViewModel de la página «Cierre de caja» (arqueo por rango de días, solo admin).
    /// Página de solo lectura (como el Panel): consulta <c>GET api/Reports/cash-close</c>,
    /// precalcula el texto de cada tabla y permite exportar el informe cargado a CSV.
    /// </summary>
    public partial class CashCloseViewModel : ObservableObject
    {
        private readonly HttpJsonClient _httpClient;
        private readonly IAuthService _authService;

        // Último informe cargado: el CSV exporta ESTE, no lo que haya ahora en los DatePicker.
        private CashCloseReportDTO? _report;

        // ===== Filtros (por defecto, hoy) =====
        [ObservableProperty] private DateTime? fromDate = DateTime.Today;
        [ObservableProperty] private DateTime? toDate = DateTime.Today;

        // ===== Estado (mensajes inline, sin MessageBox) =====
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;
        [ObservableProperty] private string? exportMessage;

        /// <summary>Hay un informe cargado (habilita exportar y muestra los resultados).</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
        private bool hasReport;

        // ===== Resumen =====
        [ObservableProperty] private string rangeText = string.Empty;
        [ObservableProperty] private string totalText = "—";
        [ObservableProperty] private string saleCountText = "—";
        [ObservableProperty] private string voidedTotalText = "—";
        [ObservableProperty] private string voidedCountText = "—";

        // ===== Desgloses y líneas =====
        [ObservableProperty] private ObservableCollection<CashCloseVenueRow> venues = [];
        [ObservableProperty] private ObservableCollection<CashCloseConceptRow> concepts = [];
        [ObservableProperty] private ObservableCollection<CashClosePaymentRow> paymentMethods = [];
        [ObservableProperty] private ObservableCollection<CashCloseLineRow> lines = [];
        [ObservableProperty] private ObservableCollection<CashCloseLineRow> voidedLines = [];
        [ObservableProperty] private bool hasVoidedLines;

        /// <summary>Solo los admin acceden al arqueo (la API responde 403 al resto).</summary>
        public bool IsAdmin => _authService.IsAdmin;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="CashCloseViewModel"/>.
        /// No carga nada al construirse: la vista consulta al mostrarse y el admin con «Consultar».
        /// </summary>
        public CashCloseViewModel(HttpJsonClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
            _authService.SessionChanged += () => OnPropertyChanged(nameof(IsAdmin));
        }

        /// <summary>
        /// Consulta el arqueo del rango seleccionado y vuelca el resultado en las tablas.
        /// </summary>
        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            ErrorMessage = null;
            ExportMessage = null;

            if (!IsAdmin)
            {
                ErrorMessage = "El cierre de caja solo está disponible para administradores.";
                return;
            }

            if (FromDate is null || ToDate is null)
            {
                ErrorMessage = "Indica las fechas «Desde» y «Hasta».";
                return;
            }

            var from = DateOnly.FromDateTime(FromDate.Value);
            var to = DateOnly.FromDateTime(ToDate.Value);

            if (from > to)
            {
                ErrorMessage = "La fecha «Desde» no puede ser posterior a «Hasta».";
                return;
            }

            try
            {
                IsBusy = true;

                var report = await _httpClient.GetAsync<CashCloseReportDTO>(ApiRoutes.Reports.CashCloseByRange(from, to));

                if (report == null)
                {
                    ErrorMessage = "La API devolvió un informe vacío.";
                    return;
                }

                ApplyReport(report);
            }
            catch (ApiException apiEx)
            {
                // 400 (rango inválido) llega con el mensaje en español del envelope
                ErrorMessage = apiEx.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "El cierre de caja solo está disponible para administradores."
                    : $"No se pudo cargar el cierre de caja. {apiEx.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local cargando el cierre de caja: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Guarda el informe cargado como CSV (E3) en la ruta que elija el usuario.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasReport))]
        private void ExportCsv()
        {
            if (_report == null)
                return;

            ExportMessage = null;

            // El diálogo de guardado vive aquí por simplicidad (Desktop no tiene servicio de diálogos de fichero).
            var dialog = new SaveFileDialog
            {
                Title = "Exportar cierre de caja",
                Filter = "CSV (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true,
                FileName = CashCloseCsvBuilder.DefaultFileName(_report.From, _report.To)
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                CashCloseCsvBuilder.WriteToFile(dialog.FileName, _report);
                ExportMessage = $"CSV guardado en {dialog.FileName}. Las ventas anuladas van marcadas y no suman en el total.";
            }
            catch (Exception ex)
            {
                // Típico: el fichero está abierto en Excel
                ErrorMessage = $"No se pudo guardar el CSV: {ex.Message}";
            }
        }

        /// <summary>
        /// Vuelca el informe en las propiedades de la vista (todo precalculado y formateado).
        /// </summary>
        private void ApplyReport(CashCloseReportDTO report)
        {
            _report = report;

            RangeText = report.From == report.To
                ? EsFormat.Date(report.From)
                : $"{EsFormat.Date(report.From)} – {EsFormat.Date(report.To)}";

            TotalText = EsFormat.Money(report.Total);
            SaleCountText = report.SaleCount.ToString(EsFormat.Culture);
            VoidedTotalText = EsFormat.Money(report.VoidedTotal);
            VoidedCountText = report.VoidedCount.ToString(EsFormat.Culture);

            Venues = new ObservableCollection<CashCloseVenueRow>(report.ByVenue.Select(v =>
                new CashCloseVenueRow(
                    v.VenueName,
                    EsFormat.Money(v.Total),
                    v.BySeller.Select(s => new CashCloseSellerRow(
                        s.SellerName,
                        s.SaleCount == 1 ? "1 venta" : $"{s.SaleCount} ventas",
                        EsFormat.Money(s.Total))).ToList())));

            Concepts = new ObservableCollection<CashCloseConceptRow>(report.ByConcept.Select(c =>
                new CashCloseConceptRow(c.Concept, c.Quantity, EsFormat.Money(c.Total))));

            PaymentMethods = new ObservableCollection<CashClosePaymentRow>(report.ByPaymentMethod.Select(p =>
                new CashClosePaymentRow(EsFormat.PaymentMethodName(p.PaymentMethod), EsFormat.Money(p.Total))));

            // Las anuladas no se mezclan con las líneas cobradas: van en su propia sección con motivo.
            Lines = new ObservableCollection<CashCloseLineRow>(
                report.Lines.Where(l => !l.IsVoided).Select(ToRow));
            VoidedLines = new ObservableCollection<CashCloseLineRow>(
                report.Lines.Where(l => l.IsVoided).Select(ToRow));
            HasVoidedLines = VoidedLines.Count > 0;

            HasReport = true;
        }

        private static CashCloseLineRow ToRow(CashCloseLineDTO l) => new(
            EsFormat.DateTimeLocal(l.LocalDateTime),
            l.VenueName,
            l.SellerName,
            l.Concept,
            l.Quantity,
            EsFormat.Money(l.UnitPrice),
            EsFormat.Money(l.Subtotal),
            EsFormat.PaymentMethodName(l.PaymentMethod),
            l.VoidReason ?? string.Empty);
    }
}
