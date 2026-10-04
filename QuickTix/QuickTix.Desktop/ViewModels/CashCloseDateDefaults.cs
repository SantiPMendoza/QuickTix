namespace QuickTix.Desktop.ViewModels
{
    /// <summary>Fechas «Desde/Hasta» resultantes y el día para el que son el valor por defecto (null si las eligió el usuario).</summary>
    public readonly record struct CashCloseDates(DateTime? From, DateTime? To, DateOnly? DefaultDay);

    /// <summary>
    /// Decisión (pura, testeable) de las fechas por defecto del cierre de caja.
    /// Solo se aplica al NAVEGAR a la página; la consulta explícita nunca toca las fechas.
    /// </summary>
    public static class CashCloseDateDefaults
    {
        /// <summary>
        /// Al abrir la página: fija «hoy» si ambas fechas están vacías, o lo refresca si siguen siendo
        /// el «hoy» por defecto de un día anterior (app abierta de un día para otro). Un rango tocado
        /// por el usuario (<paramref name="defaultDay"/> null) o con una sola fecha vacía se respeta.
        /// </summary>
        public static CashCloseDates ForNavigation(DateTime? from, DateTime? to, DateOnly? defaultDay, DateOnly today)
        {
            var todayDate = today.ToDateTime(TimeOnly.MinValue);

            if (from is null && to is null)
                return new CashCloseDates(todayDate, todayDate, today);

            bool untouchedStaleDefault =
                defaultDay is { } day
                && day != today
                && from?.Date == day.ToDateTime(TimeOnly.MinValue)
                && to?.Date == day.ToDateTime(TimeOnly.MinValue);

            return untouchedStaleDefault
                ? new CashCloseDates(todayDate, todayDate, today)
                : new CashCloseDates(from, to, defaultDay);
        }
    }
}
