using QuickTix.Desktop.Models.UserDTO;
using QuickTix.Desktop.ViewModels.Base;

namespace QuickTix.Desktop.ViewModels
{
    public partial class LoginViewModel : ViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="LoginViewModel"/>.
        /// </summary>
        /// <param name="authService">Servicio de autenticación.</param>
        /// <param name="navigationService">Servicio de navegación.</param>
        public LoginViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
        }

        // Campos de entrada del usuario
        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        // Habilita o deshabilita el botón de login
        [ObservableProperty]
        private bool _isLoginEnabled = false;

        /// <summary>
        /// Comando que intenta iniciar sesión con las credenciales ingresadas.
        /// </summary>
        [RelayCommand]
        private async Task CheckLogin()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                System.Windows.MessageBox.Show("Por favor, completa todos los campos.");
                return;
            }

            var loginData = new UserLoginDTO
            {
                UserName = Username,
                Password = Password
            };

            try
            {
                var success = await _authService.LoginAsync(loginData);

                if (success)
                {
                    var user = _authService.GetCurrentUser();
                    System.Windows.MessageBox.Show($"Bienvenido {user?.Name}");
                    _navigationService.Navigate(typeof(UsersView));
                }
                else
                {
                    System.Windows.MessageBox.Show("Usuario o contraseña incorrectos.");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error de conexión: {ex.Message}");
            }
        }

        /// <summary>
        /// Detecta cambios en el nombre de usuario para validar el inicio de sesión.
        /// </summary>
        /// <param name="value">Nuevo valor del nombre de usuario.</param>
        partial void OnUsernameChanged(string value) => ValidateLogin();

        /// <summary>
        /// Detecta cambios en la contraseña para validar el inicio de sesión.
        /// </summary>
        /// <param name="value">Nuevo valor de la contraseña.</param>
        partial void OnPasswordChanged(string value) => ValidateLogin();

        /// <summary>
        /// Valida si el botón de inicio de sesión debe estar habilitado.
        /// </summary>
        private void ValidateLogin()
        {
            IsLoginEnabled = !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }
    }
}
