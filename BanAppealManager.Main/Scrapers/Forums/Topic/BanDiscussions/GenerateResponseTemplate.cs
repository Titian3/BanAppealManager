using System;
using System.Text;
using BanAppealManager.Main.Models;

namespace BanAppealManager.Main.Scrapers.Forums.Topic.BanDiscussions
{
    public class GenerateResponseTemplate
    {
        public BanDiscussionAnalysisResponse GenerateTemplate(BanDiscussionData data, VoteOutcome outcome)
        {

            var repsonseDetails = new BanDiscussionAnalysisResponse
            {
                ResponseTemplateSection1 = GetVoteOutcome(outcome),
                ResponseTemplateSection2 = GetFinalOutcome(outcome, data),
                VoteReasonsList = outcome.VoteReasons,
                MedianReductionTime = outcome.MedianReductionTime,
                VotingAdmins = data.VotingAdmins,
                TotalVotes = data.TotalVotes,
                BanType = data.BanType,
                VotingOptions = data.VoteOptions,
                OutcomeType = outcome.VoteOutcomeType,
                CurrentAppeal = data.CurrentAppeal
            };
            
            return repsonseDetails;
        }

        private string GetVoteOutcome(VoteOutcome outcome)
        {
            return outcome.VoteOutcomeType switch
            {
                "Removal" => "This has been put to an admin discussion and vote of which we have come to the consensus to remove the ban.",
                "Maintain" => "This has been put to an admin discussion and vote of which we have come to the consensus to maintain the ban.",
                "Reduction" => $"This has been put to an admin discussion and vote of which we have come to the consensus to reduce the ban from indefinite to {outcome.MedianReductionTime} weeks.",
                "Voucher" => "This has been put to an admin discussion and vote of which we have come to the consensus to issue a voucher requirement for further appeals.",
                "Deny" => "This has been put to an admin discussion and vote of which we have come to the consensus to deny the appeal. The ban will run its course.",
                _ => throw new ArgumentException("Invalid outcome type")
            };
        }

        private string GetGeneralReasons(BanDiscussionData data)
        {
            var reasons = new StringBuilder();
            // Generalize the details based on data
            reasons.AppendLine("- New player");
            reasons.AppendLine("- Time served");
            // Add more generalized reasons
            return reasons.ToString();
        }

        private string GetFinalOutcome(VoteOutcome outcome, BanDiscussionData data)
        {
            var reductionWeeks = outcome.MedianReductionTime ?? 1;
            var endDate = DateTime.Now.AddDays(reductionWeeks * 7).ToString("yyyy-MM-dd");
            
            return outcome.VoteOutcomeType switch
            {
                "Removal" => "Appeal Accepted - Ban Removed.",
                "Maintain" => "Appeal Denied - Ban to run its course.",
                "Reduction" => $"Appeal Accepted - Reduced ban ending on [date={endDate} timezone=\"UTC\"].",
                "Deny" => $"Ban to maintain, you may reappeal no sooner than {DateTime.Now.AddMonths(6):yyyy-MM-dd}.",
                "Voucher" => @"You may appeal your ban, but only at least 6 months after your last ban, and only with a voucher of good behavior from another SS13/SS14 server.
A voucher of good behavior should be obtained from a well-known or decently active SS13/SS14 server. If it is a mainstream server, we recommend using that server's admin-help to ask for a voucher from one of the administrators explaining that you are trying to appeal a ban on SS14's Wizard's Den and want to show you have been a problem-free player during your playtime on the server. A voucher should be indicative of at least a few months of play. If the voucher is not from a mainstream server, let us know and we will figure out a way to verify it.
Appeal Denied - Voucher must be provided on any future re-appeal.",
                _ => throw new ArgumentException("Invalid outcome type")
            };
        }
        
    }
}
