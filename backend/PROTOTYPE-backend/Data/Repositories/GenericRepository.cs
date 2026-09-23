using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace PROTOTYPE_backend.Data.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context) 
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync() 
        => await _dbSet.ToListAsync();
    public async Task<T?> GetByIdAsync(int id) 
        => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null
    ) 
    {
        IQueryable<T> query = _dbSet.Where(predicate);

        if (include != null)
        {
            query = include(query);
        }

        return await query.ToListAsync();
    }
    public async Task<T?> FirstOrDefault(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null
    )
    {
        IQueryable<T> query = _dbSet.Where(predicate);

        if (include != null)
        {
            query = include(query);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task AddAsync(T entity) 
        => await _dbSet.AddAsync(entity);
    public void Update(T entity) 
        => _dbSet.Update(entity);
    public void Delete(T entity)
        => _dbSet.Remove(entity);
}
