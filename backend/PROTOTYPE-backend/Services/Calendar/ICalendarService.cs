using PROTOTYPE_backend.DTOs.Calendar;

namespace PROTOTYPE_backend.Services.Calendar
{
    public interface ICalendarService
    {
        Task<DateDto> CreateDateAsync(DateRequestDto dto, Guid userId);
        Task<DateDto?> UpdateDateAsync(Guid id, DateUpdateDto dto, Guid userId);
        Task<bool> DeleteDateAsync(Guid id, Guid userId);
        Task<DateDto?> GetDateByIdAsync(Guid id, Guid userId);
        Task<IEnumerable<DateDto>> GetDatesByRangeAsync(DateTime start, DateTime end, Guid userId, Guid? projectId = null);
    }
}