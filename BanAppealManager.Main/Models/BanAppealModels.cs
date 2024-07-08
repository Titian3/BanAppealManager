namespace BanAppealManager.Main.Models;

public class AppealData
{
    public string Username { get; set; }
    public string BanLength { get; set; }
    public string BanReason { get; set; }
    public string BanIssue { get; set; }
    public string BanPretext { get; set; }
    public string BanAppealReason { get; set; }
    public string BanVotePref { get; set; }
    public string AlternateAccounts { get; set; }
    public string AppealURL { get; set; }
    public string BanType { get; set; }
}

public class AppealSummary
{
    public string PostName { get; set; }
    public string Created { get; set; }
    public string LinkToAppeal { get; set; }
    public string OriginalPoster { get; set; }
}