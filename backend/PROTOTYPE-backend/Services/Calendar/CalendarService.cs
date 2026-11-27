using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Calendar;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Calendar
{
    public class CalendarService : ICalendarService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CalendarService> _logger;

        public CalendarService(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CalendarService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // 1. LÉTREHOZÁS
        public async Task<DateDto> CreateDateAsync(DateRequestDto dto, Guid userId)
        {
            if (dto.EndTime < dto.StartTime)
            {
                _logger.LogWarning(
                    "Naptáresemény létrehozása elutasítva: érvénytelen időintervallum. UserId: {UserId}, StartTime: {StartTime}, EndTime: {EndTime}",
                    userId, dto.StartTime, dto.EndTime);
                throw new ArgumentException("A befejezés ideje nem lehet korábbi a kezdés idejénél.");
            }

            // Ha projekthez van kötve, ellenőrizzük a calendar.manage jogosultságot
            if (dto.ProjectId.HasValue)
            {
                if (!HasProjectCalendarPermission(dto.ProjectId.Value))
                {
                    _logger.LogWarning(
                        "Jogosulatlan naptáresemény-létrehozási kísérlet projekthez (hiányzó calendar.manage). UserId: {UserId}, ProjectId: {ProjectId}",
                        userId, dto.ProjectId.Value);
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

            _logger.LogInformation(
                "Új naptáresemény létrehozva. EventId: {EventId}, ProjectId: {ProjectId}, UserId: {UserId}, StartTime: {StartTime}, IsAllDay: {IsAllDay}",
                calendarEvent.Id, calendarEvent.ProjectId, userId, calendarEvent.StartTime, calendarEvent.IsAllDay);

            return MapToDto(calendarEvent);
        }

        // 2. MÓDOSÍTÁS
        public async Task<DateDto?> UpdateDateAsync(Guid id, DateUpdateDto dto, Guid userId)
        {
            if (dto.EndTime < dto.StartTime)
            {
                _logger.LogWarning(
                    "Naptáresemény módosítása elutasítva: érvénytelen időintervallum. EventId: {EventId}, StartTime: {StartTime}, EndTime: {EndTime}",
                    id, dto.StartTime, dto.EndTime);
                throw new ArgumentException("A befejezés ideje nem lehet korábbi a kezdés idejénél.");
            }

            var calendarEvent = await _context.CalendarEvents.FindAsync(id);
            if (calendarEvent == null)
            {
                _logger.LogWarning("Naptáresemény módosítása sikertelen: az esemény nem található. EventId: {EventId}", id);
                return null;
            }

            // Jogosultság ellenőrzése a meglévő esemény típusa alapján
            EnsureCanModifyEvent(calendarEvent, userId);

            // Ha a módosítással másik projekthez rendelik át, az új projektre is kell jogosultság
            if (dto.ProjectId.HasValue && dto.ProjectId != calendarEvent.ProjectId)
            {
                if (!HasProjectCalendarPermission(dto.ProjectId.Value))
                {
                    _logger.LogWarning(
                        "Jogosulatlan projekt-áthelyezési kísérlet naptáreseménynél. EventId: {EventId}, TargetProjectId: {ProjectId}, UserId: {UserId}",
                        id, dto.ProjectId.Value, userId);
                    throw new UnauthorizedAccessException("Nincs jogosultságod a célprojekt naptárát módosítani.");
                }
            }

            calendarEvent.ProjectId = dto.ProjectId;
            calendarEvent.Title = dto.Title;
            calendarEvent.Description = dto.Description;
            calendarEvent.Location = dto.Location;
            calendarEvent.StartTime = dto.StartTime;
            calendarEvent.EndTime = dto.EndTime;
            calendarEvent.IsAllDay = dto.IsAllDay;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Naptáresemény módosítva. EventId: {EventId}, ProjectId: {ProjectId}, UserId: {UserId}",
                calendarEvent.Id, calendarEvent.ProjectId, userId);

            return MapToDto(calendarEvent);
        }

        // 3. TÖRLÉS
        public async Task<bool> DeleteDateAsync(Guid id, Guid userId)
        {
            var calendarEvent = await _context.CalendarEvents.FindAsync(id);
            if (calendarEvent == null)
            {
                _logger.LogWarning("Naptáresemény törlése sikertelen: az esemény nem található. EventId: {EventId}", id);
                return false;
            }

            EnsureCanModifyEvent(calendarEvent, userId);

            var projectId = calendarEvent.ProjectId;
            _context.CalendarEvents.Remove(calendarEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Naptáresemény törölve. EventId: {EventId}, ProjectId: {ProjectId}, UserId: {UserId}", id, projectId, userId);

            return true;
        }

        // 4. EGYEDI LEKÉRDEZÉS
        public async Task<DateDto?> GetDateByIdAsync(Guid id, Guid userId)
        {
            var calendarEvent = await _context.CalendarEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (calendarEvent == null)
            {
                _logger.LogDebug("Naptáresemény lekérdezése: nem található rekord. EventId: {EventId}", id);
                return null;
            }

            // Személyes eseményt csak a tulajdonosa, projekteseményt csak a projekt tagjai láthatnak
            if (calendarEvent.ProjectId == null)
            {
                if (calendarEvent.CreatedBy != userId)
                    throw new UnauthorizedAccessException("Nincs jogosultságod megtekinteni ezt a személyes eseményt.");
            }
            else
            {
                var isMember = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == calendarEvent.ProjectId.Value && pm.UserId == userId);

                if (!isMember && !IsSystemAdmin())
                    throw new UnauthorizedAccessException("Nincs jogosultságod megtekinteni ezt a projekteseményt.");
            }

            return MapToDto(calendarEvent);
        }

        // 5. INTERVALLUM ALAPÚ LEKÉRDEZÉS
        public async Task<IEnumerable<DateDto>> GetDatesByRangeAsync(DateTime start, DateTime end, Guid userId, Guid? projectId = null)
        {
            var query = _context.CalendarEvents
                .AsNoTracking()
                .Where(e => e.StartTime <= end && e.EndTime >= start);

            if (projectId.HasValue)
            {
                // Ellenőrizzük, hogy a felhasználó tagja-e a kért projektnek
                var isMember = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == projectId.Value && pm.UserId == userId);

                if (!isMember && !IsSystemAdmin())
                {
                    throw new UnauthorizedAccessException("Nincs jogosultságod a projekt naptárának megtekintéséhez.");
                }

                query = query.Where(e => e.ProjectId == projectId.Value);
            }
            else
            {
                // Személyes nézet: a felhasználó saját személyes eseményei + azon projektjeinek eseményei, ahol tag
                var userProjectIds = await _context.ProjectMembers
                    .Where(pm => pm.UserId == userId)
                    .Select(pm => pm.ProjectId)
                    .ToListAsync();

                query = query.Where(e =>
                    (e.ProjectId == null && e.CreatedBy == userId) ||
                    (e.ProjectId.HasValue && userProjectIds.Contains(e.ProjectId.Value)));
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

        // Jogosultság-ellenőrző segédmetódusok
        private void EnsureCanModifyEvent(CalendarEvent calendarEvent, Guid userId)
        {
            if (calendarEvent.ProjectId == null)
            {
                if (calendarEvent.CreatedBy != userId)
                {
                    _logger.LogWarning(
                        "Jogosulatlan személyes naptáresemény-módosítási kísérlet. EventId: {EventId}, OwnerId: {OwnerId}, InitiatorUserId: {UserId}",
                        calendarEvent.Id, calendarEvent.CreatedBy, userId);
                    throw new UnauthorizedAccessException("Csak a saját személyes naptáreseményedet módosíthatod vagy törölheted.");
                }
            }
            else
            {
                if (!HasProjectCalendarPermission(calendarEvent.ProjectId.Value))
                {
                    _logger.LogWarning(
                        "Jogosulatlan projektesemény-módosítási kísérlet (hiányzó calendar.manage). EventId: {EventId}, ProjectId: {ProjectId}, UserId: {UserId}",
                        calendarEvent.Id, calendarEvent.ProjectId.Value, userId);
                    throw new UnauthorizedAccessException("Nincs jogosultságod a projekt naptáreseményeit kezelni.");
                }
            }
        }

        private bool IsSystemAdmin()
        {
            var globalRole = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return globalRole == "2" || globalRole == "SystemAdmin";
        }

        private bool HasProjectCalendarPermission(Guid projectId)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return false;

            if (IsSystemAdmin()) return true;

            var permissionsJson = user.FindFirstValue("all_roles_permissions");
            if (string.IsNullOrEmpty(permissionsJson)) return false;

            try
            {
                var rolesAndPermissions = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(permissionsJson);
                if (rolesAndPermissions == null) return false;

                // 1. Projekt-szintű jogosultság ellenőrzése
                if (rolesAndPermissions.TryGetValue(projectId.ToString(), out var projPerms) &&
                    projPerms.Contains("calendar.manage"))
                {
                    return true;
                }

                // 2. Fallback: aktív munkaterület jogosultsága (Workspace Owner/Admin)
                var activeWsId = user.FindFirstValue("active_workspace_id");
                if (!string.IsNullOrEmpty(activeWsId) &&
                    rolesAndPermissions.TryGetValue(activeWsId, out var wsPerms) &&
                    wsPerms.Contains("calendar.manage"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

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