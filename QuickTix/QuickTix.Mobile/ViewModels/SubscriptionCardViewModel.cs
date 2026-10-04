using CommunityToolkit.Mvvm.ComponentModel;


namespace QuickTix.Mobile.ViewModels;
public partial class SubscriptionCardViewModel : ObservableObject
{
    [ObservableProperty] private string clientName = string.Empty;
    [ObservableProperty] private string subscriptionTitle = string.Empty;
    [ObservableProperty] private string subscriptionSubtitle = string.Empty;
    [ObservableProperty] private string validityText = string.Empty;
    [ObservableProperty] private string referenceText = string.Empty;

    [ObservableProperty] private bool isExpired;

    // Clave estable de categoría (sin acentos) para los DataTriggers de la
    // tarjeta: "Adulto" | "Nino" | "Jubilado" | "FamiliaNumerosa".
    [ObservableProperty] private string categoryKey = "Adulto";

    [ObservableProperty] private Color themeColor = Colors.DodgerBlue;

    public string StatusText => IsExpired ? "CADUCADA" : "ACTIVA";

    public Color CardColor => IsExpired ? Color.FromArgb("#BDBDBD") : ThemeColor;

    partial void OnIsExpiredChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(CardColor));
    }

    partial void OnThemeColorChanged(Color value)
    {
        OnPropertyChanged(nameof(CardColor));
    }
}