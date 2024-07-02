namespace BanAppealManager.UI.Models
{
    public class Player
    {
        public string Name { get; set; }
        public string UserId { get; set; }
        public string Server { get; set; }
    }
    
    public class ServerStatus
    {
        public string Server { get; set; }
        public bool IsSuccess { get; set; }
        public string StatusMessage { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}