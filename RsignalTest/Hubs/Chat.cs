using Microsoft.AspNetCore.SignalR;
namespace RsignalTest.Hubs
{
    public class Chat : Hub
    {
        public Task SendMessage(string user, string message)
        {
            return Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
