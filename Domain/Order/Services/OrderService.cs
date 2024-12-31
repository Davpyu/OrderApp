using DotNetService.Domain.Auth.Services;
using DotNetService.Domain.Order.Dtos;
using DotNetService.Domain.Order.Listeners;
using DotNetService.Domain.Order.Messages;
using DotNetService.Domain.Order.Repositories;
using DotNetService.Infrastructure.Dtos;
using DotNetService.Infrastructure.Exceptions;

namespace DotNetService.Domain.Order.Services
{
    public class OrderService(
        AuthService authService,
        CheckInventoryEvent checkInventoryEvent,
        OrderStoreRepository orderStoreRepository,
        OrderQueryRepository orderQueryRepository
    )
    {
        private readonly AuthService _authService = authService;
        private readonly CheckInventoryEvent _checkInventoryEvent = checkInventoryEvent;
        private readonly OrderStoreRepository _orderStoreRepository = orderStoreRepository;
        private readonly OrderQueryRepository _orderQueryRepository = orderQueryRepository;

        public async Task<PaginationModel<OrderResultDto>> Index(OrderQueryDto query = null)
        {
            var data = await _orderQueryRepository.Pagination(query);
            var formatedData = OrderResultDto.MapRepo(data.Data);
            var paginate = PaginationModel<OrderResultDto>.Parse(formatedData, data.Count, query);
            return paginate;
        }

        public async Task Create(OrderCreateDto dataCreate)
        {
            var user = await _authService.Account();
            var isOrderExist = await _orderQueryRepository.IsExistByUserAndInventory(user.Id, dataCreate.InventoryId);

            if (isOrderExist)
            {
                throw new UnprocessableEntityException(OrderErrorMessage.ErrOrderAlreadyExist);
            }

            var todayCount = await _orderQueryRepository.GetCountTodayOrder();
            todayCount += 1;
            var orderNumber = DateTime.UtcNow.ToString("yyyyMMdd") + todayCount.ToString().PadLeft(4, '0');

            var data = OrderCreateDto.Assign(dataCreate);

            await _orderStoreRepository.Create(data, user.Id, orderNumber);

            await _checkInventoryEvent.Publish(data);
        }

        public async Task<OrderResultDto> Detail(Guid id)
        {
            var order = await _orderQueryRepository.FindOneById(id);
            if (order == null)
            {
                throw new DataNotFoundException(OrderErrorMessage.ErrOrderNotFound);
            }

            return new OrderResultDto(order);
        }

        public async Task Update(Guid id, OrderUpdateDto dataUpdate)
        {
            var data = OrderUpdateDto.Assign(dataUpdate);
            await _orderStoreRepository.Update(id, data);
        }

        public async Task Delete(Guid id)
        {
            await _orderStoreRepository.Delete(id);
        }
    }
}