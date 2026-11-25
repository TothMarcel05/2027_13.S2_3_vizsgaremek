using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Notification;

namespace PROTOTYPE_backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        // 1. GET /api/notifications?page=1&limit=20&unreadOnly=true
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] bool unreadOnly = false)
        {
            if (page < 1 || limit < 1 || limit > 100)
            {
                return BadRequest(new { error = new { message = "Érvénytelen lapozási paraméterek (page >= 1, 1 <= limit <= 100).", code = "ERVENYTELEN_KERES" } });
            }

            var userId = User.GetUserId();
            var notifications = await _notificationService.GetNotificationsAsync(userId, page, limit, unreadOnly);
            return Ok(notifications);
            
        }

        // 2. GET /api/notifications/unread-count
        [HttpGet("unread-count")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUnreadNotificationsCount()
        {
            var userId = User.GetUserId();
            var unreadCount = await _notificationService.GetUnreadNotificationsCountAsync(userId);
            return Ok(new { count = unreadCount });
            
        }

        // 3. PUT /api/notifications/{id}/read
        [HttpPut("{id:guid}/read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkNotificationAsRead(Guid id)
        {
            var userId = User.GetUserId();
            await _notificationService.MarkNotificationAsReadAsync(userId, id);
            return Ok(new { message = "Az értesítés olvasottnak jelölve." });
            
        }

        // 4. PUT /api/notifications/read-all
        [HttpPut("read-all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var userId = User.GetUserId();
            await _notificationService.MarkAllNotificationsAsReadAsync(userId);
            return Ok(new { message = "Minden értesítés olvasottnak jelölve." });
            
        }
    }
}