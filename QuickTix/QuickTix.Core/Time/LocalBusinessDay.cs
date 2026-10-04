namespace QuickTix.Core.Time
{
    /// <summary>
    /// Define el "día" del negocio: el día local de Madrid (Europe/Madrid), no el día UTC.
    ///
    /// Las ventas se guardan en UTC (<c>Sale.Date</c>), pero una venta a las 23:30 de Madrid en
    /// verano son las 21:30 UTC y a las 00:30 son las 22:30 UTC del día anterior: agrupar por
    /// día UTC falsearía el arqueo y el Panel. Todo el que agrupe por día debe pasar por aquí.
    ///
    /// Los intervalos devueltos son semiabiertos [inicio, fin) en UTC y son correctos en los
    /// días de cambio de hora (23 h el último domingo de marzo, 25 h el de octubre).
    /// </summary>
    public static class LocalBusinessDay
    {
        // "Europe/Madrid" en Linux/macOS (IANA); "Romance Standard Time" en Windows sin ICU.
        private static readonly TimeZoneInfo MadridZone = ResolveMadridZone();

        private static TimeZoneInfo ResolveMadridZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            }
        }

        /// <summary>Intervalo UTC [inicio, fin) de un único día local.</summary>
        public static (DateTime StartUtc, DateTime EndUtcExclusive) ToUtcRange(DateOnly localDay)
            => ToUtcRange(localDay, localDay);

        /// <summary>
        /// Intervalo UTC [inicio, fin) de un rango inclusivo de días locales
        /// (desde las 00:00 locales de <paramref name="fromLocal"/> hasta las 00:00 locales
        /// del día siguiente a <paramref name="toLocal"/>).
        /// </summary>
        public static (DateTime StartUtc, DateTime EndUtcExclusive) ToUtcRange(DateOnly fromLocal, DateOnly toLocal)
        {
            if (fromLocal > toLocal)
                throw new ArgumentException("La fecha inicial no puede ser posterior a la final.", nameof(fromLocal));

            return (LocalMidnightToUtc(fromLocal), LocalMidnightToUtc(toLocal.AddDays(1)));
        }

        /// <summary>Día local de Madrid correspondiente a un instante UTC.</summary>
        public static DateOnly ToLocalDate(DateTime utc)
            => DateOnly.FromDateTime(ToLocalDateTime(utc));

        /// <summary>Hora local de Madrid (sin zona) de un instante UTC.</summary>
        public static DateTime ToLocalDateTime(DateTime utc)
        {
            var asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(asUtc, MadridZone);
        }

        /// <summary>Día local de Madrid "de hoy" para un instante UTC dado (inyectable para tests).</summary>
        public static DateOnly TodayLocal(DateTime utcNow) => ToLocalDate(utcNow);

        // La medianoche local nunca cae en el hueco ni en el solape del cambio de hora de
        // Madrid (los cambios ocurren a las 02:00/03:00), así que la conversión no es ambigua.
        private static DateTime LocalMidnightToUtc(DateOnly localDay)
        {
            var localMidnight = DateTime.SpecifyKind(localDay.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(localMidnight, MadridZone);
        }
    }
}
