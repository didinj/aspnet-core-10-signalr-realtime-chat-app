using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;


namespace ChatServer.Hubs
{
    public class ChatHub : Hub
    {
        // In‑memory presence
        private static readonly ConcurrentDictionary<string, string> _users = new();


        public override async Task OnConnectedAsync()
        {
            var user = Context.GetHttpContext()?.Request.Query["user"].ToString();
            if (string.IsNullOrWhiteSpace(user))
            {
                user = $"Guest-{Context.ConnectionId[..5]}";
            }


            _users[Context.ConnectionId] = user;


            await Clients.Caller.SendAsync("Presence", _users.Values.Distinct().Order());
            await Clients.Others.SendAsync("UserJoined", user);


            await base.OnConnectedAsync();
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_users.TryRemove(Context.ConnectionId, out var user))
            {
                await Clients.All.SendAsync("UserLeft", user);
            }
            await base.OnDisconnectedAsync(exception);
        }


        public Task SendMessage(string message)
        {
            var user = _users.GetValueOrDefault(Context.ConnectionId, "Unknown");
            return Clients.All.SendAsync("Message", new { user, message, at = DateTimeOffset.UtcNow });
        }


        public async Task JoinRoom(string room)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, room);
            var user = _users.GetValueOrDefault(Context.ConnectionId, "Unknown");
            await Clients.Group(room).SendAsync("RoomEvent", new { room, type = "join", user });
        }


        public async Task LeaveRoom(string room)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
            var user = _users.GetValueOrDefault(Context.ConnectionId, "Unknown");
            await Clients.Group(room).SendAsync("RoomEvent", new { room, type = "leave", user });
        }


        public Task SendToRoom(string room, string message)
        {
            var user = _users.GetValueOrDefault(Context.ConnectionId, "Unknown");
            return Clients.Group(room).SendAsync("RoomMessage", new { room, user, message, at = DateTimeOffset.UtcNow });
        }


        public Task Typing(string? room)
        {
            var user = _users.GetValueOrDefault(Context.ConnectionId, "Unknown");
            if (string.IsNullOrEmpty(room))
                return Clients.Others.SendAsync("Typing", user);
            return Clients.Group(room).SendAsync("Typing", user);
        }
    }
}