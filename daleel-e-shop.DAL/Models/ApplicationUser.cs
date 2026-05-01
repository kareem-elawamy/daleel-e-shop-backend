using Microsoft.AspNetCore.Identity;

namespace daleel_e_shop.DAL.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
