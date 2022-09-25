namespace Server.Models
{
    public class Player
    {
        public string Name { get; set; }
        public Player Opponent { get; set; }
        public string ConnectionId { get; set; }
    }
}
