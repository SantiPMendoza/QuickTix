using QuickTix.Contracts.DTOs.UserAuthDTO;
using QuickTix.Desktop.ViewModels.Base;

namespace QuickTix.Desktop.ViewModels
{
    public partial class LoginViewModel : ViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private bool _rememberUser;


        /// <summary>
        /// Inicializa una nueva instancia de <see cref="LoginViewModel"/>.
        /// </summary>
        /// <param name="authService">Servicio de autenticación.</param>
        /// <param name="navigationService">Servicio de navegación.</param>
        public LoginViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;


            // Cargar datos guardados si RememberUser está activado
            RememberUser = Properties.Settings.Default.RememberUser;

            if (RememberUser)
            {
                Username = Properties.Settings.Default.SavedUsername;
                Password = Properties.Settings.Default.SavedPassword;
            }

            ValidateLogin();
        }

        // Campos de entrada del usuario
        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        // Habilita o deshabilita el botón de login
        [ObservableProperty]
        private bool _isLoginEnabled = false;

        // ===== Aviso modal (VibraDialog en modo alerta, fix 2b) =====
        // Sustituye a los MessageBox del flujo de login.
        [ObservableProperty] private bool _isAlertOpen;
        [ObservableProperty] private string? _alertTitle;
        [ObservableProperty] private string? _alertMessage;

        // True cuando el aviso de bienvenida debe navegar al Panel al cerrarse
        private bool _navigateToPanelOnAlertClose;

        /// <summary>
        /// Muestra un aviso modal con título y mensaje.
        /// </summary>
        /// <param name="title">Título del aviso.</param>
        /// <param name="message">Mensaje del aviso.</param>
        private void ShowAlert(string title, string message)
        {
            AlertTitle = title;
            AlertMessage = message;
            IsAlertOpen = true;
        }

        /// <summary>
        /// Cierra el aviso modal. Tras el aviso de bienvenida, navega al Panel.
        /// </summary>
        [RelayCommand]
        private void CloseAlert()
        {
            IsAlertOpen = false;

            if (_navigateToPanelOnAlertClose)
            {
                _navigateToPanelOnAlertClose = false;

                // El Panel (dashboard) es la vista inicial tras el login (spec 3a)
                _navigationService.Navigate(typeof(PanelView));
            }
        }

        /// <summary>
        /// Comando que intenta iniciar sesión con las credenciales ingresadas.
        /// </summary>
        [RelayCommand]
        private async Task CheckLogin()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ShowAlert("Campos incompletos", "Por favor, completa todos los campos.");
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
                    // ======================================
                    // Guardar preferencias
                    // ======================================
                    if (RememberUser)
                    {
                        Properties.Settings.Default.SavedUsername = Username;
                        Properties.Settings.Default.SavedPassword = Password;
                        Properties.Settings.Default.RememberUser = true;
                    }
                    else
                    {
                        Properties.Settings.Default.SavedUsername = string.Empty;
                        Properties.Settings.Default.SavedPassword = string.Empty;
                        Properties.Settings.Default.RememberUser = false;
                    }

                    Properties.Settings.Default.Save();

                    // ======================================
                    var user = _authService.GetCurrentUser();

                    // La navegación al Panel ocurre al cerrar el aviso de
                    // bienvenida (CloseAlert): si navegáramos ya, la página
                    // de login desaparecería y el aviso con ella.
                    _navigateToPanelOnAlertClose = true;
                    ShowAlert("Sesión iniciada", $"Bienvenido {user?.Name}");
                }
                else
                {
                    ShowAlert("Error de acceso", "Usuario o contraseña incorrectos.");
                }
            }
            catch (Exception ex)
            {
                ShowAlert("Error de conexión", $"No se pudo conectar con el servidor: {ex.Message}");
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
