namespace BanAppealManager.Main.Models
{
    public class BanDiscussionData
    {
        public string Username { get; set; }
        public string BanType { get; set; }
        public string BanLength { get; set; }
        public string TimeServed { get; set; }
        public string CurrentAppeal { get; set; }
        public int TotalVotes { get; set; }
        public List<string> VotingAdmins { get; set; }
        public List<VoteOption> VoteOptions { get; set; }
        public List<DiscussionComment> DiscussionComments { get; set; }
    }

    public class VoteOption
    {
        public string OptionText { get; set; }
        public string Type { get; set; }
        public int Percentage { get; set; }
        public int VoteCount { get; set; }
        public List<string> Voters { get; set; }
    }


    public class DiscussionComment
    {
        public string AdminName { get; set; }
        public string VoteType { get; set; }
        public bool HasComment { get; set; }
        public string Comment { get; set; }
        public List<string> CommentReasons { get; set; }
        public string CommentSentiment { get; set; }
        public int? ReductionLengthTimeInWeeks { get; set; }
        public string DiscussionOrDecision { get; set; }
        
    }

    public class VoteOutcome
    {
        public string VoteOutcomeType { get; set; }
        public int? MedianReductionTime { get; set; }
        public List<string> VoteReasons { get; set; }
        
    }

    public class BanDiscussionAnalysisResponse
    {
        public string ResponseTemplateSection1 { get; set; }
        public string ResponseTemplateSection2 { get; set; }
        public List<string> VoteReasonsList { get; set; }
        public int? MedianReductionTime { get; set; }
        public List<string> VotingAdmins { get; set; }
        public int TotalVotes { get; set; }
        public string BanType { get; set; }
        public List<VoteOption> VotingOptions { get; set; }
        public string OutcomeType { get; set; }
        public string CurrentAppeal { get; set; }
        
    }
}