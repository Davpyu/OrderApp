using DotNetService.Constants.Logger;
using DotNetService.Domain.Order.Dtos;
using DotNetService.Domain.Order.Repositories;
using DotNetService.Infrastructure.Integrations.NATs;
using NATS.Client.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;

namespace DotNetService.Domain.Order.Listeners
{
    public class CheckInventoryEvent(
        NATsIntegration natConnection,
        ILoggerFactory loggerFactory,
        OrderStoreRepository orderStoreRepository
    )
    {
        public readonly ILogger _logger = loggerFactory.CreateLogger(LoggerConstant.ACTIVITY);
        private readonly NATsIntegration _natConnection = natConnection;
        private readonly OrderStoreRepository _orderStoreRepository = orderStoreRepository;

        public async Task Publish(OrderEventDto data)
        {
            _logger.LogInformation("Sending inventory.check event");

            var options = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

            // Define a retry policy with exponential backoff
            var retryPolicy = Policy
                .Handle<NatsException>() // Handle all exceptions (or specify specific exceptions)
                .WaitAndRetryAsync(
                    retryCount: 3, // Maximum number of retries
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (exception, retryCount, context) =>
                    {
                        _logger.LogError($"Retry {retryCount} due to: {exception.Message}");
                    });

            try
            {
                await retryPolicy.ExecuteAsync(async () =>
                {
                    var res = await _natConnection.PublishAndGetReply<string, IDictionary<string, object>>("inventory.check", JsonConvert.SerializeObject(data, options));

                    var approval = res.Where(x => x.Key == "approval").First().Value;

                    if (approval.ToString() == "Approved")
                    {
                        // update to Confirmed order
                        Models.Order updateData = new()
                        {
                            Status = OrderStatus.Confirmed,
                        };

                        await _orderStoreRepository.Update(data.Id, updateData);
                        _logger.LogInformation("Event inventory.check has gotten a reply and order has been updated to Confirmed");
                    }

                    else
                    {
                        // update to Rejected order
                        Models.Order updateData = new()
                        {
                            Status = OrderStatus.Rejected,
                        };

                        await _orderStoreRepository.Update(data.Id, updateData);
                        _logger.LogInformation("Event inventory.check has gotten a reply and order has been updated to Rejected");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Operation failed because exceeded maximum retries: {ex.Message}");
                throw;
            }
        }
    }
}
