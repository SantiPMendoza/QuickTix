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

    }
}
