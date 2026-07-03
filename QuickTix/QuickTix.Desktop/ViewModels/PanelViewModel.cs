using QuickTix.Contracts.DTOs.AnalyticsDTOs;
using System.Windows.Media;

namespace QuickTix.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel del Panel (dashboard, spec 3a del handoff Vibra).
    /// Consume el endpoint de solo lectura /api/Analytics/summary y precalcula
    /// en C# todo lo que la vista necesita dibujar (alturas de barras, arcos del
    /// donut, porcentajes) para que el XAML no haga cálculos ni divisiones.
    /// </summary>
    public partial class PanelViewModel : ObservableObject
    {
        // Cliente HTTP compartido (patrón de los demás ViewModels de Desktop)
        private readonly HttpJsonClient _httpClient;

        // Geometría del donut: lienzo 160x160, radio medio del anillo y centro
        private const double DonutSize = 160;
        private const double DonutRadius = 58;

        // Altura máxima (px) de las barras de la gráfica de ingresos
        private const double MaxBarHeight = 120;

        // Altura mínima para que un día sin ventas siga siendo visible
        private const double MinBarHeight = 3;

        // Cultura para etiquetas de día en español ("lun", "mar", ...)
        private static readonly CultureInfo SpanishCulture = new("es-ES");

        // ===== Estado de carga / error (inline, sin MessageBox) =====
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;

        // ===== KPIs =====
        [ObservableProperty] private decimal revenueToday;
        [ObservableProperty] private int ticketsSoldToday;
        [ObservableProperty] private int activeSubscriptions;
        [ObservableProperty] private int estimatedAttendanceToday;

        // ===== Gráfica de ingresos (7 días) =====
        [ObservableProperty] private ObservableCollection<RevenueBarItem> revenueBars = [];

        // ===== Donut "Ventas por tipo" =====
        [ObservableProperty] private Geometry ticketsArc = Geometry.Empty;
        [ObservableProperty] private Geometry subscriptionsArc = Geometry.Empty;
        [ObservableProperty] private int totalUnits;
        [ObservableProperty] private int ticketUnits;
        [ObservableProperty] private int subscriptionUnits;
        [ObservableProperty] private string ticketsPercentText = "—";
        [ObservableProperty] private string subscriptionsPercentText = "—";
        [ObservableProperty] private bool hasSalesData;

        // ===== Ventas recientes =====
        [ObservableProperty] private ObservableCollection<RecentSaleDTO> recentSales = [];

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PanelViewModel"/>.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para consumo de la API.</param>
        public PanelViewModel(HttpJsonClient httpClient)
        {
            _httpClient = httpClient;
            _ = LoadAsync();
        }

        /// <summary>
        /// Carga el resumen de analítica desde la API y actualiza todas las
        /// propiedades visibles del Panel.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ErrorMessage = null;

                var summary = await _httpClient.GetAsync<AnalyticsSummaryDTO>(ApiRoutes.Analytics.Summary);

                if (summary == null)
                {
                    ErrorMessage = "La API devolvió un resumen vacío.";
                    return;
                }

                ApplySummary(summary);
            }
            catch (ApiException apiEx)
            {
                ErrorMessage =
                    $"Error cargando el Panel. Código: {(int)apiEx.StatusCode}. {apiEx.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local cargando el Panel: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Vuelca el DTO de resumen en las propiedades de la vista,
        /// precalculando barras, arcos y porcentajes.
        /// </summary>
        /// <param name="summary">Resumen recibido de la API.</param>
        private void ApplySummary(AnalyticsSummaryDTO summary)
        {
            RevenueToday = summary.RevenueToday;
            TicketsSoldToday = summary.TicketsSoldToday;
            ActiveSubscriptions = summary.ActiveSubscriptions;
            EstimatedAttendanceToday = summary.EstimatedAttendanceToday;

            RevenueBars = new ObservableCollection<RevenueBarItem>(BuildRevenueBars(summary.RevenueLast7Days));
            RecentSales = new ObservableCollection<RecentSaleDTO>(summary.RecentSales);

            ApplyDonut(summary.SalesByType);
        }

        /// <summary>
        /// Construye las barras de la gráfica de ingresos con alturas relativas
        /// al día de mayor importe. Sin ventas en la semana, todas las barras
        /// quedan a altura mínima (nunca hay división por cero).
        /// </summary>
        /// <param name="days">Ingresos por día en orden cronológico.</param>
        /// <returns>Barras listas para pintar.</returns>
        private static List<RevenueBarItem> BuildRevenueBars(List<DailyRevenueDTO> days)
        {
            var maxAmount = days.Count > 0 ? days.Max(d => d.Amount) : 0m;

            return days
                .Select(d => new RevenueBarItem
                {
                    // Etiqueta corta en español: "lun", "mar", ...
                    DayLabel = d.Date.ToString("ddd", SpanishCulture),
                    Amount = d.Amount,
                    BarHeight = maxAmount > 0m
                        ? Math.Max(MinBarHeight, (double)(d.Amount / maxAmount) * MaxBarHeight)
                        : MinBarHeight,
                    // El día pico se destaca con el degradado teal (spec 3a)
                    IsPeak = maxAmount > 0m && d.Amount == maxAmount
                })
                .ToList();
        }

        /// <summary>
        /// Calcula los arcos y porcentajes del donut "Ventas por tipo".
        /// Con 0 ventas no se dibujan arcos (queda el anillo vacío de fondo).
        /// </summary>
        /// <param name="salesByType">Unidades por tipo.</param>
        private void ApplyDonut(SalesByTypeDTO salesByType)
        {
            TicketUnits = salesByType.TicketUnits;
            SubscriptionUnits = salesByType.SubscriptionUnits;
            TotalUnits = salesByType.TicketUnits + salesByType.SubscriptionUnits;
            HasSalesData = TotalUnits > 0;

            if (!HasSalesData)
            {
                TicketsArc = Geometry.Empty;
                SubscriptionsArc = Geometry.Empty;
                TicketsPercentText = "—";
                SubscriptionsPercentText = "—";
                return;
            }

            var ticketsFraction = (double)TicketUnits / TotalUnits;
            var ticketsSweep = ticketsFraction * 360.0;

            // Los arcos parten de las 12 en punto y giran en sentido horario
            TicketsArc = CreateDonutArc(startAngle: 0, sweepAngle: ticketsSweep);
            SubscriptionsArc = CreateDonutArc(startAngle: ticketsSweep, sweepAngle: 360.0 - ticketsSweep);

            var ticketsPercent = (int)Math.Round(ticketsFraction * 100.0);
            TicketsPercentText = $"{ticketsPercent} %";
            SubscriptionsPercentText = $"{100 - ticketsPercent} %";
        }

        /// <summary>
        /// Crea la geometría de un arco de donut (trazo circular) en un lienzo
        /// de <see cref="DonutSize"/> px. Ángulos en grados desde las 12 en punto.
        /// </summary>
        /// <param name="startAngle">Ángulo inicial.</param>
        /// <param name="sweepAngle">Barrido del arco.</param>
        /// <returns>Geometría lista para un Path con StrokeThickness.</returns>
        private static Geometry CreateDonutArc(double startAngle, double sweepAngle)
        {
            if (sweepAngle <= 0)
                return Geometry.Empty;

            var center = new Point(DonutSize / 2, DonutSize / 2);

            // ArcSegment no puede representar 360°: un tipo al 100% se dibuja
            // como circunferencia completa.
            if (sweepAngle >= 359.9)
            {
                var circle = new EllipseGeometry(center, DonutRadius, DonutRadius);
                circle.Freeze();
                return circle;
            }

            var start = PointOnCircle(center, DonutRadius, startAngle);
            var end = PointOnCircle(center, DonutRadius, startAngle + sweepAngle);

            var figure = new PathFigure { StartPoint = start, IsClosed = false, IsFilled = false };
            figure.Segments.Add(new ArcSegment(
                point: end,
                size: new Size(DonutRadius, DonutRadius),
                rotationAngle: 0,
                isLargeArc: sweepAngle > 180,
                sweepDirection: SweepDirection.Clockwise,
                isStroked: true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            geometry.Freeze();
            return geometry;
        }

        /// <summary>
        /// Punto sobre una circunferencia. El ángulo 0 corresponde a las 12 en
        /// punto y crece en sentido horario.
        /// </summary>
        private static Point PointOnCircle(Point center, double radius, double angleDegrees)
        {
            var angleRadians = (angleDegrees - 90.0) * Math.PI / 180.0;
            return new Point(
                center.X + radius * Math.Cos(angleRadians),
                center.Y + radius * Math.Sin(angleRadians));
        }
    }

    /// <summary>
    /// Barra de la gráfica de ingresos del Panel, con la altura ya resuelta
    /// en el ViewModel (la vista solo la pinta).
    /// </summary>
    public sealed class RevenueBarItem
    {
        public string DayLabel { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public double BarHeight { get; init; }
        public bool IsPeak { get; init; }
    }
}
