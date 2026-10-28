using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Chat;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Chat;

namespace PROTOTYPE_backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        // GET /api/chat/rooms?workspaceId=guid -> Adott workspace szobái
        [HttpGet("rooms")]
        public async Task<IActionResult> GetUserRooms([FromQuery] Guid workspaceId)
        {
            var userId = User.GetUserId();
            var rooms = await _chatService.GetUserRoomsByWorkspaceAsync(userId, workspaceId);
            return Ok(rooms);
        }

        // POST /api/chat/rooms -> Új szoba (direct, group, project_channel)
        [HttpPost("rooms")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateChatRoomDto dto)
        {
            var userId = User.GetUserId();
            var room = await _chatService.CreateRoomAsync(userId, dto);
            return Ok(room);
        }

        // GET /api/chat/rooms/{roomId:guid}/messages -> Üzenethistória
        [HttpGet("rooms/{roomId:guid}/messages")]
        public async Task<IActionResult> GetRoomMessages(Guid roomId, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            var userId = User.GetUserId();
            var messages = await _chatService.GetRoomMessagesAsync(userId, roomId, page, limit);
            return Ok(messages);
        }
    }
}