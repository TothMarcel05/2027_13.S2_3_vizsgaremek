using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Calendar;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Calendar
{
    public class CalendarService : ICalendarService
    {
        private readonly AppDbContext _context;

        public CalendarService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<DateDto> CreateDateAsync(DateRequestDto dto, Guid userId) 
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) throw new Exception("Érvénytelen felhasználó!");

            var newDate = new CalendarEvent
            {
                Id = Guid.NewGuid(),
                ProjectId = dto.ProjectId,
                Title = dto.Title,
                StartTime = DateTime.UtcNow,
                EndTime = dto.timeStamp,
                CreatedBy = userId,
                IsAllDay = false,
                Description = dto.Description
            };

            await _context.CalendarEvents.AddAsync(newDate);
            await _context.SaveChangesAsync();

            return new DateDto(
                newDate.ProjectId ?? Guid.Empty,
                newDate.Id,
                newDate.EndTime,
                newDate.Title,
                newDate.Description ?? string.Empty
            );
        
        }
    }
}

