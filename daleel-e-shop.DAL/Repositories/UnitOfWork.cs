using daleel_e_shop.DAL.Data;
using daleel_e_shop.DAL.Models;
using System.Threading.Tasks;

namespace daleel_e_shop.DAL.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IGenericRepository<Category> Categories { get; private set; }
        public IGenericRepository<SubCategory> SubCategories { get; private set; }
        public IGenericRepository<Product> Products { get; private set; }
        public IGenericRepository<Order> Orders { get; private set; }
        public IGenericRepository<OrderItem> OrderItems { get; private set; }
        public IGenericRepository<Favorite> Favorites { get; private set; }
        public IGenericRepository<CartItem> CartItems { get; private set; }
        public IGenericRepository<Review> Reviews { get; private set; }
        public IGenericRepository<ProductImage> ProductImages { get; private set; }
        public IGenericRepository<Notification> Notifications { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Categories = new GenericRepository<Category>(_context);
            SubCategories = new GenericRepository<SubCategory>(_context);
            Products = new GenericRepository<Product>(_context);
            Orders = new GenericRepository<Order>(_context);
            OrderItems = new GenericRepository<OrderItem>(_context);
            Favorites = new GenericRepository<Favorite>(_context);
            CartItems = new GenericRepository<CartItem>(_context);
            Reviews = new GenericRepository<Review>(_context);
            ProductImages = new GenericRepository<ProductImage>(_context);
            Notifications = new GenericRepository<Notification>(_context);
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
