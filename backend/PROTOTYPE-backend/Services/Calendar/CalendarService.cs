using PROTOTYPE_backend.Data.Repositories;
using PROTOTYPE_backend.DTOs.Calendar;
using PROTOTYPE_backend.Data.Repositories;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Calendar
{
    public class CalendarService : ICalendarService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CalendarService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DateDto> CreateDateAsync(DateRequestDto dto, Guid userId) 
        {
            var user = await _unitOfWork.Repository<AppUser>().FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) throw new Exception("");

            var newDate = new CalendarDate
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Date = dto.Date,
                UserId = userId
            };

            await _unitOfWork.Repository<CalendarDate>().AddAsync(newDate);
            await _unitOfWork.SaveAsync();

            return new DateDto
            {
                Id = newDate.Id,
                Title = newDate.Title,
                Date = newDate.Date
            };
        }
    }
}
