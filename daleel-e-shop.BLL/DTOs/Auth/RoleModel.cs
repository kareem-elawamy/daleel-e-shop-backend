using System.ComponentModel.DataAnnotations;

namespace daleel_e_shop.BLL.DTOs.Auth
{
    public class RoleModel
    {
        [Required]
        public string RoleName { get; set; } = string.Empty;
    }
}
