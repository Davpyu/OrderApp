using DotNetService.Domain.Order.Dtos;
using DotNetService.Domain.Order.Repositories;
using DotNetService.Infrastructure.Integrations.NATs;

namespace DotNetService.Domain.Order.Listeners
{
    public class CheckInventoryEvent(
        NATsIntegration natConnection,
        OrderStoreRepository orderStoreRepository
    )
    {
        private readonly NATsIntegration _natConnection = natConnection;
        private readonly OrderStoreRepository _orderStoreRepository = orderStoreRepository;

        public async Task Publish(Models.Order data)
        {
            var res = await _natConnection.PublishAndGetReply<Models.Order, IDictionary<string, object>>("inventory.check", data);
            if (res.TryGetValue("approval", out var approval))
            {
                if (approval.ToString() == "accepted")
                {
                    // update to Confirmed order
                    Models.Order updateData = new()
                    {
                        Status = OrderStatus.Confirmed,
                    };

                    await _orderStoreRepository.Update(data.Id, updateData);
                }

                else
                {
                    // update to Rejected order
                    Models.Order updateData = new()
                    {
                        Status = OrderStatus.Rejected,
                    };

                    await _orderStoreRepository.Update(data.Id, updateData);
                }
            }
        }
    }
}
