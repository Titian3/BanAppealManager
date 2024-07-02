using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BanAppealManager.Main.Models;

namespace BanAppealManager.Main.Scrapers.SS14Admin
{
    public class BanScraper
    {
        public async Task<List<BanDetails>> ExtractBanDetailsAsync(IPage page)
        {
            var banDetails = new List<BanDetails>();
            await page.WaitForSelectorAsync("h2:has-text('Bans') + table");
            var banSection = page.Locator("h2:has-text('Bans') + table");
            var banRows = banSection.Locator("tbody tr");
            var banRowCount = await banRows.CountAsync();

            for (int i = 0; i < banRowCount; i++)
            {
                var row = banRows.Nth(i);
                var reason = await row.Locator("td:nth-child(2)").TextContentAsync();
                var banTimeText = await row.Locator("td:nth-child(3)").TextContentAsync();
                var roundNumber = await row.Locator("td:nth-child(4)").TextContentAsync();

                // Short-circuit if banTimeText starts with "Job:"
                if (banTimeText.StartsWith("Job:"))
                {
                    break;
                }

                var banTime = DateTime.Parse(banTimeText);
                var expireTimeText = await row.Locator("td:nth-child(5)").TextContentAsync();

                var isPermanent = false;
                var isActive = true;
                var isExpired = false;
                DateTime? expireTime = null;
                DateTime? unbanTime = null;
                string unbannedBy = null;

                // Check if the ban is permanent
                if (expireTimeText.Contains("PERMANENT"))
                {
                    isPermanent = true;

                    // Check if the permanent ban was unbanned
                    if (expireTimeText.Contains("Unbanned"))
                    {
                        isActive = false;
                        var unbanInfo = expireTimeText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        // Try to parse the unban time
                        if (DateTime.TryParse(unbanInfo[1].Replace("Unbanned:", "").Trim().Split(' ')[0],
                                out var parsedUnbanTime))
                        {
                            unbanTime = parsedUnbanTime;
                        }

                        // Extract the admin name who unbanned
                        unbannedBy = unbanInfo[1].Split(' ').Last();
                    }
                }
                // Check if the ban has expired
                else if (expireTimeText.Contains("Expired"))
                {
                    isActive = false;
                    isExpired = true;

                    // Try to parse the expire time
                    if (DateTime.TryParse(expireTimeText.Split('\n')[0].Trim(), out var parsedExpireTime))
                    {
                        expireTime = parsedExpireTime;
                    }
                }
                // Check if the ban was unbanned before its expiration
                else if (expireTimeText.Contains("Unbanned"))
                {
                    isActive = false;
                    var unbanInfo = expireTimeText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    // Try to parse the expire time
                    if (DateTime.TryParse(unbanInfo[0].Trim(), out var parsedExpireTime))
                    {
                        expireTime = parsedExpireTime;
                    }

                    // Try to parse the unban time
                    if (DateTime.TryParse(unbanInfo[1].Replace("Unbanned:", "").Trim().Split(' ')[0],
                            out var parsedUnbanTime))
                    {
                        unbanTime = parsedUnbanTime;
                    }

                    // Extract the admin name who unbanned
                    unbannedBy = unbanInfo[1].Split(' ').Last();
                }
                // Check if the ban is still active and has a set expiration date
                else
                {
                    // Try to parse the expire time
                    if (DateTime.TryParse(expireTimeText.Trim(), out var parsedExpireTime))
                    {
                        expireTime = parsedExpireTime;
                    }
                }

                // Extract the link for ban hits and retrieve details
                var banHitsLink = await row.Locator("td:nth-child(7) a").GetAttributeAsync("href");
                var banHits = await ExtractBanHitsAsync(page.Context, banHitsLink);

                // Add the ban details to the list
                banDetails.Add(new BanDetails
                {
                    Reason = reason,
                    BanTime = banTime,
                    RoundNumber = roundNumber,
                    IsPermanent = isPermanent,
                    IsActive = isActive,
                    IsExpired = isExpired,
                    ExpireTime = expireTime,
                    UnbanTime = unbanTime,
                    UnbannedBy = unbannedBy,
                    Banhits = banHits
                });
            }

            return banDetails;
        }

        private async Task<List<Banhits>> ExtractBanHitsAsync(IBrowserContext context, string banHitsLink)
        {
            if (string.IsNullOrEmpty(banHitsLink))
            {
                return new List<Banhits>();
            }

            var banHitsPage = await context.NewPageAsync();
            await banHitsPage.GotoAsync($"https://ss14-admin.spacestation14.com{banHitsLink}");

            var banHits = new List<Banhits>();
            var hitsRows = banHitsPage.Locator("table tbody tr");

            for (int i = 0; i < await hitsRows.CountAsync(); i++)
            {
                var hitRow = hitsRows.Nth(i);
                var username = await hitRow.Locator("td:nth-child(1)").TextContentAsync();
                var userId = Guid.Parse(await hitRow.Locator("td:nth-child(2)").TextContentAsync());
                var time = DateTime.Parse(await hitRow.Locator("td:nth-child(3)").TextContentAsync());
                var ipAddress = await hitRow.Locator("td:nth-child(4)").TextContentAsync();
                var hwid = await hitRow.Locator("td:nth-child(5)").TextContentAsync();

                banHits.Add(new Banhits
                {
                    Username = username,
                    UserId = userId,
                    Time = time,
                    IpAddress = ipAddress,
                    HWID = hwid
                });
            }

            await banHitsPage.CloseAsync();
            return banHits;
        }
    }
}
