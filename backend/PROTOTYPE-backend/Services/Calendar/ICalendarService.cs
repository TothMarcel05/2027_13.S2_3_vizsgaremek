using PROTOTYPE_backend.DTOs.Calendar;

namespace PROTOTYPE_backend.Services.Calendar
{
    public interface ICalendarService
    {
        Task<DateDto> CreateDateAsync(DateRequestDto dto, Guid userId);
        Task<DateDto?> UpdateDateAsync(Guid id, DateUpdateDto dto);
        Task<bool> DeleteDateAsync(Guid id);
        Task<DateDto?> GetDateByIdAsync(Guid id);
        Task<IEnumerable<DateDto>> GetDatesByRangeAsync(DateTime start, DateTime end, Guid userId, Guid? projectId = null);
    }
}