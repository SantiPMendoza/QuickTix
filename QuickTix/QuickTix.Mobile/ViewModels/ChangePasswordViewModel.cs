using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Mobile.Services;

namespace QuickTix.Mobile.ViewModels
{
    /// <summary>
    /// ViewModel para el cambio de contraseña en Mobile.
    /// Valida los campos del formulario, ejecuta la operación de cambio y redirige a la Shell correspondiente según rol.
    /// </summary>
    public partial class ChangePasswordViewModel : ObservableObject
    {
        private readonly IAuthService _authService;

        // Rol capturado al entrar para decidir a qué Shell volver tras el cambio
        private readonly string _roleToContinue;

        [ObservableProperty] private string currentPassword = string.Empty;
        [ObservableProperty] private string newPassword = string.Empty;
        [ObservableProperty] private string confirmNewPassword = string.Empty;

        [ObservableProperty] private bool isSubmitEnabled;
        [ObservableProperty] private bool isBusy;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ChangePasswordViewModel"/>.
        /// Obtiene el rol del usuario actual y ejecuta una validación inicial.
        /// </summary>
        /// <param name="authService">Servicio de autenticación.</param>
        public ChangePasswordViewModel(IAuthService authService)
        {
            _authService = authService;

            var user = _authService.GetCurrentUser();
            _roleToContinue = (user?.Role ?? string.Empty).Trim().ToLowerInvariant();

            Validate();
        }

        partial void OnCurrentPasswordChanged(string value) => Validate();
        partial void OnNewPasswordChanged(string value) => Validate();
        partial void OnConfirmNewPasswordChanged(string value) => Validate();

        /// <summary>
        /// Aplica reglas de validación del formulario y habilita/deshabilita el envío.
        /// </summary>
        private void Validate()
        {
            IsSubmitEnabled =
                !string.IsNullOrWhiteSpace(CurrentPassword) &&
                !string.IsNullOrWhiteSpace(NewPassword) &&
                !string.IsNullOrWhiteSpace(ConfirmNewPassword) &&
                NewPassword == ConfirmNewPassword &&
                NewPassword.Length >= 6;
        }

        /// <summary>
        /// Ejecuta el cambio de contraseña.
        /// Muestra mensajes de validación, controla el estado busy y redirige a la Shell por rol.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task ConfirmChangePasswordAsync()
        {
            if (!IsSubmitEnabled)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Aviso",
                    "Revisa los campos. La nueva contraseña debe coincidir y tener al menos 6 caracteres.",
                    "OK");

                return;
            }

            try
            {
                IsBusy = true;

                var result = await _authService.ChangePasswordAsync(CurrentPassword, NewPassword);
                if (!result)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        "No se pudo cambiar la contraseña. Verifica la contraseña actual.",
                        "OK");

                    return;
                }

                var user = _authService.GetCurrentUser();
                if (user is not null)
                    user.MustChangePassword = false;

                switch (_roleToContinue)
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
                            $"Rol no soportado: '{_roleToContinue}'",
                            "OK");
                        break;
                }
            }
            catch (ApiException apiEx)
            {
                // Errores controlados desde la API (ApiResponse + ApiException)
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    apiEx.Message,
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.ToString(), "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
