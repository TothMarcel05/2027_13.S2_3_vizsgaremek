using System;
using System.Threading.Tasks;

namespace PROTOTYPE_backend.Data.Repositories;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class;
    Task<int> CompleteAsync();
}