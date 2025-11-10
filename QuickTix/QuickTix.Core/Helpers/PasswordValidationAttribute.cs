using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace QuickTix.Core.Helpers
{
    public class PasswordValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            string password = value.ToString()!;

            // Ejemplo: La contraseña debe tener al menos 8 caracteres, 
            // una letra mayúscula, una minúscula y un dígito.
            if (password.Length < 8)
            {
                ErrorMessage = "Password must be at least 8 characters long.";
                return false;
            }
            /**
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                ErrorMessage = "Password must contain at least one uppercase letter.";
                return false;
            }
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                ErrorMessage = "Password must contain at least one lowercase letter.";
                return false;
            }*/
            if (!Regex.IsMatch(password, @"\d"))
            {
                ErrorMessage = "Password must contain at least one digit.";
                return false;
            }
            return true;
        }
    }
}
