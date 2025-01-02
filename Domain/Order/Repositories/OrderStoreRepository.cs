using DotNetService.Infrastructure.Databases;
using DotNetService.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;
using DbDeleteConcurrencyException = Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException;

namespace DotNetService.Domain.Order.Repositories
{
    public class OrderStoreRepository(
        IamDBContext context
    )
    {
        private readonly IamDBContext _context = context;

        public async Task<Guid> Create(Models.Order data, Guid userId, string orderNumber)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var id = Guid.NewGuid();

            try
            {
                var newOrder = await _context.Orders.AddAsync(new Models.Order
                {
                    Id = id,
                    InventoryId = data.InventoryId,
                    Quantity = data.Quantity,
                    UserId = userId,
                    OrderNumber = orderNumber,
                    Status = data.Status
                });
                var createdOrder = newOrder.Entity;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await _context.Database.RollbackTransactionAsync();
                throw;
            }
            finally
            {
                // to remove current tracking on orders
                _context.ChangeTracker.Clear();
            }

            return id;
        }

        public async Task Update(Guid id, Models.Order newData)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Update the order
                var data = await _context.Orders.Where(data => data.Id == id).FirstOrDefaultAsync();
                data.Status = newData.Status;
                _context.Orders.Update(data);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await _context.Database.RollbackTransactionAsync();
                throw new UnprocessableEntityException("No data was updated.");
            }
            catch (Exception)
            {
                await _context.Database.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task Delete(Guid id)
        {
            try
            {
                Models.Order data = new() { Id = id };
                _context.Orders.Attach(data);
                _context.Orders.Remove(data);
                await _context.SaveChangesAsync();
            }
            catch (DbDeleteConcurrencyException)
            {
                throw new UnprocessableEntityException("No data was deleted.");
            }
        }
    }
}