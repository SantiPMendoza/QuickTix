using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using QuickTix.Core.Helpers;

namespace QuickTix.Desktop.Models.UserDTO
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
