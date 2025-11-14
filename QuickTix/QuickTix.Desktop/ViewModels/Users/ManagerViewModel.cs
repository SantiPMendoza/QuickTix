using QuickTix.Core.Models.Entities;
using QuickTix.Desktop.Models.DTOs;
using QuickTix.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;

namespace QuickTix.Desktop.ViewModels.Users
{
    public partial class ManagerViewModel : BaseCrudViewModel<ManagerDTO, CreateManagerDTO>
    {
        protected override string Endpoint => "Manager";

        [ObservableProperty] private ObservableCollection<VenueDTO> venues = [];
        [ObservableProperty] private VenueDTO? selectedVenue;

        public ManagerViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
            _ = LoadAsync();
        }

        public async Task LoadVenuesAsync()
        {
            try
            {
                Venues = new ObservableCollection<VenueDTO>(
                    await _httpClient.GetListAsync<VenueDTO>("api/Venue"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando recintos: {ex.Message}");
            }
        }


    }
}
