using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.DTOs.Chat;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Chat;

namespace PROTOTYPE_backend.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public async Task JoinRoom(string roomId)
        {
            var userId = Context.User.GetUserId();

            if (!Guid.TryParse(roomId, out var roomGuid))
            {
                _logger.LogWarning("SignalR JoinRoom elutasítva: érvénytelen RoomId formátum. UserId: {UserId}, RoomId: {RoomId}, ConnectionId: {ConnectionId}",
                    userId, roomId, Context.ConnectionId);
                return;
            }

            var isMember = await _chatService.IsUserMemberOfRoomAsync(userId, roomGuid);

            if (isMember)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                _logger.LogDebug("Felhasználó csatlakozott a SignalR szobához. UserId: {UserId}, RoomId: {RoomId}, ConnectionId: {ConnectionId}",
                    userId, roomGuid, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("Jogosulatlan SignalR JoinRoom kísérlet! UserId: {UserId}, RoomId: {RoomId}, ConnectionId: {ConnectionId}",
                    userId, roomGuid, Context.ConnectionId);
            }
        }

        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            _logger.LogDebug("Felhasználó elhagyta a SignalR szobát. UserId: {UserId}, RoomId: {RoomId}, ConnectionId: {ConnectionId}",
                Context.User.GetUserId(), roomId, Context.ConnectionId);
        }

        public async Task SendMessage(SendMessageDto dto)
        {
            var userId = Context.User.GetUserId();
            var savedMessage = await _chatService.SaveMessageAsync(userId, dto);

            if (savedMessage != null)
            {
                await Clients.Group(dto.RoomId.ToString())
                    .SendAsync("ReceiveMessage", savedMessage);
            }
            else
            {
                _logger.LogWarning("SignalR SendMessage sikertelen (nem szobatag). UserId: {UserId}, RoomId: {RoomId}, ConnectionId: {ConnectionId}",
                    userId, dto.RoomId, Context.ConnectionId);
            }
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.GetUserId();

            if (exception != null)
            {
                _logger.LogWarning(exception, "SignalR kapcsolat váratlanul megszakadt. UserId: {UserId}, ConnectionId: {ConnectionId}",
                    userId, Context.ConnectionId);
            }
            else
            {
                _logger.LogDebug("SignalR kapcsolat normál módon lezárult. UserId: {UserId}, ConnectionId: {ConnectionId}",
                    userId, Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}