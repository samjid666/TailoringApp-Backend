using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tailoring.Core.DTOs;
using Tailoring.Core.Entities;
using Tailoring.Core.Interfaces;

namespace Tailoring.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderService orderService,
            ILogger<OrdersController> _logger)
        {
            _orderService = orderService;
            this._logger = _logger;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedOrdersDto>> GetOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? customerId = null,
            [FromQuery] string sortBy = "Priority")
        {
            try
            {
                var orders = await _orderService.GetOrdersAsync(pageNumber, pageSize, customerId, sortBy);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            try
            {
                var order = await _orderService.GetOrderWithDetailsAsync(id);
                if (order == null)
                    return NotFound();

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Order>> CreateOrder(CreateOrderDto orderDto)
        {
            try
            {
                _logger.LogInformation("Creating order for customer {CustomerId} with garment type {GarmentType}",
                    orderDto.CustomerId, orderDto.GarmentType);

                var createdOrder = await _orderService.CreateOrderFromDtoAsync(orderDto);

                _logger.LogInformation("Successfully created order {OrderId} with order number {OrderNumber}",
                    createdOrder.Id, createdOrder.OrderNumber);

                return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, createdOrder);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating order: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order. Details: {Message}, InnerException: {InnerException}",
                    ex.Message, ex.InnerException?.Message);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Order>> UpdateOrder(int id, UpdateOrderDto updateDto)
        {
            try
            {
                var order = await _orderService.UpdateOrderAsync(id, updateDto);
                return Ok(order);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteOrder(int id)
        {
            try
            {
                await _orderService.DeleteOrderAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<Order>> UpdateOrderStatus(int id, [FromBody] int status)
        {
            try
            {
                var order = await _orderService.UpdateOrderStatusAsync(id, status);
                return Ok(order);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for {OrderId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}