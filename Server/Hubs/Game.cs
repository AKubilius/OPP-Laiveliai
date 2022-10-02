using Microsoft.AspNetCore.SignalR;
using Server.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;

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
            bool isSuccesful = false;
            lock (_lockerRegisteredPlayers)
            {
                var player = _registeredPlayers.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);

                if (player == null)
                {
                    Player temp = _registeredPlayers.FirstOrDefault(x => x.Name.Equals(name));
                    if (temp == null)
                    {
                        player = new Player { ConnectionId = Context.ConnectionId, Name = name };
                        _registeredPlayers.Add(player);
                        isSuccesful = true;
                    }
                }
            }

            if (isSuccesful)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("RegisterComplete");
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("NameOccupied");
            }

        }

        public void LogoutPlayer()
        {
            lock (_lockerRegisteredPlayers)
            {
                var player = _registeredPlayers.First(x => x.ConnectionId == Context.ConnectionId);

                if (player != null)
                {
                    lock (_lockerMatchmaking)
                    {
                        _playersInMatchmaking.Remove(player);
                    }
                    _registeredPlayers.Remove(player);
                }
            }
        }

        public async void LeftMatch()
        {
            Match match = null;
            lock (_matches)
            {
                match = _matches.First(x => x.Players.Any(y => y.ConnectionId == Context.ConnectionId));

                if (match != null)
                {
                    _matches.Remove(match);
                }
            }

            if(match != null)
            {
                foreach (Player player in match.Players)
                {
                    if(player.ConnectionId != Context.ConnectionId)
                        await Clients.Client(player.ConnectionId).SendAsync("LeaveMatch");
                }
            }
        }

        public async void SendLocation(int matchId, string playerName, string facing, int xAxis, int yAxis)
        {
            Match match = null;
            lock (_lockerMatches)
            {
               
                foreach(Match m in _matches)
                {
                    foreach(Player p in m.Players)
                    {
                        if (p.Name.Equals(playerName))
                        {
                            match = m;
                            break;
                        }
                    }
                }
            }
            Player opponent = match.Players.First(x => x.Name != playerName);
            if(opponent != null)
                await Clients.Client(opponent.ConnectionId).SendAsync("LocationInfo", playerName, facing, xAxis, yAxis);
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
                await Clients.Client(player.ConnectionId).SendAsync("InMatchmakingQueue");
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
            
            var match = new Match { Players = new List<Player> { player, opponent }, MatchId = DateTime.UtcNow.GetHashCode()};

            lock (_lockerMatches)
            {
                _matches.Add(match);
            }

            await Clients.Client(player.ConnectionId).SendAsync("MatchCreated", match.MatchId);
            await Clients.Client(opponent.ConnectionId).SendAsync("MatchCreated", match.MatchId);

        }
    }
}
