namespace BanAppealManager.Main.Models;

    public class Userdetails
    {
        public string Username { get; set; }
        public DateTime FirstSeen { get; set; }
        public bool Whitelisted { get; set; }
        public double PlaytimeOverall { get; set; }
        public string AdminPageUrl { get; set; }
        public List<BanDetails> Banlist { get; set; }
        public List<RoleBanDetails> RoleBanlist { get; set; }
        public List<NoteDetails> Notes { get; set; }
        public List<GroupedRoleBan> GroupedRoleBans { get; set; }
    }

    public class BanDetails
    {
        public string Reason { get; set; }
        public DateTime BanTime { get; set; }
        public string RoundNumber { get; set; }
        public bool IsPermanent { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public DateTime? ExpireTime { get; set; }
        public DateTime? UnbanTime { get; set; }
        public string UnbannedBy { get; set; }
        public List<Banhits> Banhits { get; set; }
    }

    public class RoleBanDetails
    {
        public string Reason { get; set; }
        public string Role { get; set; }
        public DateTime BanTime { get; set; }
        public string RoundNumber { get; set; }
        public bool IsPermanent { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public DateTime? ExpireTime { get; set; }
        public DateTime? UnbanTime { get; set; }
        public string UnbannedBy { get; set; }
        public string BanningAdmin { get; set; }
    }

    public class Banhits
    {
        public string Username { get; set; }
        public Guid UserId { get; set; }
        public DateTime Time { get; set; }
        public string IpAddress { get; set; }
        public string HWID { get; set; }
    }

    public class NoteDetails
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public string Round { get; set; }
        public string Severity { get; set; }
        public string Visible { get; set; }
        public string Playtime { get; set; }
        public string Expires { get; set; }
        public DateTime Created { get; set; }
        public DateTime Edited { get; set; }
    }
    
    public class GroupedRoleBan
    {
        public string Reason { get; set; }
        public DateTime BanTime { get; set; }
        public DateTime? ExpireTime { get; set; }
        public DateTime? UnbanTime { get; set; }
        public string UnbannedBy { get; set; }
        public List<string> Roles { get; set; }
        public List<string> Departments { get; set; }
    }