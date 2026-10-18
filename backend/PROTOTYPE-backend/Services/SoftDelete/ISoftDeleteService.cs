using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PROTOTYPE_backend.DTOs.SoftDelete;

namespace PROTOTYPE_backend.Services.SoftDelete
{
    public interface ISoftDeleteService
    {
        Task<bool> SoftDeleteAsync(SoftDeleteRequestDto dto);
        Task<bool> RestoreAsync(RestoreRequestDto dto);
        Task<IEnumerable<DeletedItemDto>> GetDeletedItemsAsync(string userId);
        Task<int> HardDeleteExpiredItemsAsync(); 
    }
}