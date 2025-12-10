using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using QuickTix.Mobile.Models.UserDTO;
using QuickTix.Mobile.Services;

namespace QuickTix.Mobile.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;

            RememberUser = Preferences.Get("RememberUser", false);

            if (RememberUser)
            {
                Username = Preferences.Get("SavedUsername", "");
                Password = Preferences.Get("SavedPassword", "");
            }

            ValidateLogin();
        }

        // ------------------------------
        // PROPIEDADES GENERADAS
        // ------------------------------

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool rememberUser;

        [ObservableProperty]
        private bool isLoginEnabled;

        // ------------------------------
        // COMANDO DE LOGIN
        // ------------------------------
        [RelayCommand]
        private async Task CheckLoginAsync()
        {
            if (!IsLoginEnabled)
            {
                await Application.Current.MainPage.DisplayAlert("Aviso", "Completa todos los campos.", "OK");
                return;
            }

            var dto = new UserLoginDTO
            {
                UserName = Username,
                Password = Password
            };

            try
            {
                var success = await _authService.LoginAsync(dto);

                if (!success)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Usuario o contraseña incorrectos.", "OK");
                    return;
                }

                // Guardar preferencias de usuario
                if (RememberUser)
                {
                    Preferences.Set("SavedUsername", Username);
                    Preferences.Set("SavedPassword", Password);
                    Preferences.Set("RememberUser", true);
                }
                else
                {
                    Preferences.Remove("SavedUsername");
                    Preferences.Remove("SavedPassword");
                    Preferences.Set("RememberUser", false);
                }

                // Obtener el usuario actual desde AuthService
                var user = _authService.GetCurrentUser();

                if (user is null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No se pudo obtener la información del usuario.", "OK");
                    return;
                }

                // Navegación según rol: cambiando completamente la Shell
                var role = user.Role?.ToLowerInvariant() ?? string.Empty;

                switch (role)
                {
                    case "client":
                        App.Current.MainPage = new AppShell_Client();
                        break;

                    case "manager":
                        App.Current.MainPage = new AppShell_Manager();
                        break;

                    case "admin":
                        // Si en el futuro tienes un AppShell_Admin, cámbialo aquí
                        App.Current.MainPage = new AppShell_Manager();
                        break;

                    default:
                        await Application.Current.MainPage.DisplayAlert(
                            "Error",
                            $"Rol no soportado: {user.Role ?? "(sin rol)"}",
                            "OK");
                        break;
                }

            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }


        // ------------------------------
        // MÉTODOS PARCIALES
        // Estos los genera automáticamente el toolkit
        // ------------------------------

        partial void OnUsernameChanged(string value) => ValidateLogin();
        partial void OnPasswordChanged(string value) => ValidateLogin();

        // ------------------------------
        // VALIDACIÓN DEL FORMULARIO
        // ------------------------------

        private void ValidateLogin()
        {
            IsLoginEnabled =
                !string.IsNullOrWhiteSpace(Username)
                && !string.IsNullOrWhiteSpace(Password);
        }
    }
}
