namespace PROTOTYPE_backend.Services.SoftDelete;

public interface ISoftDeleteService
{
    Task<bool> SoftDeleteAsync<T>(Guid id, Guid userId) where T : class;
    Task<bool> RestoreAsync<T>(Guid id) where T : class;
    Task HardDeleteExpiredAsync<T>(TimeSpan retentionPeriod) where T : class;
}