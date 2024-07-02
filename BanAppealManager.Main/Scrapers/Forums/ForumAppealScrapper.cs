using System.Text;
using BanAppealManager.Main.Models;
using Microsoft.Playwright;

namespace BanAppealManager.Main.Scrapers.Forums
{
    public class ForumAppealScraper
    {
        private readonly IBrowser _browser;

        public ForumAppealScraper(IBrowser browser)
        {
            _browser = browser;
        }

        public async Task<AppealData> ScrapeAppealData(string appealUrl)
        {
            var context = await _browser.NewContextAsync();
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(5000);
            await page.GotoAsync(appealUrl);

            var appealData = new AppealData
            {
                AppealURL = appealUrl
            };

            appealData.Username = await ExtractTextAsync(page, "strong:has-text(\"Username:\") + code");
            appealData.BanLength = await ExtractTextAsync(page, "strong:has-text(\"Length of ban:\") + code");
            appealData.BanReason = await ExtractTextAsync(page, "strong:has-text(\"Ban reason:\") + code");
            appealData.BanIssue = await ExtractTextWithBreakAsync(page, "strong:has-text(\"Ban Issue:\")", "code");
            appealData.BanPretext =
                await ExtractTextBetweenHeadersAsync(page, "Events leading to the ban", "Reason the ban should be removed");
            appealData.BanVotePref = await ExtractTextAsync(page, "strong:has-text(\"Vote Opt-Out:\") + code");
            appealData.BanAppealReason =
                await ExtractTextBetweenHeadersAsync(page, "Reason the ban should be removed", "Alternate Accounts");
            appealData.AlternateAccounts = await ExtractTextAsync(page, "h2:has-text(\"Alternate Accounts\") + p");

            // Determine the ban type
            appealData.BanType = await DetermineBanTypeAsync(page);

            await page.CloseAsync();
            await context.CloseAsync();

            return appealData;
        }

        private async Task<string> ExtractTextAsync(IPage page, string selector)
        {
            try
            {
                var element = await page.Locator(selector).ElementHandleAsync();
                return element != null ? await element.InnerTextAsync() : "Not found";
            }
            catch (Exception)
            {
                return "Not found";
            }
        }

        private async Task<string> ExtractTextWithBreakAsync(IPage page, string parentSelector, string siblingSelector)
        {
            try
            {
                var parentElement = page.Locator(parentSelector);
                if (await parentElement.CountAsync() == 0)
                {
                    return "Not found";
                }

                // Use XPath to find the following sibling code tag after br
                var siblingElement = page.Locator($"{parentSelector} + br + {siblingSelector}");
                if (await siblingElement.CountAsync() == 0)
                {
                    return "Not found";
                }

                return await siblingElement.InnerTextAsync();
            }
            catch (Exception)
            {
                return "Not found";
            }
        }

        private async Task<string> ExtractTextBetweenHeadersAsync(IPage page, string startHeader, string endHeader)
        {
            var textBuilder = new StringBuilder();
            var parentLocator = page.Locator("div.cooked");
            var children = await parentLocator.Locator(":scope > *").AllAsync();

            bool withinRange = false;
            foreach (var child in children)
            {
                var tagName = await child.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
                if (tagName == "h2")
                {
                    var headerText = await child.InnerTextAsync();
                    if (headerText.Contains(startHeader))
                    {
                        withinRange = true;
                        continue;
                    }

                    if (headerText.Contains(endHeader))
                    {
                        withinRange = false;
                        break;
                    }
                }

                if (withinRange && tagName == "p")
                {
                    var textContent = await child.InnerTextAsync();
                    textBuilder.AppendLine(textContent);
                }
            }

            return textBuilder.ToString().Trim();
        }

        private async Task<string> DetermineBanTypeAsync(IPage page)
        {
            try
            {
                var topicTitleContainer = page.Locator("#topic-title");

                var serverBanTag = topicTitleContainer.Locator("a[data-tag-name='appeal-server-ban']");
                if (await serverBanTag.CountAsync() > 0)
                {
                    return "Game Ban";
                }

                var roleBanTag = topicTitleContainer.Locator("a[data-tag-name='appeal-role-ban']");
                if (await roleBanTag.CountAsync() > 0)
                {
                    return "Role Ban";
                }

                return "Unknown";
            }
            catch (Exception)
            {
                return "Unknown";
            }
        }
    }
}
