// Controllers/RestaurantsController.cs
using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.Models;
using FoodDeliveryAPI.DTOs.Restaurants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/restaurants")]
    public class RestaurantsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public RestaurantsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var restaurants = await _db.Restaurants
                .Include(r => r.MenuItems)
                .Where(r => r.IsActive)
                .Select(r => new RestaurantDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    CuisineType = r.CuisineType,
                    Address = r.Address,
                    PhoneNumber = r.PhoneNumber,
                    Rating = r.Rating,
                    MenuItems = r.MenuItems
                        .Where(m => m.IsAvailable)
                        .Select(m => new MenuItemDto
                        {
                            Id = m.Id,
                            Name = m.Name,
                            Description = m.Description,
                            Price = m.Price,
                            Category = m.Category
                        }).ToList()
                })
                .ToListAsync();

            return Ok(restaurants);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var restaurant = await _db.Restaurants
                .Include(r => r.MenuItems)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (restaurant == null)
                return NotFound(new { message = "Restaurant not found" });

            var restaurantDto = new RestaurantDto
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Description = restaurant.Description,
                CuisineType = restaurant.CuisineType,
                Address = restaurant.Address,
                PhoneNumber = restaurant.PhoneNumber,
                Rating = restaurant.Rating,
                MenuItems = restaurant.MenuItems
                    .Where(m => m.IsAvailable)
                    .Select(m => new MenuItemDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Description = m.Description,
                        Price = m.Price,
                        Category = m.Category
                    }).ToList()
            };

            return Ok(restaurantDto);
        }

        [HttpPost]
        [Authorize(Roles = "RestaurantOwner,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateRestaurantDto dto)
        {
            // Check if owner exists
            var owner = await _db.Users.FindAsync(dto.OwnerId);
            if (owner == null || owner.Role != "RestaurantOwner")
                return BadRequest(new { message = "Invalid owner ID or owner is not a restaurant owner" });

            var restaurant = new Restaurant
            {
                Name = dto.Name,
                OwnerId = dto.OwnerId,
                Description = dto.Description,
                CuisineType = dto.CuisineType,
                Address = dto.Address,
                PhoneNumber = dto.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Restaurants.Add(restaurant);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = restaurant.Id }, restaurant);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "RestaurantOwner,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRestaurantDto dto)
        {
            var restaurant = await _db.Restaurants.FindAsync(id);
            if (restaurant == null)
                return NotFound(new { message = "Restaurant not found" });

            // Check authorization (only owner or admin can update)
            var currentUserId = int.Parse(User.Claims.First(c => c.Type == "sub").Value);
            var currentUserRole = User.Claims.First(c => c.Type == "role").Value;

            if (currentUserRole != "Admin" && currentUserId != restaurant.OwnerId)
                return Forbid();

            restaurant.Name = dto.Name ?? restaurant.Name;
            restaurant.Description = dto.Description ?? restaurant.Description;
            restaurant.CuisineType = dto.CuisineType ?? restaurant.CuisineType;
            restaurant.Address = dto.Address ?? restaurant.Address;
            restaurant.PhoneNumber = dto.PhoneNumber ?? restaurant.PhoneNumber;
            restaurant.IsActive = dto.IsActive ?? restaurant.IsActive;
            restaurant.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Restaurant updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var restaurant = await _db.Restaurants.FindAsync(id);
            if (restaurant == null)
                return NotFound(new { message = "Restaurant not found" });

            // Soft delete (deactivate)
            restaurant.IsActive = false;
            restaurant.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Restaurant deactivated successfully" });
        }
    }

    public class RestaurantDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CuisineType { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public decimal Rating { get; set; }
        public List<MenuItemDto> MenuItems { get; set; }
    }

    public class MenuItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
    }

    public class CreateRestaurantDto
    {
        public string Name { get; set; }
        public int OwnerId { get; set; }
        public string Description { get; set; }
        public string CuisineType { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class UpdateRestaurantDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string CuisineType { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public bool? IsActive { get; set; }
    }
}