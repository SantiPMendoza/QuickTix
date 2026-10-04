using System.ComponentModel.DataAnnotations;

namespace QuickTix.Contracts.Validation.Attributes
{
    /// <summary>
    /// Longitud máxima medida sobre el texto recortado (Trim). Coincide con la regla de los
    /// repositorios, que recortan antes de guardar: espacios de relleno no cuentan.
    /// Un valor nulo es válido (la obligatoriedad la decide [Required]).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class TrimmedMaxLengthAttribute : ValidationAttribute
    {
        public int MaxLength { get; }

        public TrimmedMaxLengthAttribute(int maxLength)
        {
            MaxLength = maxLength;
            ErrorMessage = $"No puede superar los {maxLength} caracteres.";
        }

        public override bool IsValid(object? value)
            => value is not string s || s.Trim().Length <= MaxLength;
    }
}
