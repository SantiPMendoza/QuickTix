using QuickTix.Core.Models.DTOs;
using QuickTix.Desktop.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Desktop.ViewModels
{
    public partial class SubscriptionsViewModel : BaseCrudViewModel<SubscriptionDTO, CreateSubscriptionDTO>
    {
        protected override string Endpoint => "Subscription";

        public SubscriptionsViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
            _ = LoadAsync();
        }
    }
}
