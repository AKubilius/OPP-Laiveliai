using Microsoft.AspNetCore.SignalR;
using Server.Models;

namespace Server.Hubs
{
    public class Game : Hub
    {
        private static List<Player> _registeredPlayers = new List<Player>();
        private static List<Match> _matches = new List<Match>();
        private static List<Player> _playersInMatchmaking = new List<Player>();

        private object _lockerRegisteredPlayers = new object();
        private object _lockerMatchmaking = new object();
        private object _lockerMatches = new object();


        public async void RegisterPlayer(string name)
        {
            lock (_lockerRegisteredPlayers)
            {
                var player = _registeredPlayers.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
                if (player == null)
                {
                    player = new Player { ConnectionId = Context.ConnectionId, Name = name };
                    _registeredPlayers.Add(player);
                }
            }

            await Clients.Client(Context.ConnectionId).SendAsync("RegisterComplete");
        }

        public async void SendLocation()
        {

        }

        public async void FindOpponent()
        {
            var player = _registeredPlayers.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (player == null) return;

            // player.LookingForOpponent = true;
            Player opponent = null;
            lock (_lockerMatchmaking)
            {
                _playersInMatchmaking.Add(player);
                opponent = _playersInMatchmaking.FirstOrDefault(x => x.ConnectionId != player.ConnectionId);
            }

            if (opponent == null)
            {
                await Clients.Client(player.ConnectionId).SendAsync("NoOpponents");
                return;
            }

            player.Opponent = opponent;
            opponent.Opponent = player;


            lock(_lockerMatchmaking)
            {
                _playersInMatchmaking.Remove(opponent);
                _playersInMatchmaking.Remove(player);
            }

            await Clients.Client(player.ConnectionId).SendAsync("FoundOpponent", opponent.Name);
            await Clients.Client(opponent.ConnectionId).SendAsync("FoundOpponent", player.Name);

            var match = new Match { Player1 = player, Player2 = opponent, MatchId = 1};

            lock (_lockerMatches)
            {
                _matches.Add(match);
            }

            await Clients.Client(player.ConnectionId).SendAsync("MatchCreated", match.MatchId);
            await Clients.Client(opponent.ConnectionId).SendAsync("MatchCreated", match.MatchId);



            //start match by sending players info and more stuf (set player coordinates etc)
        }
    }
}
