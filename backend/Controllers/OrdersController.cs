// Controllers/OrdersController.cs
using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.DTOs.Orders;
using FoodDeliveryAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public OrdersController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            // Validate customer exists
            var customer = await _db.Users.FindAsync(dto.CustomerId);
            if (customer == null)
                return BadRequest(new { message = "Customer not found" });

            // Validate restaurant exists
            var restaurant = await _db.Restaurants.FindAsync(dto.RestaurantId);
            if (restaurant == null)
                return BadRequest(new { message = "Restaurant not found" });

            // Validate menu items
            var order = new Order
            {
                CustomerId = dto.CustomerId,
                RestaurantId = dto.RestaurantId,
                DeliveryAddress = dto.DeliveryAddress,
                Status = "Pending",
                PaymentStatus = "Unpaid",
                CreatedAt = DateTime.UtcNow
            };

            decimal total = 0;
            foreach (var item in dto.Items)
            {
                var menuItem = await _db.MenuItems
                    .Include(m => m.Restaurant)
                    .FirstOrDefaultAsync(m => m.Id == item.MenuItemId && m.RestaurantId == dto.RestaurantId);

                if (menuItem == null)
                    return BadRequest(new { message = $"MenuItem with ID {item.MenuItemId} not found in this restaurant" });

                if (item.Quantity <= 0)
                    return BadRequest(new { message = $"Quantity for item {menuItem.Name} must be greater than 0" });

                total += menuItem.Price * item.Quantity;

                order.Items.Add(new OrderItem
                {
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    UnitPrice = menuItem.Price
                });
            }

            if (order.Items.Count == 0)
                return BadRequest(new { message = "Order must contain at least one item" });

            order.TotalAmount = total;

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // Return order with details
            var orderDto = new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                RestaurantId = order.RestaurantId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                DeliveryAddress = order.DeliveryAddress,
                OrderDate = order.CreatedAt,
                Items = order.Items.Select(oi => new OrderItemDto
                {
                    MenuItemId = oi.MenuItemId,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    MenuItemName = _db.MenuItems.Find(oi.MenuItemId)?.Name
                }).ToList()
            };

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, orderDto);
        }

        [HttpGet("customer/{customerId}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetCustomerOrders(int customerId)
        {
            // Check if user is authorized to view these orders
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserRole != "Admin" && currentUserId != customerId)
                return Forbid();

            var orders = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Restaurant)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                RestaurantId = o.RestaurantId,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                DeliveryAddress = o.DeliveryAddress,
                OrderDate = o.CreatedAt,
                Items = o.Items.Select(oi => new OrderItemDto
                {
                    MenuItemId = oi.MenuItemId,
                    MenuItemName = oi.MenuItem?.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            });

            return Ok(orderDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Customer)
                .Include(o => o.Restaurant)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { message = "Order not found" });

            // Check authorization
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserRole != "Admin" && currentUserId != order.CustomerId)
                return Forbid();

            var orderDto = new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                RestaurantId = order.RestaurantId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                DeliveryAddress = order.DeliveryAddress,
                OrderDate = order.CreatedAt,
                Items = order.Items.Select(oi => new OrderItemDto
                {
                    MenuItemId = oi.MenuItemId,
                    MenuItemName = oi.MenuItem?.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };

            return Ok(orderDto);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Customer)
                .Include(o => o.Restaurant)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                RestaurantId = o.RestaurantId,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                DeliveryAddress = o.DeliveryAddress,
                OrderDate = o.CreatedAt,
                Items = o.Items.Select(oi => new OrderItemDto
                {
                    MenuItemId = oi.MenuItemId,
                    MenuItemName = oi.MenuItem?.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            });

            return Ok(orderDtos);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,RestaurantOwner")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { message = "Order not found" });

            // Check if user is restaurant owner
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserRole == "RestaurantOwner")
            {
                var restaurant = await _db.Restaurants
                    .FirstOrDefaultAsync(r => r.Id == order.RestaurantId && r.OwnerId == currentUserId);

                if (restaurant == null)
                    return Forbid();
            }

            order.Status = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Order status updated successfully", orderId = id, newStatus = dto.Status });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUserRole()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }

    public class UpdateOrderStatusDto
    {
        public string Status { get; set; }
    }
}