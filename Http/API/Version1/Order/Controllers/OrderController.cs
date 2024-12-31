using Microsoft.AspNetCore.Mvc;
using System.Net;
using DotNetService.Domain.Order.Services;
using DotNetService.Infrastructure.Attributes;
using DotNetService.Constants.Permission;
using DotNetService.Domain.Order.Dtos;
using DotNetService.Infrastructure.Helpers;

namespace DotNetService.Http.API.Version1.Order.Controllers
{
    [Route("api/v1/orders")]
    [ApiController]

    public class OrderController(
        OrderService orderService
        ) : ControllerBase
    {
        private readonly OrderService _orderService = orderService;

        [HttpGet()]
        [Permissions(PermissionConstant.ORDER_VIEW)]
        public async Task<ApiResponse> Index([FromQuery] OrderQueryDto query)
        {
            var paginationResult = await _orderService.Index(query);
            return new ApiResponsePagination<OrderResultDto>(HttpStatusCode.OK, paginationResult);
        }

        [HttpGet("{id}")]
        [Permissions(PermissionConstant.ORDER_VIEW)]
        public async Task<ApiResponse> Show(Guid id)
        {
            Models.Order data = await _orderService.Detail(id);
            return new ApiResponseData<Models.Order>(HttpStatusCode.OK, data);
        }

        [HttpPost()]
        [Permissions(PermissionConstant.ORDER_CREATE)]
        public async Task<ApiResponse> Store(OrderCreateDto dataCreate)
        {
            await _orderService.Create(dataCreate);
            return new ApiResponseData<Models.Order>(HttpStatusCode.OK, null);
        }

        [HttpPut("{id}")]
        [Permissions(PermissionConstant.ORDER_UPDATE)]
        public async Task<ApiResponse> Update(Guid id, OrderUpdateDto dataUpdate)
        {
            await _orderService.Update(id, dataUpdate);
            return new ApiResponseData<Models.Order>(HttpStatusCode.OK, null);
        }

        [HttpDelete("{id}")]
        [Permissions(PermissionConstant.ORDER_DELETE)]
        public async Task<ApiResponse> Delete(Guid id)
        {
            await _orderService.Delete(id);
            return new ApiResponseData<Models.Order>(HttpStatusCode.OK, null);
        }
    }
}
