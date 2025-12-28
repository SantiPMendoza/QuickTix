using QuickTix.Core.Models.Entities;

namespace QuickTix.Contracts.DTOs.UserAuthDTO
{
    public class UserLoginResponseDTO
    {
        public UserDTO User { get; set; } = null!;
        public string Token { get; set; } = string.Empty;
    }
}
