using System.ComponentModel.DataAnnotations;

namespace daleel_e_shop.BLL.DTOs.Auth
{
    public class UserRoleModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string RoleName { get; set; } = string.Empty;
    }
}
