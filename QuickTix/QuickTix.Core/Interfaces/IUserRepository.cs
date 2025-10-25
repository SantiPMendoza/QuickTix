
using QuickTix.Core.Models.DTOs.UserAuthDTO;
using QuickTix.Core.Models.Entities;

namespace QuickTix.Core.Interfaces
{
    /// <summary>
    /// Contrato para el repositorio de usuarios (Identity + JWT).
    /// </summary>
    public interface IUserRepository
    {
        // Obtiene lista de usuarios (solo DTOs)
        Task<List<UserDTO>> GetUserDTOsAsync();

        // Obtiene usuario por Id
        Task<AppUser?> GetUserAsync(string id);

        // Verifica si el nombre de usuario está disponible
        Task<bool> IsUniqueUserAsync(string userName);

        // Login con credenciales
        Task<UserLoginResponseDTO?> LoginAsync(UserLoginDTO dto);

        // Registro de nuevo usuario
        Task<UserLoginResponseDTO?> RegisterAsync(UserRegistrationDTO dto);
    }
}
