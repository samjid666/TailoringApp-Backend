using Tailoring.Core.Entities;
using Tailoring.Core.Enums;
using Tailoring.Core.Interfaces;

namespace Tailoring.Core.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Customer> _customerRepository;

        public OrderService(
            IRepository<Order> orderRepository,
            IRepository<Customer> customerRepository)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            // Validate customer exists
            var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
            if (customer == null)
                throw new ArgumentException("Customer not found");

            // Generate order number
            order.OrderNumber = GenerateOrderNumber();
            order.Status = OrderStatus.Pending;
            order.OrderDate = DateTime.UtcNow;

            return await _orderRepository.AddAsync(order);
        }

        public async Task<Order> UpdateOrderStatusAsync(int orderId, int status)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException("Order not found");

            if (!Enum.IsDefined(typeof(OrderStatus), status))
                throw new ArgumentException("Invalid order status");

            order.Status = (OrderStatus)status;
            await _orderRepository.UpdateAsync(order);

            return order;
        }

        public async Task<IReadOnlyList<Order>> GetPendingOrdersAsync()
        {
            return await _orderRepository.GetAsync(o =>
                o.Status != OrderStatus.Delivered &&
                o.Status != OrderStatus.Cancelled);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByCustomerAsync(int customerId)
        {
            return await _orderRepository.GetAsync(o => o.CustomerId == customerId);
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            return await _orderRepository.GetByIdAsync(orderId);
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString()[..8]}";
        }
    }
}