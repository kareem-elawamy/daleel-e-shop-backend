using daleel_e_shop.BLL.DTOs.Auth;
using daleel_e_shop.BLL.Services.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace daleel_e_shop.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRoleAsync([FromBody] RoleModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _roleService.CreateRoleAsync(model);

            if (!string.IsNullOrEmpty(result))
                return BadRequest(result);

            return Ok(new { Message = "Role created successfully." });
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRoleToUserAsync([FromBody] UserRoleModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _roleService.AssignRoleToUserAsync(model);

            if (!string.IsNullOrEmpty(result))
                return BadRequest(result);

            return Ok(new { Message = "Role assigned successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> GetRolesAsync()
        {
            var roles = await _roleService.GetRolesAsync();
            return Ok(roles);
        }

        [HttpGet("user/{email}")]
        public async Task<IActionResult> GetUserRolesAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required.");

            var roles = await _roleService.GetUserRolesAsync(email);
            return Ok(roles);
        }
    }
}
