using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using QuickTix.Contracts.Enums;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Mobile.Helpers;
using QuickTix.Mobile.Services;
using QuickTix.Mobile.Views;

namespace QuickTix.Mobile.ViewModels
{
    /// <summary>
    /// ViewModel de abonos (suscripciones) para Mobile.
    /// Carga las suscripciones del cliente en sesión, las transforma a tarjetas de UI y expone logout.
    /// </summary>
    public partial class SubscriptionsViewModel : ObservableObject
    {
        public ObservableCollection<SubscriptionCardViewModel> Subscriptions { get; } = new();

        [ObservableProperty] private SubscriptionCardViewModel? selectedSubscription;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;

        private readonly ISubscriptionService _subscriptionService;
        private readonly IAppSession _session;
        private readonly IAuthService _authService;
        private readonly IServiceProvider _services;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="SubscriptionsViewModel"/>.
        /// </summary>
        /// <param name="subscriptionService">Servicio de suscripciones.</param>
        /// <param name="session">Sesión de aplicación (ids/claims del JWT).</param>
        /// <param name="authService">Servicio de autenticación.</param>
        /// <param name="services">Proveedor de servicios para resolver páginas.</param>
        public SubscriptionsViewModel(
            ISubscriptionService subscriptionService,
            IAppSession session,
            IAuthService authService,
            IServiceProvider services)
        {
            _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _services = services;
        }

        /// <summary>
        /// Carga las suscripciones del cliente actual y construye el listado de tarjetas para UI.
        /// Ordena por vigencia y fecha de fin, y selecciona la primera tarjeta como predeterminada.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var clientId = _session.ClientId;
                if (clientId <= 0)
                    throw new InvalidOperationException("ClientId no disponible en sesión. Revisa los claims del JWT.");

                var data = await _subscriptionService.GetByClientAsync(clientId);

                var cards = data
                    .OrderByDescending(IsActive)
                    .ThenByDescending(x => x.EndDate)
                    .Select(MapToCard)
                    .ToList();

                Subscriptions.Clear();
                foreach (var c in cards)
                    Subscriptions.Add(c);

                SelectedSubscription = Subscriptions.FirstOrDefault();
            }
            catch (ApiException apiEx)
            {
                ErrorMessage = $"Error cargando abonos: {apiEx.Message}";
                Subscriptions.Clear();
                SelectedSubscription = null;
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

        /// <summary>
        /// Mapea una <see cref="SubscriptionDTO"/> a una tarjeta de UI.
        /// </summary>
        /// <param name="s">Suscripción a representar.</param>
        /// <returns>ViewModel de tarjeta.</returns>
        private SubscriptionCardViewModel MapToCard(SubscriptionDTO s)
        {
            var expired = DateTime.UtcNow.Date > s.EndDate.Date;

            return new SubscriptionCardViewModel
            {
                ClientName = _session.Name ?? $"Cliente #{_session.ClientId}",
                SubscriptionTitle = DurationToTitle(s.Duration),
                SubscriptionSubtitle = $"Categoría: {CategoryToText(s.Category)} · Recinto: {s.VenueName}",
                ValidityText = $"Inicio: {s.StartDate:dd/MM/yyyy} · Fin: {s.EndDate:dd/MM/yyyy}",
                ReferenceText = $"Ref: SUB-{s.Id:D6}",
                IsExpired = expired,
                ThemeColor = DurationToColor(s.Duration),
                CategoryKey = CategoryToKey(s.Category)
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

        /// <summary>
        /// Clave estable (ASCII, sin acentos) que consumen los DataTriggers de la tarjeta
        /// para elegir el degradado por categoría — mismo mapeo que los chips de Desktop.
        /// </summary>
        /// <param name="category">Categoría del abono.</param>
        /// <returns>Clave de categoría para la UI.</returns>
        private static string CategoryToKey(SubscriptionCategory category) => category switch
        {
            SubscriptionCategory.Niño => "Nino",
            SubscriptionCategory.Adulto => "Adulto",
            SubscriptionCategory.Jubilado => "Jubilado",
            SubscriptionCategory.FamiliaNumerosa => "FamiliaNumerosa",
            _ => "Adulto"
        };

        private static Color DurationToColor(SubscriptionDuration duration) => duration switch
        {
            SubscriptionDuration.Quincenal => Color.FromArgb("#6D28D9"),
            SubscriptionDuration.Mensual => Color.FromArgb("#F97316"),
            SubscriptionDuration.Temporada => Color.FromArgb("#2563EB"),
            _ => Colors.DodgerBlue
        };

        /// <summary>
        /// Cierra la sesión actual y redirige al login.
        /// </summary>
        /// <returns>Tarea completada.</returns>
        [RelayCommand]
        private Task LogoutAsync()
        {
            _authService.Logout();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var loginPage = _services.GetRequiredService<LoginPage>();
                App.Current.MainPage = new NavigationPage(loginPage);
            });

            return Task.CompletedTask;
        }
    }
}
