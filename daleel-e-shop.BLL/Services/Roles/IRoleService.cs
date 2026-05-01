using daleel_e_shop.BLL.DTOs.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Roles
{
    public interface IRoleService
    {
        Task<string> CreateRoleAsync(RoleModel model);
        Task<string> AssignRoleToUserAsync(UserRoleModel model);
        Task<List<string>> GetRolesAsync();
        Task<List<string>> GetUserRolesAsync(string email);
    }
}
