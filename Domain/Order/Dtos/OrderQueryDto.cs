

using DotNetService.Infrastructure.Dtos;

namespace DotNetService.Domain.Order.Dtos
{
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Rejected,
        Shipping,
        Shipped,
        Completed
    }
    public class OrderQueryDto : QueryDto
    {
        public OrderStatus? Status { get; set; }
    }
}