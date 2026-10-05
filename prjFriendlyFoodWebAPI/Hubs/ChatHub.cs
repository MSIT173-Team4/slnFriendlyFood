using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;
using System.Security.Claims;
namespace prjFriendlyFoodWebAPI.Hubs
{
    public class ChatHub:Hub
    {
        private readonly FriendlyFoodDbContext _db;
        public ChatHub(FriendlyFoodDbContext db)
        {
            _db = db;
        }
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Connected: {Context.ConnectionId}");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Disconnected: {Context.ConnectionId}");

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(
                    int roomId,
                    string message)
        {
            int senderId = int.Parse(
                Context.User!
                    .FindFirst(ClaimTypes.NameIdentifier)!
                    .Value
            );
            bool allowed = await _db.TChatRooms
            .AnyAsync(r =>
                r.FId == roomId &&
                (
                    r.FUser1Id == senderId ||
                    r.FUser2Id == senderId
                )
            );
            if (!allowed)
            {
                throw new HubException("not allow send message");
            }
            var msg = new TChat
            {
                FChatRoomId = roomId,
                FSenderId = senderId,
                FContent=message,
                FMessageType=1,
                FCreatedTime=DateTime.Now,
            };
            _db.TChats.Add(msg);
            await _db.SaveChangesAsync();
            await Clients
                .Group($"room-{roomId}")
                .SendAsync(
                    "ReceiveMessage",
                    new
                    {
                        roomId,
                        senderId,
                        content = message,
                        sendTime = DateTime.Now
                    }
                );
        }
        public async Task JoinRoom(int roomId)
        {
            int userId = int.Parse(
                Context.User!
                    .FindFirst(ClaimTypes.NameIdentifier)!
                    .Value
            );

            bool allowed = await _db.TChatRooms
                .AnyAsync(r =>
                    r.FId == roomId &&
                    (
                        r.FUser1Id == userId ||
                        r.FUser2Id == userId
                    )
                );

            if (!allowed)
            {
                throw new HubException("無權限加入此聊天室");
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"room-{roomId}"
            );

            Console.WriteLine(
                $"User {userId} joined room-{roomId}"
            );
        }
    }
}
