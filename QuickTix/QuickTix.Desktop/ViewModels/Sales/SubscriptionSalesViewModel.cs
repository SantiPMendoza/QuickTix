
using QuickTix.Contracts.DTOs.SaleDTOs.Subscription;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Desktop.ViewModels.Base;


namespace QuickTix.Desktop.ViewModels.Sales
{
    /// <summary>
    /// ViewModel responsable del histórico de ventas de suscripciones.
    /// Consume endpoints específicos de consulta (no CRUD estándar).
    /// </summary>
    public partial class SubscriptionSalesViewModel
        : SalesHistoryViewModelBase<SubscriptionSaleDTO>
    {
        /// <summary>
        /// Listado de managers disponible para la vista
        /// (filtros, contexto o visualización).
        /// </summary>
        [ObservableProperty] private ObservableCollection<ManagerDTO> managers = [];

        /// <summary>
        /// Manager actualmente seleccionado en la UI.
        /// </summary>
        [ObservableProperty] private ManagerDTO? selectedManager;

        /// <summary>
        /// Recurso lógico asociado a este ViewModel.
        /// Se mantiene por coherencia aunque no se use CRUD base.
        /// </summary>
        protected override string Endpoint => "Sale";

        /// <summary>
        /// Ruta del listado del histórico de suscripciones.
        /// Sobrescribe el comportamiento CRUD por defecto.
        /// </summary>
        protected override string ListRoute => ApiRoutes.Sale.HistorySubscriptions;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="SubscriptionSalesViewModel"/>.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para consumo de la API.</param>
        /// <param name="authService">Servicio de autenticación (rol de la sesión).</param>
        public SubscriptionSalesViewModel(HttpJsonClient httpClient, IAuthService authService)
            : base(httpClient, authService)
        {
            _ = LoadAsync();
        }

        /// <inheritdoc />
        protected override int GetSaleId(SubscriptionSaleDTO item) => item.Id;

        /// <inheritdoc />
        protected override bool GetIsVoided(SubscriptionSaleDTO item) => item.IsVoided;
    }
}
