using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace BanAppealManager.Main.Scrapers.Forums.Category
{
    public class ForumBanAppealSummary
    {
        private readonly IBrowser _browser;

        public ForumBanAppealSummary(IBrowser browser)
        {
            _browser = browser;
        }

        public async Task<List<AppealSummary>> GetAppealSummariesAsync()
        {
            var context = await _browser.NewContextAsync();
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(5000);
            await page.GotoAsync("https://forum.spacestation14.com/c/ban-appeals/appeals-game/40/l/latest?assigned=nobody&status=open");

            var appeals = new List<AppealSummary>();

            var rows = await page.QuerySelectorAllAsync("table.topic-list tr.topic-list-item");

            foreach (var row in rows)
            {
                var postName = await ExtractTextAsync(row, "a.title.raw-link.raw-topic-link");
                if (postName == "About the Game Servers category") continue; // Exclude the info appeal

                var solutionStatus = await ExtractAttributeAsync(row, "span.topic-status", "title");
                if (solutionStatus != null && solutionStatus.Contains("This topic has a solution")) continue; // Exclude topics with solutions

                var createdTitle = await ExtractAttributeAsync(row, "td.num.topic-list-data.age.activity", "title");
                var linkToAppeal = await ExtractAttributeAsync(row, "a.title.raw-link.raw-topic-link", "href");
                var originalPoster = await ExtractOriginalPosterAsync(row, "td.posters.topic-list-data a img");

                if (!string.IsNullOrEmpty(linkToAppeal))
                {
                    linkToAppeal = "https://forum.spacestation14.com" + linkToAppeal;
                }

                var createdDate = ExtractCreatedDate(createdTitle);

                appeals.Add(new AppealSummary
                {
                    PostName = postName,
                    Created = createdDate,
                    LinkToAppeal = linkToAppeal,
                    OriginalPoster = originalPoster
                });
            }

            await page.CloseAsync();
            await context.CloseAsync();

            return appeals;
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

        private async Task<string> ExtractOriginalPosterAsync(IElementHandle element, string selector)
        {
            var children = await element.QuerySelectorAllAsync(selector);
            foreach (var child in children)
            {
                var title = await child.GetAttributeAsync("title");
                if (title != null && title.Contains("Original Poster"))
                {
                    var parent = await child.QuerySelectorAsync("..");
                    return await parent.GetAttributeAsync("data-user-card");
                }
            }
            return "Unknown";
        }
    }

    public class AppealSummary
    {
        public string PostName { get; set; }
        public string Created { get; set; }
        public string LinkToAppeal { get; set; }
        public string OriginalPoster { get; set; }
    }
}
