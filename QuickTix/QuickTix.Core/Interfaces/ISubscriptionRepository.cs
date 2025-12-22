using QuickTix.Core.Models.Entities;

namespace QuickTix.Core.Interfaces
{
    public interface ISubscriptionRepository : IRepository<Subscription> {

        Task<ICollection<Subscription>> GetByClientAsync(int clientId);
    }
}
