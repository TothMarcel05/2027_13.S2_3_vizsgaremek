using System;
using System.Linq.Expressions;

namespace PROTOTYPE_backend.Data.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null
    );
    Task<T?> FirstOrDefault(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null
    );
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}
