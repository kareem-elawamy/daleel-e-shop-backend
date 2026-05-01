using System.ComponentModel.DataAnnotations;

namespace daleel_e_shop.BLL.DTOs.Auth
{
    public class LoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
