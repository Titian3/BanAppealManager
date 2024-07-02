using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BanAppealManager.Main.Models;

namespace BanAppealManager.Main.Scrapers.SS14Admin
{
    public class PageScraper
    {
        public async Task<Userdetails> ExtractUserDetailsAsync(IPage page, string userID)
        {
            double playtimeOverall = 0;
            try
            {
                await page.WaitForSelectorAsync("td:has-text('Overall')");
                playtimeOverall = ExtractPlaytime(await page.InnerHTMLAsync("tbody"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting playtime: {ex.Message}");
                playtimeOverall = 0;
            }

            var username = await page.Locator("dt:has-text('Last seen username:') + dd").TextContentAsync();
            var firstSeenText = await page.Locator("dt:has-text('First seen time:') + dd").TextContentAsync();
            var firstSeen = DateTime.Parse(firstSeenText).Date;
            var whitelistedText = await page.Locator("dt:has-text('Whitelisted:') + dd").TextContentAsync();
            var whitelisted = whitelistedText.Contains("✔️ yes");
            var adminPageUrl = $"https://ss14-admin.spacestation14.com/Players/Info/{userID}";
            var banlist = await new BanScraper().ExtractBanDetailsAsync(page);
            var roleBanlist = await new RoleBanScraper().ExtractRoleBanDetailsAsync(page);
            var notes = await ExtractNotesAsync(page);

            return new Userdetails
            {
                Username = username,
                FirstSeen = firstSeen,
                Whitelisted = whitelisted,
                PlaytimeOverall = playtimeOverall,
                AdminPageUrl = adminPageUrl,
                Banlist = banlist,
                RoleBanlist = roleBanlist,
                Notes = notes
            };
        }

        public async Task<List<NoteDetails>> ExtractNotesAsync(IPage page)
        {
            var notes = new List<NoteDetails>();

            // Check if the notes table exists
            if (await page.Locator("div.container-fluid >> text=Notes >> .. >> table tbody tr").CountAsync() > 0)
            {
                var notesRows = page.Locator("div.container-fluid >> text=Notes >> .. >> table tbody tr");

                for (int i = 0; i < await notesRows.CountAsync(); i++)
                {
                    var row = notesRows.Nth(i);
                    var type = await row.Locator("td:nth-child(1) span").TextContentAsync();
                    var message = await row.Locator("td:nth-child(2)").TextContentAsync();
                    var round = await row.Locator("td:nth-child(3)").TextContentAsync();
                    var severity = await row.Locator("td:nth-child(4) span").TextContentAsync();
                    var visible = await row.Locator("td:nth-child(5) span").TextContentAsync();
                    var playtime = await row.Locator("td:nth-child(6)").TextContentAsync();
                    var expires = await row.Locator("td:nth-child(7)").TextContentAsync();

                    var createdText = await row.Locator("td:nth-child(8)").TextContentAsync();
                    var created = ParseDateTime(createdText);

                    var editedText = await row.Locator("td:nth-child(9)").TextContentAsync();
                    var edited = ParseDateTime(editedText);

                    notes.Add(new NoteDetails
                    {
                        Type = type,
                        Message = message,
                        Round = round,
                        Severity = severity,
                        Visible = visible,
                        Playtime = playtime,
                        Expires = expires,
                        Created = created,
                        Edited = edited
                    });
                }
            }

            return notes;
        }

        private DateTime ParseDateTime(string dateTimeText)
        {
            var dateTimeMatch = System.Text.RegularExpressions.Regex.Match(dateTimeText, @"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}");
            if (dateTimeMatch.Success)
            {
                return DateTime.Parse(dateTimeMatch.Value);
            }

            return DateTime.MinValue;
        }

        public double ExtractPlaytime(string playtimeHtml)
        {
            try
            {
                double totalPlaytime = 0;
                var playtimeStrings = playtimeHtml.Split(new[] { "<td class=\"font-weight-bold\">Overall</td>" }, StringSplitOptions.None);

                if (playtimeStrings.Length > 1)
                {
                    var timeString = playtimeStrings[1].Split(new[] { "<td>" }, StringSplitOptions.None)[1].Split(new[] { "</td>" }, StringSplitOptions.None)[0];
                    var timeParts = timeString.Split(':');
                    int hours = int.Parse(timeParts[0]);
                    int minutes = int.Parse(timeParts[1]);
                    totalPlaytime = hours + (minutes / 60.0);
                    return totalPlaytime;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
