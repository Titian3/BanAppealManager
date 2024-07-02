using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BanAppealManager.Main.Models;

namespace BanAppealManager.Main.Scrapers.SS14Admin
{
    public class RoleBanScraper
    {
        public async Task<List<RoleBanDetails>> ExtractRoleBanDetailsAsync(IPage page)
        {
            var roleBanDetails = new List<RoleBanDetails>();
            await page.WaitForSelectorAsync("h2:has-text('Role Bans') + table");
            var roleBanSection = page.Locator("h2:has-text('Role Bans') + table");
            var roleBanRows = roleBanSection.Locator("tbody tr");
            var roleBanRowCount = await roleBanRows.CountAsync();

            for (int i = 0; i < roleBanRowCount; i++)
            {
                var row = roleBanRows.Nth(i);
                var reason = await row.Locator("td:nth-child(2)").TextContentAsync();
                var role = await row.Locator("td:nth-child(3)").TextContentAsync();
                var banTime = DateTime.Parse(await row.Locator("td:nth-child(4)").TextContentAsync());
                var roundNumber = await row.Locator("td:nth-child(5)").TextContentAsync();
                var expireTimeText = await row.Locator("td:nth-child(6)").TextContentAsync();

                var isPermanent = false;
                var isActive = true;
                var isExpired = false;
                DateTime? expireTime = null;
                DateTime? unbanTime = null;
                string unbannedBy = null;

                // Logic to determine ban status
                if (expireTimeText.Contains("PERMANENT"))
                {
                    isPermanent = true;
                    if (expireTimeText.Contains("Unbanned"))
                    {
                        isActive = false;
                        var unbanInfo = expireTimeText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (DateTime.TryParse(unbanInfo[1].Replace("Unbanned:", "").Trim().Split(' ')[0],
                                out var parsedUnbanTime))
                        {
                            unbanTime = parsedUnbanTime;
                        }

                        unbannedBy = unbanInfo[1].Split(' ').Last();
                    }
                }
                else if (expireTimeText.Contains("Expired"))
                {
                    isActive = false;
                    isExpired = true;
                    if (DateTime.TryParse(expireTimeText.Split('\n')[0].Trim(), out var parsedExpireTime))
                    {
                        expireTime = parsedExpireTime;
                    }
                }
                else if (expireTimeText.Contains("Unbanned"))
                {
                    isActive = false;
                    var unbanInfo = expireTimeText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (DateTime.TryParse(unbanInfo[0].Trim(), out var parsedExpireTime))
                    {
                        expireTime = parsedExpireTime;
                    }

                    if (DateTime.TryParse(unbanInfo[1].Replace("Unbanned:", "").Trim().Split(' ')[0],
                            out var parsedUnbanTime))
                    {
                        unbanTime = parsedUnbanTime;
                    }

                    unbannedBy = unbanInfo[1].Split(' ').Last();
                }
                else
                {
                    if (DateTime.TryParse(expireTimeText.Trim(), out var parsedExpireTime))
                    {
                        expireTime = parsedExpireTime;
                    }
                }

                roleBanDetails.Add(new RoleBanDetails
                {
                    Reason = reason,
                    Role = role,
                    BanTime = banTime,
                    RoundNumber = roundNumber,
                    IsPermanent = isPermanent,
                    IsActive = isActive,
                    IsExpired = isExpired,
                    ExpireTime = expireTime,
                    UnbanTime = unbanTime,
                    UnbannedBy = unbannedBy
                });
            }

            return roleBanDetails;
        }
    }
}
