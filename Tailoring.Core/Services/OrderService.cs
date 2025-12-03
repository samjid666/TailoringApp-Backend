using Tailoring.Core.DTOs;
using Tailoring.Core.Entities;
using Tailoring.Core.Enums;
using Tailoring.Core.Interfaces;

namespace Tailoring.Core.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<Measurement> _measurementRepository;

        public OrderService(
            IRepository<Order> orderRepository,
            IRepository<Customer> customerRepository,
            IRepository<Measurement> measurementRepository)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _measurementRepository = measurementRepository;
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

        public async Task<Order> CreateOrderFromDtoAsync(CreateOrderDto orderDto)
        {
            try
            {
                int customerId;

                // Either use existing customer or create new one
                if (orderDto.CustomerId.HasValue && orderDto.CustomerId.Value > 0)
                {
                    // Validate customer exists
                    var existingCustomer = await _customerRepository.GetByIdAsync(orderDto.CustomerId.Value);
                    if (existingCustomer == null)
                        throw new ArgumentException($"Customer with ID {orderDto.CustomerId.Value} not found");
                    customerId = orderDto.CustomerId.Value;
                }
                else
                {
                    // Create new customer from provided details
                    if (string.IsNullOrWhiteSpace(orderDto.CustomerFirstName) ||
                        string.IsNullOrWhiteSpace(orderDto.CustomerLastName) ||
                        string.IsNullOrWhiteSpace(orderDto.CustomerEmail))
                    {
                        throw new ArgumentException("Customer ID or customer details (FirstName, LastName, Email) are required");
                    }

                    var newCustomer = new Customer
                    {
                        FirstName = orderDto.CustomerFirstName,
                        LastName = orderDto.CustomerLastName,
                        Email = orderDto.CustomerEmail,
                        Phone = orderDto.CustomerPhone ?? string.Empty,
                        Address = string.Empty
                    };

                    var createdCustomer = await _customerRepository.AddAsync(newCustomer);
                    customerId = createdCustomer.Id;
                }

                // Validate DueDate
                if (orderDto.DueDate < DateTime.UtcNow.Date)
                {
                    throw new ArgumentException("Due date cannot be in the past");
                }

                // Create order
                var order = new Order
                {
                    CustomerId = customerId,
                    OrderNumber = GenerateOrderNumber(),
                    GarmentType = orderDto.GarmentType,
                    FabricType = orderDto.FabricType,
                    StyleDetails = orderDto.StyleDetails ?? string.Empty,
                    SpecialInstructions = orderDto.SpecialInstructions ?? string.Empty,
                    OrderDate = DateTime.UtcNow,
                    DueDate = orderDto.DueDate,
                    TotalAmount = orderDto.TotalAmount,
                    AdvancePaid = orderDto.AdvancePaid,
                    Status = OrderStatus.Pending,
                    Priority = orderDto.Priority
                };

                var createdOrder = await _orderRepository.AddAsync(order);

                // Create measurement if provided
                if (orderDto.Chest.HasValue || orderDto.Waist.HasValue || orderDto.Hip.HasValue ||
                    orderDto.Shoulder.HasValue || orderDto.ArmLength.HasValue || orderDto.Inseam.HasValue ||
                    orderDto.Neck.HasValue)
                {
                    var measurement = new Measurement
                    {
                        CustomerId = customerId,
                        MeasurementType = orderDto.GarmentType,
                        TakenOn = DateTime.UtcNow,
                        Chest = orderDto.Chest ?? 0,
                        Waist = orderDto.Waist ?? 0,
                        Hip = orderDto.Hip ?? 0,
                        Shoulder = orderDto.Shoulder ?? 0,
                        ArmLength = orderDto.ArmLength ?? 0,
                        Inseam = orderDto.Inseam ?? 0,
                        Neck = orderDto.Neck ?? 0,
                        AdditionalNotes = orderDto.MeasurementNotes
                    };

                    await _measurementRepository.AddAsync(measurement);
                }

                return createdOrder;
            }
            catch (Exception ex)
            {
                // Log and rethrow with more context
                throw new Exception($"Error creating order: {ex.Message}", ex);
            }
        }

        public async Task<Order> UpdateOrderAsync(int orderId, UpdateOrderDto updateDto)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException("Order not found");

            if (!string.IsNullOrEmpty(updateDto.GarmentType))
                order.GarmentType = updateDto.GarmentType;
            if (!string.IsNullOrEmpty(updateDto.FabricType))
                order.FabricType = updateDto.FabricType;
            if (!string.IsNullOrEmpty(updateDto.StyleDetails))
                order.StyleDetails = updateDto.StyleDetails;
            if (!string.IsNullOrEmpty(updateDto.SpecialInstructions))
                order.SpecialInstructions = updateDto.SpecialInstructions;
            if (updateDto.DueDate.HasValue)
                order.DueDate = updateDto.DueDate.Value;
            if (updateDto.TotalAmount.HasValue)
                order.TotalAmount = updateDto.TotalAmount.Value;
            if (updateDto.AdvancePaid.HasValue)
                order.AdvancePaid = updateDto.AdvancePaid.Value;
            if (updateDto.Status.HasValue)
                order.Status = updateDto.Status.Value;
            if (updateDto.Priority.HasValue)
                order.Priority = updateDto.Priority.Value;

            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order);

            return order;
        }

        public async Task DeleteOrderAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException("Order not found");

            await _orderRepository.DeleteAsync(order);
        }

        public async Task<PaginatedOrdersDto> GetOrdersAsync(int pageNumber, int pageSize, int? customerId = null, string? sortBy = "Priority")
        {
            // Get orders with customer data
            var allOrders = await _orderRepository.GetAsync(
                o => !customerId.HasValue || o.CustomerId == customerId.Value,
                o => o.Customer);

            // Sort orders
            var sortedOrders = sortBy?.ToLower() switch
            {
                "priority" => allOrders.OrderBy(o => o.Priority).ThenByDescending(o => o.OrderDate),
                "date" => allOrders.OrderByDescending(o => o.OrderDate),
                "duedate" => allOrders.OrderBy(o => o.DueDate),
                "status" => allOrders.OrderBy(o => o.Status),
                _ => allOrders.OrderBy(o => o.Priority).ThenByDescending(o => o.OrderDate)
            };

            var totalCount = sortedOrders.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var paginatedOrders = sortedOrders
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderListDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.Customer != null ? $"{o.Customer.FirstName} {o.Customer.LastName}" : "Unknown",
                    GarmentType = o.GarmentType,
                    FabricType = o.FabricType,
                    Status = o.Status,
                    OrderDate = o.OrderDate,
                    DueDate = o.DueDate,
                    TotalAmount = o.TotalAmount,
                    AdvancePaid = o.AdvancePaid,
                    Balance = o.TotalAmount - o.AdvancePaid,
                    Priority = o.Priority
                })
                .ToList();

            return new PaginatedOrdersDto
            {
                Orders = paginatedOrders,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            };
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
            return await _orderRepository.GetAsync(
                o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled,
                o => o.Customer);
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