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

        [ObservableProperty]
        private bool isBusy;

        // ------------------------------
        // COMANDO DE LOGIN
        // ------------------------------
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

                // Preferencias (esto no es UI, pero está bien aquí)
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
                        Application.Current.MainPage.DisplayAlert("Error", "No se pudo obtener la información del usuario.", "OK"));
                    return;
                }

                if (user.MustChangePassword)
                {
                    var page = _services.GetRequiredService<ChangePasswordPage>();

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        // Importante: asegúrate de que existe NavigationPage o navegación válida
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
                            // Ojo: aquí estás dentro de InvokeOnMainThreadAsync; si quieres alert, hazlo fuera o con Dispatcher async.
                            App.Current.MainPage.DisplayAlert("Error", $"Rol no soportado: {user.Role ?? "(sin rol)"}", "OK");
                            break;
                    }
                });
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
