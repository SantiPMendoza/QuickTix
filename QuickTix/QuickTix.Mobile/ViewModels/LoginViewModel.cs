using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using QuickTix.Contracts.DTOs.UserAuthDTO;
using QuickTix.Mobile.Services;
using QuickTix.Mobile.Views;

namespace QuickTix.Mobile.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider _services;

        public LoginViewModel(IAuthService authService, IServiceProvider services)
        {
            _authService = authService;

            _services = services;

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

                if (user.MustChangePassword)
                {
                    var page = _services.GetRequiredService<ChangePasswordPage>();
                    await Application.Current.MainPage.Navigation.PushAsync(page);
                    return;
                }


                // Si NO requiere cambio, entonces cambias a la Shell según rol
                var role = (user.Role ?? string.Empty).Trim().ToLowerInvariant();

                switch (role)
                {
                    case "client":
                        App.Current.MainPage = new AppShell_Client();
                        break;

                    case "manager":
                        App.Current.MainPage = new AppShell_Manager();
                        break;

                    case "admin":
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
