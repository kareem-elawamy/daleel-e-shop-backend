using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace daleel_e_shop.DAL.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, string[]? includes = null);
        Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate, string[]? includes = null);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}
