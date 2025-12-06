// Controllers/MenuItemsController.cs
using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.Models;
using FoodDeliveryAPI.DTOs.Restaurants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/restaurants/{restaurantId}/menuitems")]
    [Authorize]
    public class MenuItemsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public MenuItemsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int restaurantId)
        {
            var menuItems = await _db.MenuItems
                .Where(m => m.RestaurantId == restaurantId && m.IsAvailable)
                .Select(m => new MenuItemDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    Category = m.Category
                })
                .ToListAsync();

            return Ok(menuItems);
        }

        [HttpPost]
        [Authorize(Roles = "RestaurantOwner,Admin")]
        public async Task<IActionResult> Create(int restaurantId, [FromBody] CreateMenuItemDto dto)
        {
            // Check if restaurant exists and user is authorized
            var restaurant = await _db.Restaurants.FindAsync(restaurantId);
            if (restaurant == null)
                return NotFound(new { message = "Restaurant not found" });

            // Check authorization
            var currentUserId = int.Parse(User.Claims.First(c => c.Type == "sub").Value);
            var currentUserRole = User.Claims.First(c => c.Type == "role").Value;

            if (currentUserRole != "Admin" && currentUserId != restaurant.OwnerId)
                return Forbid();

            var menuItem = new MenuItem
            {
                RestaurantId = restaurantId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Category = dto.Category,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.MenuItems.Add(menuItem);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { restaurantId, id = menuItem.Id }, menuItem);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int restaurantId, int id)
        {
            var menuItem = await _db.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id && m.RestaurantId == restaurantId && m.IsAvailable);

            if (menuItem == null)
                return NotFound(new { message = "Menu item not found" });

            var menuItemDto = new MenuItemDto
            {
                Id = menuItem.Id,
                Name = menuItem.Name,
                Description = menuItem.Description,
                Price = menuItem.Price,
                Category = menuItem.Category
            };

            return Ok(menuItemDto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "RestaurantOwner,Admin")]
        public async Task<IActionResult> Update(int restaurantId, int id, [FromBody] UpdateMenuItemDto dto)
        {
            var menuItem = await _db.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id && m.RestaurantId == restaurantId);

            if (menuItem == null)
                return NotFound(new { message = "Menu item not found" });

            // Check authorization
            var restaurant = await _db.Restaurants.FindAsync(restaurantId);
            var currentUserId = int.Parse(User.Claims.First(c => c.Type == "sub").Value);
            var currentUserRole = User.Claims.First(c => c.Type == "role").Value;

            if (currentUserRole != "Admin" && currentUserId != restaurant.OwnerId)
                return Forbid();

            menuItem.Name = dto.Name ?? menuItem.Name;
            menuItem.Description = dto.Description ?? menuItem.Description;
            menuItem.Price = dto.Price ?? menuItem.Price;
            menuItem.Category = dto.Category ?? menuItem.Category;
            menuItem.IsAvailable = dto.IsAvailable ?? menuItem.IsAvailable;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Menu item updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "RestaurantOwner,Admin")]
        public async Task<IActionResult> Delete(int restaurantId, int id)
        {
            var menuItem = await _db.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id && m.RestaurantId == restaurantId);

            if (menuItem == null)
                return NotFound(new { message = "Menu item not found" });

            // Check authorization
            var restaurant = await _db.Restaurants.FindAsync(restaurantId);
            var currentUserId = int.Parse(User.Claims.First(c => c.Type == "sub").Value);
            var currentUserRole = User.Claims.First(c => c.Type == "role").Value;

            if (currentUserRole != "Admin" && currentUserId != restaurant.OwnerId)
                return Forbid();

            // Soft delete
            menuItem.IsAvailable = false;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Menu item deactivated successfully" });
        }
    }

    public class CreateMenuItemDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
    }

    public class UpdateMenuItemDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; }
        public string Category { get; set; }
        public bool? IsAvailable { get; set; }
    }
}