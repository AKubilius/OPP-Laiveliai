using Microsoft.AspNetCore.SignalR;
using Server.Models;

namespace Server.Hubs
{
    public class Game : Hub
    {
        private static List<Client> _clients = new List<Client>();

        private object _locker = new object();
        public async void RegisterClient(string name)
        {
            lock (_locker)
            {
                var client = _clients.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
                if (client == null)
                {
                    client = new Client { ConnectionId = Context.ConnectionId, Name = name };
                    _clients.Add(client);
                }

                client.IsPlaying = false;
            }

            await Clients.Client(Context.ConnectionId).SendAsync("RegisterComplete");
        }

        public async void FindOpponent()
        {
            var player = _clients.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (player == null) return;

            player.LookingForOpponent = true;

            var opponent = _clients.FirstOrDefault(x => x.ConnectionId != player.ConnectionId && x.LookingForOpponent && !x.IsPlaying);
            if (opponent == null)
            {
                await Clients.Client(player.ConnectionId).SendAsync("NoOpponents");
                return;
            }

            PlayerIsPlaying(player);
            PlayerIsPlaying(opponent);

            player.Opponent = opponent;
            opponent.Opponent = player;

            await Clients.Client(player.ConnectionId).SendAsync("FoundOpponent", opponent.Name);
            await Clients.Client(opponent.ConnectionId).SendAsync("FoundOpponent", player.Name);
        }

        private static void PlayerIsPlaying(Client player)
        {
            player.IsPlaying = true;
            player.LookingForOpponent = false;
        }
    }
}
