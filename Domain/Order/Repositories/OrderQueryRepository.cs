using System.Linq.Expressions;
using DotNetService.Domain.Order.Dtos;
using DotNetService.Infrastructure.Dtos;
using DotNetService.Infrastructure.Databases;
using DotNetService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DotNetService.Domain.Order.Repositories
{
    public partial class OrderQueryRepository(
        IamDBContext context
        )
    {
        private readonly IamDBContext _context = context;

        public async Task<PaginationResult<Models.Order>> Pagination(OrderQueryDto queryParams)
        {
            int skip = (queryParams.Page - 1) * queryParams.PerPage;
            var query = _context.Orders
            .AsNoTracking()
            .AsQueryable();

            query = QuerySearch(query, queryParams);
            query = QueryFilter(query, queryParams);
            query = QuerySort(query, queryParams);

            var data = await query.Skip(skip).Take(queryParams.PerPage).ToListAsync();
            var count = await Count(query);

            return new PaginationResult<Models.Order>
            {
                Data = data,
                Count = count,
            };
        }

        private static IQueryable<Models.Order> QuerySearch(IQueryable<Models.Order> query, OrderQueryDto queryParams)
        {
            if (queryParams.Search != null)
            {
                query = query.Where(data =>
                    data.OrderNumber.Contains(queryParams.Search)
                );
            }

            return query;
        }

        private static IQueryable<Models.Order> QueryFilter(IQueryable<Models.Order> query, OrderQueryDto queryParams)
        {
            if (queryParams.Status != null)
            {
                query = query.Where(data => data.Status.Equals(queryParams.Status));
            }

            return query;
        }

        private static IQueryable<Models.Order> QuerySort(IQueryable<Models.Order> query, OrderQueryDto queryParams)
        {
            queryParams.SortBy ??= "updated_at";

            Dictionary<string, Expression<Func<Models.Order, object>>> sortFunctions = new()
            {
                { "updated_at", data => data.UpdatedAt! },
                { "created_at", data => data.CreatedAt! },
            };

            if (!sortFunctions.TryGetValue(queryParams.SortBy, out Expression<Func<Models.Order, object>> value))
            {
                throw new BadHttpRequestException($"Invalid sort column: {queryParams.SortBy}, available sort columns: {string.Join(", ", sortFunctions.Keys)}");
            }

            query = queryParams.Order == SortOrder.Asc
                ? query.OrderBy(value).AsQueryable()
                : query.OrderByDescending(value).AsQueryable();

            return query;
        }

        public async Task<int> Count(IQueryable<Models.Order> query)
        {
            return await query.Select(x => x.Id).CountAsync();
        }
    }

    public partial class OrderQueryRepository
    {

        public async Task<Models.Order> FindOneById(Guid id = default)
        {
            return await _context.Orders
                .Where(data => data.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsExistByUserAndInventory(Guid userId, Guid inventoryId)
        {
            return await _context.Orders.Where(order => order.UserId == userId && order.InventoryId == inventoryId).AnyAsync();
        }

        public async Task<int> GetCountTodayOrder()
        {
            return await _context.Orders.Where(data => data.CreatedAt.Value.Date == DateTime.UtcNow).CountAsync();
        }
    }
}