using daleel_e_shop.BLL.DTOs.Auth;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthModel> RegisterAsync(RegisterModel model);
        Task<AuthModel> GetTokenAsync(LoginModel model);
        Task<string> UploadProfileImageAsync(string email, IFormFile image);
        Task<UserProfileDto?> GetProfileAsync(string userId);
        Task<UserProfileDto?> UpdateProfileAsync(string userId, UpdateProfileDto dto);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto dto);
    }
}
