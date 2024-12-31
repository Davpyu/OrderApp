using System.ComponentModel.DataAnnotations;

namespace DotNetService.Domain.Order.Dtos
{
    public class OrderCreateDto
    {
        [Required]
        public Guid InventoryId { get; set; }

        [Required]
        public int Quantity { get; set; }

        public static Models.Order Assign(OrderCreateDto data)
        {
            Models.Order res = new()
            {
                InventoryId = data.InventoryId,
                Quantity = data.Quantity,
                Status = OrderStatus.Pending
            };

            return res;
        }
    }
}