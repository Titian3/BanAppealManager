using BanAppealManager.Main.Models;
using BanAppealManager.Main.Scrapers.SS14Admin;
using BanAppealManager.Main.Summarizers;

namespace BanAppealManager.Main.ResponseTemplates
{
    public static class ProcessAppealTemplate
    {
        public static string Generate(AppealData appealData, Userdetails userDetails, GPTResponseAppealProcessing gptResponseAppealProcessing, string ahelpLink)
        {
            string banTimeServedText;
            string oldestActiveBanReason;

            switch (appealData.BanType)
            {
                case "Game Ban":
                    var gameBanTimeServed = HelperMethods.CalculateBanTimeServed(userDetails);
                    banTimeServedText = gameBanTimeServed > 0 ? $"{gameBanTimeServed} days" : "Less than a day";
                    oldestActiveBanReason = HelperMethods.GetLatestBanReason(userDetails);
                    break;

                case "Role Ban":
                    var roleBanTimeServed = HelperMethods.CalculateRoleBanTimeServed(userDetails);
                    banTimeServedText = roleBanTimeServed > 0 ? $"{roleBanTimeServed} days" : "Less than a day";
                    oldestActiveBanReason = HelperMethods.GetLatestRoleBanReason(userDetails);
                    break;

                default:
                    banTimeServedText = "Unknown";
                    oldestActiveBanReason = "Unknown";
                    break;
            }

            string previousAppeals = $"https://forum.spacestation14.com/search?q={userDetails.Username}%20tags%3Aappeal-accepted%2Cappeal-rejected";

            string aHelpText = ahelpLink == "No aHelp" ? "No aHelp" : $"[Ahelp]({ahelpLink})";

            int banCount = userDetails.Banlist.Count;
            int roleBanCount = userDetails.RoleBanlist.Count;

            string votingText;
            switch (appealData.BanType)
            {
                case "Game Ban":
                    votingText = @"
# Vote
*For remove ban or upgrade to voucher to succeed, the option must have more than 50% of all votes. Please provide a justification for your votes in a response.*
*Votes to reduce must include a suggested reduction time and some explanation for choosing the time. Reduction suggestions are from the time that the vote started, not from the time that the ban was applied or the appeal was made.*
*Votes to upgrade to voucher will not be counted unless you provide an explanation for why you believe an upgrade to voucher is appropriate.*

[poll type=regular results=on_vote public=true chartType=bar groups=wizden-game-admins,wizden-trialmins]
* Remove Ban
* Reduce Ban
* Upgrade to Voucher
[/poll]";
                    break;

                case "Role Ban":
                    votingText = @"
# Vote
*For remove ban or maintain to succeed, the option must have more than 50% of all votes. Please provide a justification for your votes in a response.*
*Votes to reduce must include a suggested reduction time and some explanation for choosing the time. Reduction suggestions are from the time that the vote started, not from the time that the ban was applied or the appeal was made.*
*Votes to maintain will not be counted unless you provide an explanation for why you believe a maintain is appropriate.*

[poll type=regular results=on_vote public=true chartType=bar groups=wizden-game-admins,wizden-trialmins]
* Remove Ban
* Reduce Ban
* Maintain Ban
[/poll]";
                    break;

                default:
                    votingText = @"
[poll type=regular results=on_vote public=true chartType=bar groups=wizden-game-admins,wizden-trialmins]
* Vote Option 1
* Vote Option 2
* Vote Option 3
[/poll]";
                    break;
            }

            return $@"
{userDetails.Username} - {gptResponseAppealProcessing.SummarizedBanReason}

# Summary
Ban type: **{appealData.BanType}**
Ban Length: **{appealData.BanLength}**
Ban time Served so far: **{banTimeServedText}**

**Appeal Summary(Source: GPT4):**
{gptResponseAppealProcessing.AppealSummary}

**Appeal Summary(Source: Human):**
None.

# Player History
Player Username: {userDetails.Username}
Player Overall Playtime: **{userDetails.PlaytimeOverall:F2} Hours**
Whitelisted: **{(userDetails.Whitelisted ? "Yes" : "No")}**
First Login: **{userDetails.FirstSeen:yyyy-MM-dd}**

# Details
Ban reason from BanNote: **{oldestActiveBanReason}**
Ban Issue: **{appealData.BanIssue}**
Vote Opt-Out: **{appealData.BanVotePref}**
Total Bans: **{banCount}**
Total Role Bans: **{roleBanCount}**

# References
{aHelpText}
[SS14.Admin Player Page]({userDetails.AdminPageUrl})
[Current Appeal]({appealData.AppealURL})
[List previous Appeals]({previousAppeals})

{votingText}";
        }
    }
}
