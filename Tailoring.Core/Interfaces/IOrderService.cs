using Tailoring.Core.DTOs;
using Tailoring.Core.Entities;

namespace Tailoring.Core.Interfaces
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<Order> CreateOrderFromDtoAsync(CreateOrderDto orderDto);
        Task<Order> UpdateOrderAsync(int orderId, UpdateOrderDto updateDto);
        Task<Order> UpdateOrderStatusAsync(int orderId, int status);
        Task DeleteOrderAsync(int orderId);
        Task<PaginatedOrdersDto> GetOrdersAsync(int pageNumber, int pageSize, int? customerId = null, string? sortBy = "Priority");
        Task<IReadOnlyList<Order>> GetPendingOrdersAsync();
        Task<IReadOnlyList<Order>> GetOrdersByCustomerAsync(int customerId);
        Task<Order?> GetOrderWithDetailsAsync(int orderId);
    }
}