using CommunityToolkit.Mvvm.ComponentModel;
using QuickTix.Core.Enums;

namespace QuickTix.Desktop.Models.Forms
{
    public partial class SubscriptionFormModel : ObservableObject
    {
        [ObservableProperty] private int venueId;
        [ObservableProperty] private SubscriptionCategory category;
        [ObservableProperty] private SubscriptionDuration duration;
        [ObservableProperty] private DateTime startDate = DateTime.Today;

        [ObservableProperty] private DateTime? endDate;
        [ObservableProperty] private decimal? price;
    }
}
