namespace Server.Models
{
    public class PLayer
    {
        public string Name { get; set; }
        public PLayer Opponent { get; set; }
        public bool IsPlaying { get; set; }
        public bool LookingForOpponent { get; set; }
        public string ConnectionId { get; set; }
    }
}
