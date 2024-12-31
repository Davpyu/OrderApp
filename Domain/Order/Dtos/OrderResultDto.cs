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
            Status = order.Status;
        }

        public static List<OrderResultDto> MapRepo(List<Models.Order> data)
        {
            return data?.Select(data => new OrderResultDto(data)).ToList();
        }
    }
}