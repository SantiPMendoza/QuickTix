using System.ComponentModel.DataAnnotations;

namespace QuickTix.Contracts.Validation.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class NifNieAttribute : ValidationAttribute
    {
        public NifNieAttribute()
        {
            ErrorMessage = "El NIF/NIE no es válido.";
        }

        public override bool IsValid(object? value)
        {
            if (value is null) return true; // Campo opcional
            if (value is not string s) return false;

            var v = s.Trim();
            if (v.Length == 0) return true; // Permite vacío si no es obligatorio

            return SpanishIdValidator.IsValidNifOrNie(v);
        }
    }
}
