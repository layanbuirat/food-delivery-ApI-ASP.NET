// Services/IAuthService.cs
using FoodDeliveryAPI.DTOs.Auth;
using FoodDeliveryAPI.Models;

namespace FoodDeliveryAPI.Services
{
    public interface IAuthService
    {
        Task<RegistrationResult> RegisterAsync(RegisterDto dto);
        Task<User?> ValidateUserAsync(LoginDto dto);
    }

    public class RegistrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; }
    }
}