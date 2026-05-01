using daleel_e_shop.BLL.DTOs.Auth;
using daleel_e_shop.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Roles
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleService(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<string> CreateRoleAsync(RoleModel model)
        {
            if (await _roleManager.RoleExistsAsync(model.RoleName))
                return "Role already exists.";

            var result = await _roleManager.CreateAsync(new IdentityRole(model.RoleName));

            if (result.Succeeded)
                return string.Empty;

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return $"Error creating role: {errors}";
        }

        public async Task<string> AssignRoleToUserAsync(UserRoleModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return "User not found.";

            if (!await _roleManager.RoleExistsAsync(model.RoleName))
                return "Role does not exist.";

            if (await _userManager.IsInRoleAsync(user, model.RoleName))
                return "User is already assigned to this role.";

            var result = await _userManager.AddToRoleAsync(user, model.RoleName);

            if (result.Succeeded)
                return string.Empty;

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return $"Error assigning role: {errors}";
        }

        public async Task<List<string>> GetRolesAsync()
        {
            return await _roleManager.Roles.Select(r => r.Name ?? string.Empty).ToListAsync();
        }

        public async Task<List<string>> GetUserRolesAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new List<string>();

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }
    }
}
