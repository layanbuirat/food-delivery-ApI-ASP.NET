// Services/AuthService.cs
using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.DTOs.Auth;
using FoodDeliveryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext db, ILogger<AuthService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<RegistrationResult> RegisterAsync(RegisterDto dto)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _db.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (existingUser != null)
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = "Email already registered"
                    };

                // Validate role
                var validRoles = new[] { "Customer", "RestaurantOwner", "Admin" };
                if (!validRoles.Contains(dto.Role))
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = "Invalid role. Valid roles: Customer, RestaurantOwner, Admin"
                    };

                // Create new user
                var user = new User
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Role = dto.Role,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                return new RegistrationResult
                {
                    Success = true,
                    Message = "Registration successful",
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return new RegistrationResult
                {
                    Success = false,
                    Message = "An error occurred during registration"
                };
            }
        }

        public async Task<User?> ValidateUserAsync(LoginDto dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return null;

            return user;
        }
    }
}