namespace QuickTix.Core.Interfaces
{
    /// <summary>Resultado de anular una venta (ver <see cref="ISaleRepository.VoidAsync"/>).</summary>
    public enum VoidSaleResult
    {
        Voided,
        NotFound,
        AlreadyVoided,
        InvalidReason
    }
}
