using System.ComponentModel.DataAnnotations;

namespace QuickTix.Contracts.DTOs.UserAuthDTO
{
    public class UserLoginDTO
    {
        [Required(ErrorMessage = "Field required: UserName")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Field required: Password")]
        public string Password { get; set; } = null!;
    }
}
