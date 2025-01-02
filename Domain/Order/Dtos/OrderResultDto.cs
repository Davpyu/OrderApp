using DotNetService.Domain.Permission.Dtos;

namespace DotNetService.Domain.Order.Dtos
{
    public class OrderResultDto : Models.Order
    {

        public OrderResultDto(Models.Order order)
        {
            Id = order.Id;
            InventoryId = order.InventoryId;
            Quantity = order.Quantity;
            OrderNumber = order.OrderNumber;
            UserId = order.UserId;
            Status = order.Status;
            CreatedAt = order.CreatedAt;
            UpdatedAt = order.UpdatedAt;
        }

        public static List<OrderResultDto> MapRepo(List<Models.Order> data)
        {
            return data?.Select(data => new OrderResultDto(data)).ToList();
        }
    }
}