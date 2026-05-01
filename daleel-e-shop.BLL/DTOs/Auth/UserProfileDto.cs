using System.Collections.Generic;

namespace daleel_e_shop.BLL.DTOs.Auth
{
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
