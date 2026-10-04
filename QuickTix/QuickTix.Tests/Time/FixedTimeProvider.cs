namespace QuickTix.Tests.Time
{
    /// <summary>
    /// Reloj fijo para tests deterministas (evita depender del paquete Testing de Microsoft).
    /// </summary>
    public sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
