namespace Server.Models
{
    public class Client
    {
        public string Name { get; set; }
        public Client Opponent { get; set; }
        public bool IsPlaying { get; set; }
        public bool LookingForOpponent { get; set; }
        public string ConnectionId { get; set; }
    }
}
