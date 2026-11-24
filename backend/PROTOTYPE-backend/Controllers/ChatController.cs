using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        // 1. GET /api/chat/rooms?workspaceId=guid -> Adott workspace szobái ([Authorize])
        [HttpGet("rooms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserRooms([FromQuery] Guid workspaceId)
        {
            if (workspaceId == Guid.Empty)
            {
                return BadRequest(new { message = "A 'workspaceId' megadása kötelező." });
            }

            try
            {
                var userId = User.GetUserId();
                var rooms = await _chatService.GetUserRoomsByWorkspaceAsync(userId, workspaceId);
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a csevegőszobák lekérdezésekor. WorkspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a szobák lekérdezésekor." });
            }
        }

        // 2. POST /api/chat/rooms -> Új szoba (direct, group, project_channel)
        [HttpPost("rooms")]
        [Authorize(Policy = "chat.room.create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateRoom([FromBody] CreateChatRoomDto dto)
        {
            if (dto.WorkspaceId == Guid.Empty)
            {
                return BadRequest(new { message = "A 'WorkspaceId' megadása kötelező." });
            }

            try
            {
                var userId = User.GetUserId();
                var room = await _chatService.CreateRoomAsync(userId, dto);
                return StatusCode(StatusCodes.Status201Created, room);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt új csevegőszoba létrehozásakor. WorkspaceId: {WorkspaceId}", dto.WorkspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a szoba létrehozásakor." });
            }
        }

        // 3. POST /api/chat/rooms/direct -> 1:1 privát szoba lekérése vagy létrehozása ([Authorize])
        [HttpPost("rooms/direct")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrCreateDirectRoom([FromQuery] Guid workspaceId, [FromQuery] Guid targetUserId)
        {
            if (workspaceId == Guid.Empty || targetUserId == Guid.Empty)
            {
                return BadRequest(new { message = "A 'workspaceId' és a 'targetUserId' megadása kötelező." });
            }

            try
            {
                var userId = User.GetUserId();
                if (userId == targetUserId)
                {
                    return BadRequest(new { message = "Önmagaddal nem indíthatsz privát beszélgetést." });
                }

                var room = await _chatService.GetOrCreateDirectRoomAsync(userId, workspaceId, targetUserId);
                return Ok(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a 1:1 csevegőszoba megnyitásakor. WorkspaceId: {WorkspaceId}, TargetUserId: {TargetUserId}",
                    workspaceId, targetUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a privát szoba megnyitásakor." });
            }
        }

        // 4. GET /api/chat/rooms/{roomId:guid}/messages -> Üzenethistória ([Authorize])
        [HttpGet("rooms/{roomId:guid}/messages")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRoomMessages(Guid roomId, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (page < 1 || limit < 1 || limit > 100)
            {
                return BadRequest(new { message = "Érvénytelen lapozási paraméterek (page >= 1, 1 <= limit <= 100)." });
            }

            try
            {
                var userId = User.GetUserId();

                if (!await _chatService.IsUserMemberOfRoomAsync(userId, roomId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Nem vagy tagja ennek a csevegőszobának." });
                }

                var messages = await _chatService.GetRoomMessagesAsync(userId, roomId, page, limit);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt az üzenethistória lekérdezésekor. RoomId: {RoomId}", roomId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt az üzenetek lekérdezésekor." });
            }
        }

        // 5. POST /api/chat/rooms/{roomId:guid}/messages -> Üzenet küldése a csevegőszobába ([Authorize])
        [HttpPost("rooms/{roomId:guid}/messages")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendMessage(Guid roomId, [FromBody] SendMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.MessageText) && (dto.Attachments == null || dto.Attachments.Count == 0))
            {
                return BadRequest(new { message = "Az üzenet szövege és a mellékletek listája egyszerre nem lehet üres." });
            }

            try
            {
                var userId = User.GetUserId();
                var message = await _chatService.SendMessageAsync(userId, roomId, dto);
                return StatusCode(StatusCodes.Status201Created, message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt az üzenet küldésekor. RoomId: {RoomId}", roomId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt az üzenet küldésekor." });
            }
        }

        // 6. DELETE /api/chat/messages/{messageId:guid} -> Saját üzenet törlése ([Authorize])
        [HttpDelete("messages/{messageId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            try
            {
                var userId = User.GetUserId();
                await _chatService.DeleteMessageAsync(userId, messageId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt az üzenet törlésekor. MessageId: {MessageId}", messageId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt az üzenet törlésekor." });
            }
        }
    }
}