using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data; // cseréld a saját DbContext namespace-edre
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

        // 1. LÉTREHOZÁS
        public async Task<DateDto> CreateDateAsync(DateRequestDto dto, Guid userId)
        {
            if (dto.EndTime < dto.StartTime)
            {
                throw new ArgumentException("A befejezés ideje nem lehet korábbi a kezdés idejénél.");
            }

            // Ha a naptáresemény projekthez van kötve, ellenőrizzük a tagságot
            if (dto.ProjectId.HasValue)
            {
                var isMember = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == dto.ProjectId.Value && pm.UserId == userId);

                if (!isMember)
                {
                    throw new UnauthorizedAccessException("Nincs jogosultságod eseményt hozzáadni ehhez a projekthez.");
                }
            }

            var calendarEvent = new CalendarEvent
            {
                Id = Guid.NewGuid(),
                ProjectId = dto.ProjectId,
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsAllDay = dto.IsAllDay,
                CreatedBy = userId
            };

            _context.CalendarEvents.Add(calendarEvent);
            await _context.SaveChangesAsync();

            return MapToDto(calendarEvent);
        }

        // 2. MÓDOSÍTÁS
        public async Task<DateDto?> UpdateDateAsync(Guid id, DateUpdateDto dto)
        {
            if (dto.EndTime < dto.StartTime)
            {
                throw new ArgumentException("A befejezés ideje nem lehet korábbi a kezdés idejénél.");
            }

            var calendarEvent = await _context.CalendarEvents.FindAsync(id);
            if (calendarEvent == null) return null;

            calendarEvent.ProjectId = dto.ProjectId;
            calendarEvent.Title = dto.Title;
            calendarEvent.Description = dto.Description;
            calendarEvent.Location = dto.Location;
            calendarEvent.StartTime = dto.StartTime;
            calendarEvent.EndTime = dto.EndTime;
            calendarEvent.IsAllDay = dto.IsAllDay;

            await _context.SaveChangesAsync();

            return MapToDto(calendarEvent);
        }

        // 3. TÖRLEÉS
        public async Task<bool> DeleteDateAsync(Guid id)
        {
            var calendarEvent = await _context.CalendarEvents.FindAsync(id);
            if (calendarEvent == null) return false;

            _context.CalendarEvents.Remove(calendarEvent);
            await _context.SaveChangesAsync();

            return true;
        }

        // 4. EGYEDI LEKÉRDEZÉS
        public async Task<DateDto?> GetDateByIdAsync(Guid id)
        {
            var calendarEvent = await _context.CalendarEvents.FindAsync(id);
            if (calendarEvent == null) return null;

            return MapToDto(calendarEvent);
        }

        // 5. INTERVALLUM ALAPÚ LEKÉRDEZÉS
        public async Task<IEnumerable<DateDto>> GetDatesByRangeAsync(DateTime start, DateTime end, Guid userId, Guid? projectId = null)
        {
            var query = _context.CalendarEvents
                .Where(e => e.StartTime <= end && e.EndTime >= start);

            if (projectId.HasValue)
            {
                // Projekt alapú szűrés
                query = query.Where(e => e.ProjectId == projectId.Value);
            }
            else
            {
                // Felhasználó saját vagy azon projektjeinek eseményei, ahol tag
                var userProjectIds = await _context.ProjectMembers
                    .Where(pm => pm.UserId == userId)
                    .Select(pm => pm.ProjectId)
                    .ToListAsync();

                query = query.Where(e => e.CreatedBy == userId || (e.ProjectId.HasValue && userProjectIds.Contains(e.ProjectId.Value)));
            }

            return await query.Select(e => new DateDto(
                e.Id,
                e.ProjectId,
                e.Title,
                e.Description,
                e.Location,
                e.StartTime,
                e.EndTime,
                e.IsAllDay,
                e.CreatedBy
            )).ToListAsync();
        }

        // Segédmetódus a leképezéshez
        private static DateDto MapToDto(CalendarEvent e) => new(
            e.Id,
            e.ProjectId,
            e.Title,
            e.Description,
            e.Location,
            e.StartTime,
            e.EndTime,
            e.IsAllDay,
            e.CreatedBy
        );
    }
}