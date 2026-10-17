namespace PROTOTYPE_backend.Services.SoftDelete;

public class SoftDeleteService : ISoftDeleteService
{
    public async Task<bool> SoftDeleteAsync<T>(Guid id, Guid userId) where T : class
    {
        return await Task.FromResult(true);
    }

    public async Task<bool> RestoreAsync<T>(Guid id) where T : class
    {
        return await Task.FromResult(true);
    }

    public async Task HardDeleteExpiredAsync<T>(TimeSpan retentionPeriod) where T : class
    {
        await Task.CompletedTask;
    }
}