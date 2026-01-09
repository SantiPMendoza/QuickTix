using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using QuickTix.Contracts.DTOs.UserAuthDTO;
using QuickTix.Mobile.Services;
using QuickTix.Mobile.Views;

namespace QuickTix.Mobile.ViewModels
{
    /// <summary>
    /// ViewModel de login para Mobile.
    /// Gestiona credenciales, preferencia de "recordar usuario", validación del formulario,
    /// flujo de cambio obligatorio de contraseña y navegación a la Shell correspondiente por rol.
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider _services;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="LoginViewModel"/>.
        /// Carga preferencias de "recordar usuario" y precarga credenciales si aplica.
        /// </summary>
        /// <param name="authService">Servicio de autenticación.</param>
        /// <param name="services">Proveedor de servicios para resolver páginas en navegación.</param>
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

        [ObservableProperty] private string username = string.Empty;
        [ObservableProperty] private string password = string.Empty;
        [ObservableProperty] private bool rememberUser;
        [ObservableProperty] private bool isLoginEnabled;
        [ObservableProperty] private bool isBusy;

        /// <summary>
        /// Ejecuta el proceso de login.
        /// Valida el formulario, llama al servicio, persiste preferencias y navega según el estado del usuario.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task CheckLoginAsync()
        {
            if (IsBusy)
                return;

            if (!IsLoginEnabled)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current.MainPage.DisplayAlert("Aviso", "Completa todos los campos.", "OK"));

                return;
            }

            IsBusy = true;

            try
            {
                var dto = new UserLoginDTO { UserName = Username, Password = Password };

                var success = await _authService.LoginAsync(dto);
                if (!success)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        Application.Current.MainPage.DisplayAlert("Error", "Usuario o contraseña incorrectos.", "OK"));

                    return;
                }

                // Preferencias (persistencia local de credenciales)
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

                var user = _authService.GetCurrentUser();
                if (user is null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        Application.Current.MainPage.DisplayAlert(
                            "Error",
                            "No se pudo obtener la información del usuario.",
                            "OK"));

                    return;
                }

                if (user.MustChangePassword)
                {
                    var page = _services.GetRequiredService<ChangePasswordPage>();

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Application.Current.MainPage.Navigation.PushAsync(page);
                    });

                    return;
                }

                var role = (user.Role ?? string.Empty).Trim().ToLowerInvariant();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    switch (role)
                    {
                        case "client":
                            App.Current.MainPage = new AppShell_Client();
                            break;

                        case "manager":
                        case "admin":
                            App.Current.MainPage = new AppShell_Manager();
                            break;

                        default:
                            Application.Current.MainPage.DisplayAlert(
                                "Error",
                                $"Rol no soportado: {user.Role ?? "(sin rol)"}",
                                "OK");
                            break;
                    }
                });
            }
            catch (ApiException apiEx)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current.MainPage.DisplayAlert("Error", apiEx.Message, "OK"));
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK"));
            }
            finally
            {
                IsBusy = false;
                ValidateLogin();
            }
        }

        partial void OnUsernameChanged(string value) => ValidateLogin();
        partial void OnPasswordChanged(string value) => ValidateLogin();

        /// <summary>
        /// Valida el estado del formulario y actualiza la disponibilidad del comando de login.
        /// </summary>
        private void ValidateLogin()
        {
            IsLoginEnabled =
                !string.IsNullOrWhiteSpace(Username) &&
                !string.IsNullOrWhiteSpace(Password);
        }
    }
}
