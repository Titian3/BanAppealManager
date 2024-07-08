using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BanAppealManager.Main.Models;
using BanAppealManager.Main.Scrapers.SS14Admin;
using BanAppealManager.Main.Summarizers;
using Microsoft.Playwright;

namespace BanAppealManager.Main.Scrapers.Forums.Topic.BanDiscussions
{
    public class BanAppealDiscussionScraper
    {
        private readonly AuthHandler _authHandler;
        private readonly GPTConnector _gptConnector;
        private readonly string _discussionUrl;
        

        public BanAppealDiscussionScraper(AuthHandler authHandler, string discussionUrl, GPTConnector gptConnector)
        {
            _authHandler = authHandler;
            _discussionUrl = discussionUrl;
            _gptConnector = gptConnector;
        }

        public async Task<BanDiscussionData> ScrapeDiscussionDataAsync()
        {
            var page = await _authHandler.EnsureAuthenticatedAsync(_discussionUrl, isForum: true);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Username
            var playerInfoParagraph = await page.Locator("article#post_1 p:has-text('Player Username:')").TextContentAsync();
            var username = ExtractUsername(playerInfoParagraph);
            
            //Ban details
            var banType = await ExtractStrongTagContent(page, "article#post_1 p", "Ban type:");
            var banLength = await ExtractStrongTagContent(page, "article#post_1 p", "Ban Length:");
            var timeServed = await ExtractStrongTagContent(page, "article#post_1 p", "Ban time Served so far:");

            
            //Linkouts
            var currentAppeal = await page.Locator("article#post_1 a:has-text('Current Appeal')").GetAttributeAsync("href");
            
            //Votes
            var totalVotesText = await page.Locator("div.poll-info_counts-count > span.info-number").TextContentAsync();
            var totalVotes = int.Parse(totalVotesText);
            var voteOptions = await ScrapeVoteOptionsAsync(page, totalVotes);
            
            //Voting Admins
            var votingAdminList = voteOptions.SelectMany(votes => votes.Voters).Distinct().ToList();
            
            //Comments
            var discussionComments = await ScrapeDiscussionCommentsAsync(page);

            await page.CloseAsync();

            // Construct the BanDiscussionData object
            var discussionData = new BanDiscussionData
            {
                Username = username,
                BanType = banType,
                BanLength = banLength,
                TimeServed = timeServed,
                CurrentAppeal = currentAppeal,
                TotalVotes = totalVotes,
                VoteOptions = voteOptions,
                VotingAdmins = votingAdminList,
                DiscussionComments = discussionComments
            };

            return discussionData;
        }

        private string ExtractUsername(string playerInfoParagraph)
        {
            var match = Regex.Match(playerInfoParagraph, @"Player Username:\s*(\w+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
        private async Task<string> ExtractStrongTagContent(IPage page, string selector, string searchText)
        {
            var element = page.Locator($"{selector}:has-text('{searchText}')").First;

            if (element == null)
            {
                return string.Empty;
            }

            var elementContent = await element.InnerHTMLAsync();

            var match = Regex.Match(elementContent, @$"{Regex.Escape(searchText)}.*?<strong>(.*?)<\/strong>");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
        
        private async Task<List<VoteOption>> ScrapeVoteOptionsAsync(IPage page, int totalVotes)
        {
            var options = new List<VoteOption>();
            var voteElements = await page.QuerySelectorAllAsync("ul.results > li");

            foreach (var element in voteElements)
            {
                var optionText = await element.QuerySelectorAsync("span.option-text");
                var percentage = await element.QuerySelectorAsync("span.percentage");
                var voterElements = await element.QuerySelectorAllAsync("ul.poll-voters-list > li > img");

                var optionTextContent = await optionText.TextContentAsync();
                var percentageTextContent = await percentage.TextContentAsync();

                var voters = new List<string>();
                foreach (var voter in voterElements)
                {
                    var src = await voter.GetAttributeAsync("src");
                    var adminName = ExtractAdminNameFromImageUrl(src);
                    if (!string.IsNullOrEmpty(adminName))
                    {
                        voters.Add(adminName);
                    }
                }

                var percentageValue = int.Parse(percentageTextContent.Replace("%", ""));
                var count = (int)Math.Round((percentageValue / 100.0) * totalVotes);

                options.Add(new VoteOption
                {
                    OptionText = optionTextContent,
                    Type = ClassifyOptionText(optionTextContent),
                    Percentage = percentageValue,
                    VoteCount = count,
                    Voters = voters
                });
            }

            return options;
        }

        private string ExtractAdminNameFromImageUrl(string url)
        {
            var match = Regex.Match(url,
                @"user_avatar/forum\.spacestation14\.com/([^/]+)/");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private string ClassifyOptionText(string optionText)
        {
            if (optionText.Contains("Remove Ban", StringComparison.OrdinalIgnoreCase))
                return "Remove";
            if (optionText.Contains("Upgrade to Voucher", StringComparison.OrdinalIgnoreCase))
                return "Upgrade";
            if (optionText.Contains("Reduce Ban", StringComparison.OrdinalIgnoreCase))
                return "Reduce";
            if (optionText.Contains("Maintain Ban", StringComparison.OrdinalIgnoreCase))
                return "Maintain";
            if (optionText.Contains("Deny Appeal", StringComparison.OrdinalIgnoreCase))
                return "Deny";

            return "Other";
        }

        private async Task<List<DiscussionComment>> ScrapeDiscussionCommentsAsync(IPage page)
        {
            var comments = new List<DiscussionComment>();
            var commentElements = await page.QuerySelectorAllAsync("div.topic-post");
            
            // Skip the first element as its the topic post
            var commentElementsToProcess = commentElements.Skip(1);

            foreach (var element in commentElementsToProcess)
            {
                var adminNameElement = await element.QuerySelectorAsync("span.username > a");
                var commentElement = await element.QuerySelectorAsync("div.cooked");

                var adminName = adminNameElement != null ? await adminNameElement.TextContentAsync() : string.Empty;
                var comment = commentElement != null ? await commentElement.TextContentAsync() : string.Empty;

                if (!string.IsNullOrWhiteSpace(comment))
                {
                    // Analyze the comment using GPTConnector
                    var analysisResult = await _gptConnector.AnalyzeComment(comment);

                    // Add the comment with the derived fields
                    comments.Add(new DiscussionComment
                    {
                        AdminName = adminName,
                        VoteType = analysisResult.CommentVoteType,
                        HasComment = true,
                        Comment = comment,
                        CommentReasons = analysisResult.VotingReasons ?? new List<string>(),
                        CommentSentiment = analysisResult.CommentSentiment,
                        ReductionLengthTimeInWeeks = analysisResult.ReductionLengthTimeInWeeks,
                        DiscussionOrDecision = analysisResult.DiscussionOrDecision
                    });
                }
                else
                {
                    // Add the comment without analysis
                    comments.Add(new DiscussionComment
                    {
                        AdminName = adminName,
                        VoteType = string.Empty,
                        HasComment = false,
                        Comment = string.Empty,
                        CommentReasons = new List<string>(),
                        CommentSentiment = string.Empty,
                        ReductionLengthTimeInWeeks = 0
                    });
                }
            }

            return comments;
        }
    }
}