using QuickTix.Core.Time;

namespace QuickTix.Tests.Time
{
    /// <summary>
    /// El "día" del negocio es el día local de Madrid, no el día UTC. Estos tests fijan
    /// la conversión día local -> intervalo UTC [inicio, fin) incluidos los cambios de hora.
    /// </summary>
    public class LocalBusinessDayTests
    {
        private static DateTime Utc(int y, int m, int d, int h = 0, int min = 0) =>
            new DateTime(y, m, d, h, min, 0, DateTimeKind.Utc);

        [Fact]
        public void ToUtcRange_NormalSummerDay_Is24HoursStartingAt22UtcPreviousDay()
        {
            var (start, end) = LocalBusinessDay.ToUtcRange(new DateOnly(2026, 7, 15));

            Assert.Equal(Utc(2026, 7, 14, 22), start);
            Assert.Equal(Utc(2026, 7, 15, 22), end);
            Assert.Equal(DateTimeKind.Utc, start.Kind);
        }

        [Fact]
        public void ToUtcRange_NormalWinterDay_Is24HoursStartingAt23UtcPreviousDay()
        {
            var (start, end) = LocalBusinessDay.ToUtcRange(new DateOnly(2026, 1, 15));

            Assert.Equal(Utc(2026, 1, 14, 23), start);
            Assert.Equal(Utc(2026, 1, 15, 23), end);
        }

        [Fact]
        public void ToUtcRange_LastSundayOfMarch_Has23Hours()
        {
            // 2026-03-29: a las 02:00 locales se salta a las 03:00.
            var (start, end) = LocalBusinessDay.ToUtcRange(new DateOnly(2026, 3, 29));

            Assert.Equal(Utc(2026, 3, 28, 23), start);
            Assert.Equal(Utc(2026, 3, 29, 22), end);
            Assert.Equal(TimeSpan.FromHours(23), end - start);
        }

        [Fact]
        public void ToUtcRange_LastSundayOfOctober_Has25Hours()
        {
            // 2026-10-25: a las 03:00 locales se retrocede a las 02:00.
            var (start, end) = LocalBusinessDay.ToUtcRange(new DateOnly(2026, 10, 25));

            Assert.Equal(Utc(2026, 10, 24, 22), start);
            Assert.Equal(Utc(2026, 10, 25, 23), end);
            Assert.Equal(TimeSpan.FromHours(25), end - start);
        }

        [Fact]
        public void ToUtcRange_InclusiveRange_SpansFromStartOfFirstToEndOfLastDay()
        {
            var (start, end) = LocalBusinessDay.ToUtcRange(new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 26));

            Assert.Equal(Utc(2026, 10, 23, 22), start);
            Assert.Equal(Utc(2026, 10, 26, 23), end);
        }

        [Fact]
        public void ToUtcRange_FromAfterTo_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                LocalBusinessDay.ToUtcRange(new DateOnly(2026, 7, 16), new DateOnly(2026, 7, 15)));
        }

        [Fact]
        public void ToLocalDate_SummerSaleAt2130Utc_BelongsToThatMadridDay()
        {
            // 21:30 UTC = 23:30 en Madrid (UTC+2).
            Assert.Equal(new DateOnly(2026, 7, 15), LocalBusinessDay.ToLocalDate(Utc(2026, 7, 15, 21, 30)));
        }

        [Fact]
        public void ToLocalDate_SummerSaleAt2230Utc_BelongsToNextMadridDay()
        {
            // 22:30 UTC = 00:30 del día siguiente en Madrid.
            Assert.Equal(new DateOnly(2026, 7, 16), LocalBusinessDay.ToLocalDate(Utc(2026, 7, 15, 22, 30)));
        }

        [Fact]
        public void TodayLocal_UsesMadridDay()
        {
            Assert.Equal(new DateOnly(2026, 7, 16), LocalBusinessDay.TodayLocal(Utc(2026, 7, 15, 22, 30)));
        }

        [Fact]
        public void ToLocalDateTime_ConvertsUtcToMadridWallClock()
        {
            var local = LocalBusinessDay.ToLocalDateTime(Utc(2026, 7, 15, 21, 30));

            Assert.Equal(new DateTime(2026, 7, 15, 23, 30, 0), local);
        }
    }
}
