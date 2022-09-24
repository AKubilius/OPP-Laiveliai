using Microsoft.AspNetCore.SignalR;
using Server.Models;

namespace Server.Hubs
{
    public class Game : Hub
    {
        private static List<PLayer> _players = new List<PLayer>();
        private static List<Match> _mathces = new List<Match>();

        private object _locker = new object();
        public async void RegisterPlayer(string name)
        {
            lock (_locker)
            {
                var player = _players.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
                if (player == null)
                {
                    player = new PLayer { ConnectionId = Context.ConnectionId, Name = name };
                    _players.Add(player);
                }

                player.IsPlaying = false;
            }

            await Clients.Client(Context.ConnectionId).SendAsync("RegisterComplete");
        }

        public async void FindOpponent()
        {
            var player = _players.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (player == null) return;

            player.LookingForOpponent = true;

            var opponent = _players.FirstOrDefault(x => x.ConnectionId != player.ConnectionId && x.LookingForOpponent && !x.IsPlaying);
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

            var match = new Match { Player1 = player, Player2 = opponent };

            lock (_locker)
            {
                _mathces.Add(match);
            }

            //start match by sending players info and more stuf (set player coordinates etc)
        }

        private static void PlayerIsPlaying(PLayer player)
        {
            player.IsPlaying = true;
            player.LookingForOpponent = false;
        }
    }
}
