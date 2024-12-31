using System.ComponentModel.DataAnnotations;

namespace DotNetService.Domain.Order.Dtos
{
    public class OrderUpdateDto
    {
        [Required]
        public int Quantity { get; set; }

        public static Models.Order Assign(OrderUpdateDto data)
        {
            Models.Order res = new()
            {
                Quantity = data.Quantity,
            };

            return res;
        }
    }
}