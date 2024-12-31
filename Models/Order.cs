using DotNetService.Domain.Order.Dtos;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotNetService.Models
{
    public class Order : BaseModel
    {
        [Required]
        public string OrderNumber { get; set; }

        [Required]
        public Guid InventoryId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        [Required]
        public OrderStatus Status { get; set; }
    }
}