using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace daleel_e_shop.DAL.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    }
}
