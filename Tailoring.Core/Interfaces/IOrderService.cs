using Tailoring.Core.Entities;

namespace Tailoring.Core.Interfaces
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<Order> UpdateOrderStatusAsync(int orderId, int status);
        Task<IReadOnlyList<Order>> GetPendingOrdersAsync();
        Task<IReadOnlyList<Order>> GetOrdersByCustomerAsync(int customerId);
        Task<Order?> GetOrderWithDetailsAsync(int orderId);
    }
}