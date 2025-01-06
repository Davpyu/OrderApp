using DotNetService.Constants.Event;
using DotNetService.Constants.Logger;
using DotNetService.Domain.Order.Dtos;
using DotNetService.Domain.Order.Repositories;
using DotNetService.Infrastructure.Integrations.NATs;
using DotNetService.Infrastructure.Shareds;
using NATS.Client.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;

namespace DotNetService.Domain.Order.Listeners
{
    public enum QuantityStatus
    {
        Approved,
        Rejected
    }

    public class CheckInventoryEvent(
        NATsIntegration natsIntegration,
        ILoggerFactory loggerFactory,
        OrderStoreRepository orderStoreRepository
    )
    {
        public readonly ILogger _logger = loggerFactory.CreateLogger(LoggerConstant.ACTIVITY);
        private readonly NATsIntegration _natsIntegration = natsIntegration;
        private readonly OrderStoreRepository _orderStoreRepository = orderStoreRepository;

        public async Task Publish(OrderEventDto data)
        {
            var jsonData = Utils.JsonSerialize(data);
            var subject = _natsIntegration.Subject(
                NATsEventModuleEnum.INVENTORY,
                NATsEventActionEnum.CHECK,
                NATsEventStatusEnum.REQUEST
            );
            _logger.LogInformation("Sending {name} event started with data {jsonData}", subject, jsonData);

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
                    var res = await _natsIntegration.PublishAndGetReply<string, object>(subject, jsonData);

                    var jsonResult = Utils.JsonSerialize(res);
                    _logger.LogInformation($"Getting a reply with data {jsonResult}");

                    var result = Utils.JsonDeserialize<Dictionary<string, string>>(jsonResult);

                    var approval = result.Where(x => x.Key == "status").First().Value;

                    if (Enum.Parse<QuantityStatus>(approval) == QuantityStatus.Approved)
                    {
                        // update to Confirmed order
                        Models.Order updateData = new()
                        {
                            Status = OrderStatus.Confirmed,
                        };

                        await _orderStoreRepository.Update(data.Id, updateData);
                        _logger.LogInformation("Event {subject} has gotten a reply and order has been updated to Confirmed", subject);
                    }

                    else
                    {
                        // update to Rejected order
                        Models.Order updateData = new()
                        {
                            Status = OrderStatus.Rejected,
                        };

                        await _orderStoreRepository.Update(data.Id, updateData);
                        _logger.LogInformation("Event {subject} has gotten a reply and order has been updated to Rejected", subject);
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
