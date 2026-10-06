using PROTOTYPE_backend.DTOs.Calendar;

namespace PROTOTYPE_backend.Services.Calendar
{
    public interface ICalendarService
    {
        Task<DateDto> CreateDateAsync(DateRequestDto dto, Guid userId);
    }
}
