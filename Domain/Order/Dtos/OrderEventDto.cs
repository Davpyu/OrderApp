namespace DotNetService.Domain.Order.Dtos
{
    public class OrderEventDto
    {
        public Guid Id { get; set;}
        public Guid InventoryId { get; set; }
        public int Quantity { get; set; }

        public OrderEventDto(Models.Order order, Guid id)
        {
            Id = id;
            InventoryId = order.InventoryId;
            Quantity = order.Quantity;
        }
    }
}