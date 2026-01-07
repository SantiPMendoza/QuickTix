using System.ComponentModel.DataAnnotations;

namespace QuickTix.Contracts.Validation.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PhoneNumberAttribute : ValidationAttribute
    {
        public PhoneNumberAttribute()
        {
            ErrorMessage = "El número de teléfono debe tener exactamente 9 dígitos (solo números).";
        }

        public override bool IsValid(object? value)
        {
            if (value is null) return true;
            if (value is not string s) return false;

            var v = s.Trim();
            if (v.Length == 0) return true;

            return RegexCatalog.Phone9Digits().IsMatch(v);
        }
    }
}
