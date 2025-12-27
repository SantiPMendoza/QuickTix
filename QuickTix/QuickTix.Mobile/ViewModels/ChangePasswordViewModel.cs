using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Mobile.Services;

namespace QuickTix.Mobile.ViewModels;

public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    // Guardamos el rol para continuar a la Shell correcta tras el cambio
    private readonly string _roleToContinue;

    [ObservableProperty]
    private string currentPassword = string.Empty;

    [ObservableProperty]
    private string newPassword = string.Empty;

    [ObservableProperty]
    private string confirmNewPassword = string.Empty;

    [ObservableProperty]
    private bool isSubmitEnabled;

    [ObservableProperty]
    private bool isBusy;

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

    private void Validate()
    {
        IsSubmitEnabled =
            !string.IsNullOrWhiteSpace(CurrentPassword) &&
            !string.IsNullOrWhiteSpace(NewPassword) &&
            !string.IsNullOrWhiteSpace(ConfirmNewPassword) &&
            NewPassword == ConfirmNewPassword &&
            NewPassword.Length >= 6;
    }

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

            // Actualizar estado local
            var user = _authService.GetCurrentUser();
            if (user is not null)
                user.MustChangePassword = false;

            // Continuar a la Shell por rol
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
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
