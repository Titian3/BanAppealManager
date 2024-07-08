using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using BanAppealManager.Main.Scrapers.SS14Admin;

namespace BanAppealManager.Main.Scrapers.Forums.Category
{
    public class ForumBanAppealDiscussionSummary
    {
        private readonly IBrowser _browser;
        private readonly AuthHandler _authHandler;

        private const string DiscussionUrl =
            "https://forum.spacestation14.com/c/ban-appeals-internal/game-servers/42?solved=no";

        public ForumBanAppealDiscussionSummary(IBrowser browser, AuthHandler authHandler)
        {
            _browser = browser;
            _authHandler = authHandler;
        }

        public async Task<List<DiscussionSummary>> GetDiscussionSummariesAsync()
        {
            var page = await _authHandler.EnsureAuthenticatedAsync(DiscussionUrl, isForum: true);
            await page.WaitForURLAsync(DiscussionUrl);
            var discussions = new List<DiscussionSummary>();

            var rows = await page.QuerySelectorAllAsync("table.topic-list tr.topic-list-item");

            foreach (var row in rows)
            {
                var postName = await ExtractTextAsync(row, "a.title.raw-link.raw-topic-link");
                if (postName == "About the Game Servers category") continue; // Exclude the info appeal

                var solutionStatus = await ExtractAttributeAsync(row, "span.topic-status", "title");
                if (solutionStatus != null && solutionStatus.Contains("This topic has a solution")) continue; // Exclude topics with solutions

                var createdTitle = await ExtractAttributeAsync(row, "td.num.topic-list-data.age.activity", "title");
                var linkToDiscussion = await ExtractAttributeAsync(row, "a.title.raw-link.raw-topic-link", "href");
                var replies = await ExtractRepliesAsync(row, "td.num.posts-map.posts.topic-list-data");

                if (!string.IsNullOrEmpty(linkToDiscussion))
                {
                    linkToDiscussion = "https://forum.spacestation14.com" + linkToDiscussion;
                }

                var createdDate = ExtractCreatedDate(createdTitle);

                discussions.Add(new DiscussionSummary
                {
                    PostName = postName,
                    Replies = replies,
                    Created = createdDate,
                    LinkToDiscussion = linkToDiscussion
                });
            }

            await page.CloseAsync();
            await page.Context.CloseAsync();

            return discussions;
        }

        private string ExtractCreatedDate(string title)
        {
            if (string.IsNullOrEmpty(title)) return "Unknown";

            var match = Regex.Match(title, @"Created:\s*(.+?)(?:\n|$)");
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private async Task<string> ExtractTextAsync(IElementHandle element, string selector)
        {
            try
            {
                var child = await element.QuerySelectorAsync(selector);
                return child != null ? await child.InnerTextAsync() : "Not found";
            }
            catch (Exception)
            {
                return "Not found";
            }
        }

        private async Task<string> ExtractAttributeAsync(IElementHandle element, string selector, string attribute)
        {
            try
            {
                var child = await element.QuerySelectorAsync(selector);
                return child != null ? await child.GetAttributeAsync(attribute) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int> ExtractRepliesAsync(IElementHandle element, string selector)
        {
            try
            {
                var text = await ExtractTextAsync(element, selector);
                return int.TryParse(text, out var replies) ? replies : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }

    public class DiscussionSummary
    {
        public string PostName { get; set; }
        public int Replies { get; set; }
        public string Created { get; set; }
        public string LinkToDiscussion { get; set; }
    }
}
