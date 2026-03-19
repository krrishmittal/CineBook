using Microsoft.AspNetCore.SignalR;

namespace CineBook.API.Hubs
{
    public class SeatHub : Hub
    {
        // Called when user opens the seat selection page
        // They join a "group" for that specific showtime
        public async Task JoinShowtime(string showtimeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"showtime-{showtimeId}");
        }

        // Called when user leaves the page
        public async Task LeaveShowtime(string showtimeId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"showtime-{showtimeId}");
        }

        // Auto cleanup when connection drops
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}