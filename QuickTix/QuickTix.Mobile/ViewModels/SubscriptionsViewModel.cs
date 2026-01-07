using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using QuickTix.Contracts.Enums;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Mobile.Helpers;
using QuickTix.Mobile.Services;

namespace QuickTix.Mobile.ViewModels;

public partial class SubscriptionsViewModel : ObservableObject
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAppSession _session;

    public ObservableCollection<SubscriptionCardViewModel> Subscriptions { get; } = new();

    [ObservableProperty] private SubscriptionCardViewModel? selectedSubscription;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    public SubscriptionsViewModel(ISubscriptionService subscriptionService, IAppSession session)
    {
        _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var clientId = _session.ClientId;
            if (clientId <= 0)
                throw new InvalidOperationException("ClientId no disponible en sesión. Revisa los claims del JWT.");

            var data = await _subscriptionService.GetByClientAsync(clientId);

            var cards = data
                .OrderByDescending(x => IsActive(x))
                .ThenByDescending(x => x.EndDate)
                .Select(MapToCard)
                .ToList();

            Subscriptions.Clear();
            foreach (var c in cards)
                Subscriptions.Add(c);

            SelectedSubscription = Subscriptions.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error cargando abonos: {ex.Message}";
            Subscriptions.Clear();
            SelectedSubscription = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool IsActive(SubscriptionDTO s)
        => DateTime.UtcNow.Date <= s.EndDate.Date;

    private SubscriptionCardViewModel MapToCard(SubscriptionDTO s)
    {
        var expired = DateTime.UtcNow.Date > s.EndDate.Date;

        return new SubscriptionCardViewModel
        {
            ClientName = _session.Email ?? $"Cliente #{_session.ClientId}",
            SubscriptionTitle = DurationToTitle(s.Duration),
            SubscriptionSubtitle = $"Categoría: {CategoryToText(s.Category)} · VenueId: {s.VenueId} · {s.Price:0.##}€",
            ValidityText = $"Inicio: {s.StartDate:dd/MM/yyyy} · Fin: {s.EndDate:dd/MM/yyyy}",
            ReferenceText = $"Ref: SUB-{s.Id:D6}",
            IsExpired = expired,
            ThemeColor = DurationToColor(s.Duration)
        };
    }

    private static string DurationToTitle(SubscriptionDuration duration) => duration switch
    {
        SubscriptionDuration.Quincenal => "QUINCENAL",
        SubscriptionDuration.Mensual => "MENSUAL",
        SubscriptionDuration.Temporada => "TEMPORADA",
        _ => "ABONO"
    };

    private static string CategoryToText(SubscriptionCategory category) => category switch
    {
        SubscriptionCategory.Niño => "Niño",
        SubscriptionCategory.Adulto => "Adulto",
        SubscriptionCategory.Jubilado => "Jubilado",
        SubscriptionCategory.FamiliaNumerosa => "Familia numerosa",
        _ => category.ToString()
    };

    private static Color DurationToColor(SubscriptionDuration duration) => duration switch
    {
        SubscriptionDuration.Quincenal => Color.FromArgb("#6D28D9"),
        SubscriptionDuration.Mensual => Color.FromArgb("#F97316"),
        SubscriptionDuration.Temporada => Color.FromArgb("#2563EB"),
        _ => Colors.DodgerBlue
    };
}
