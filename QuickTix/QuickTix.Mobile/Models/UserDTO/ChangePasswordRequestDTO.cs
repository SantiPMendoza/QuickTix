namespace QuickTix.Mobile.Models.UserDTO;

public class ChangePasswordRequestDTO
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
