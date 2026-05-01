using daleel_e_shop.DAL.Models;
using System;
using System.Threading.Tasks;

namespace daleel_e_shop.DAL.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<Category> Categories { get; }
        IGenericRepository<SubCategory> SubCategories { get; }
        IGenericRepository<Product> Products { get; }
        IGenericRepository<Order> Orders { get; }
        IGenericRepository<OrderItem> OrderItems { get; }
        IGenericRepository<Favorite> Favorites { get; }
        IGenericRepository<CartItem> CartItems { get; }
        IGenericRepository<Review> Reviews { get; }
        IGenericRepository<ProductImage> ProductImages { get; }
        IGenericRepository<Notification> Notifications { get; }

        Task<int> CompleteAsync();
    }
}
