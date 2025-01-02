using System.ComponentModel.DataAnnotations;

namespace DotNetService.Domain.Order.Dtos
{
    public class OrderUpdateDto
    {
        [Required]
        public OrderStatus Status { get; set; }

        public static Models.Order Assign(OrderUpdateDto data)
        {
            Models.Order res = new()
            {
                Status = data.Status,
            };

            return res;
        }
    }
}