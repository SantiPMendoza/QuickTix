using QuickTix.Contracts.Helpers;
using System.ComponentModel.DataAnnotations;

namespace QuickTix.Contracts.DTOs.UserAuthDTO
{
    public class UserRegistrationDTO
    {
        [Required(ErrorMessage = "Field required: Name")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Field required: UserName")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Field required: Email")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Field required: Password")]
        [PasswordValidation]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Field required: Role")]
        public string Role { get; set; } = null!;
    }
}
