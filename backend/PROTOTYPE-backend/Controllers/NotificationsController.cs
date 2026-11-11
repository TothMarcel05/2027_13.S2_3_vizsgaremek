using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Auth;
using PROTOTYPE_backend.DTOs.Notification;
using PROTOTYPE_backend.Services.Notification;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PROTOTYPE_backend.Controllers
{
    [ApiController]
    [Route("api/workspaces")]
    //[Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("/notifications")]
        public async Task<IActionResult> GetNotifications(Guid userId, int page = 1, int limit = 20, bool unreadOnly = false)
        {
            var notification = await _notificationService.GetNotificationsAsync(userId);
            return Ok(notification);
        }


        [HttpGet("/notifications/unread-count")]
        public async Task<IActionResult> GetUnreadNotifications(Guid userId)
        {
            var notification = await _notificationService.GetUnreadNotificationsCountAsync(userId);
            return Ok(notification);
        }


        [HttpPut("/notifications/{id}/read")]
        public async Task<IActionResult> GetUnreadNotificationToRead(Guid userId, Guid notificationId)
        {
            var notification = await _notificationService.MarkNotificationAsReadAsync(userId, notificationId);
            return Ok(notification);
        }


        [HttpPut("/notifications/read-all")]
        public async Task<IActionResult> GetAllUnreadNotificationsToRead(Guid userId)
        {
            var notification = await _notificationService.MarkAllNotificationsAsReadAsync(userId);
            return Ok(notification);
        }


    }

}