using BanAppealManager.Main.Models;
using Microsoft.Playwright;

namespace BanAppealManager.Main.Scrapers.SS14Admin
{
    public class SS14AdminScraperallinone
    {
        private string _adminUsername;
        private string _adminPassword;
        private IBrowser _browser;
        private IBrowserContext _context;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public SS14AdminScraperallinone(string adminUsername, string adminPassword, IBrowser browser)
        {
            _adminUsername = adminUsername;
            _adminPassword = adminPassword;
            _browser = browser;
        }

        private async Task SaveAuthStateAsync()
        {
            var storageState = await _context.StorageStateAsync();
            await File.WriteAllTextAsync("authState.json", storageState);
        }

        private async Task LoadAuthStateAsync()
        {
            if (File.Exists("authState.json"))
            {
                var storageState = await File.ReadAllTextAsync("authState.json");
                _context = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    StorageState = storageState
                });
            }
            else
            {
                _context = await _browser.NewContextAsync();
            }
        }

        public async Task InitializeAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_context == null)
                {
                    await LoadAuthStateAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Userdetails> GetUserDetailsAsync(string userID)
        {
            await _semaphore.WaitAsync();
            try
            {
                var page = await _context.NewPageAsync();
                await page.GotoAsync($"https://ss14-admin.spacestation14.com/Players/Info/{userID}");

                if (page.Url.Contains("/Identity/Account/Login"))
                {
                    await page.FillAsync("#Input_EmailOrUsername", _adminUsername);
                    await page.FillAsync("#Input_Password", _adminPassword);
                    await page.CheckAsync("#Input_RememberMe");
                    await page.ClickAsync("button[type='submit']");

                    if (await page.Locator("text='Two-factor authentication'").CountAsync() > 0)
                    {
                        Console.WriteLine("Enter the 2FA code:");
                        var twoFactorCode = Console.ReadLine();
                        await page.FillAsync("#Input_TwoFactorCode", twoFactorCode);
                        await page.CheckAsync("#Input_RememberMachine");
                        await page.ClickAsync("button[type='submit']");
                        await SaveAuthStateAsync();
                    }

                    await page.GotoAsync($"https://ss14-admin.spacestation14.com/Players/Info/{userID}");
                }

                double playtimeOverall = 0;
                try
                {
                    await page.WaitForSelectorAsync("td:has-text('Overall')");
                    playtimeOverall = ExtractPlaytime(await page.InnerHTMLAsync("tbody"));
                }
                catch
                {
                    // Handle the case where the playtime table is not found
                    playtimeOverall = 0;
                }

                var username = await page.Locator("dt:has-text('Last seen username:') + dd").TextContentAsync();
                var firstSeenText = await page.Locator("dt:has-text('First seen time:') + dd").TextContentAsync();
                var firstSeen = DateTime.Parse(firstSeenText).Date;
                var whitelistedText = await page.Locator("dt:has-text('Whitelisted:') + dd").TextContentAsync();
                var whitelisted = whitelistedText.Contains("✔️ yes");
                var playtimeHtml = await page.InnerHTMLAsync("tbody");
                var adminPageUrl = $"https://ss14-admin.spacestation14.com/Players/Info/{userID}";
                var banlist = await ExtractBanDetailsAsync(page);
                var roleBanlist = await ExtractRoleBanDetailsAsync(page);
                var notes = await ExtractNotesAsync(page);

                var userDetails = new Userdetails
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

                await page.CloseAsync();
                return userDetails;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<List<BanDetails>> ExtractBanDetailsAsync(IPage page)
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
                var banHits = await ExtractBanHitsAsync(page, banHitsLink);

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


        private async Task<List<RoleBanDetails>> ExtractRoleBanDetailsAsync(IPage page)
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

        // Add a method to classify and group role bans
        public Dictionary<string, List<string>> ClassifyRoleBans(List<RoleBanDetails> roleBanDetails)
        {
            var departmentRoleMapping = new Dictionary<string, List<string>>
            {
                {
                    "Service",
                    new List<string>
                    {
                        "Janitor", "Botanist", "Bartender", "Chef", "Clown", "Mime", "ServiceWorker", "Chaplain",
                        "Musician", "Borg", "Lawyer", "Librarian", "Reporter", "Boxer", "Zookeeper", "Passenger"
                    }
                },
                { "Science", new List<string> { "Scientist", "ResearchAssistant", "SeniorResearcher" } },
                {
                    "Engineering",
                    new List<string>
                        { "StationEngineer", "AtmosphericTechnician", "TechnicalAssistant", "SeniorEngineer" }
                },
                { "Cargo", new List<string> { "SalvageSpecialist", "CargoTechnician" } },
                {
                    "Medical",
                    new List<string>
                        { "MedicalDoctor", "Chemist", "MedicalIntern", "Paramedic", "Psychologist", "SeniorPhysician" }
                },
                {
                    "Command",
                    new List<string>
                    {
                        "Captain", "ChiefMedicalOfficer", "HeadOfPersonnel", "Quartermaster", "HeadOfSecurity",
                        "ResearchDirector", "ChiefEngineer", "CentralCommandOfficial"
                    }
                },
                {
                    "Security",
                    new List<string>
                        { "SecurityOfficer", "SecurityCadet", "Warden", "Detective", "SeniorOfficer", "Brigmedic" }
                }
            };

            var groupedRoleBans = new Dictionary<string, List<string>>();

            foreach (var roleBan in roleBanDetails)
            {
                foreach (var department in departmentRoleMapping)
                {
                    if (department.Value.Contains(roleBan.Role))
                    {
                        if (!groupedRoleBans.ContainsKey(department.Key))
                        {
                            groupedRoleBans[department.Key] = new List<string>();
                        }

                        groupedRoleBans[department.Key].Add(roleBan.Role);
                    }
                }
            }

            return groupedRoleBans;
        }

        private async Task<List<Banhits>> ExtractBanHitsAsync(IPage page, string banHitsLink)
        {
            if (string.IsNullOrEmpty(banHitsLink))
            {
                return new List<Banhits>();
            }

            var banHitsPage = await _context.NewPageAsync();
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

        private async Task<List<NoteDetails>> ExtractNotesAsync(IPage page)
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
            var dateTimeMatch =
                System.Text.RegularExpressions.Regex.Match(dateTimeText, @"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}");
            if (dateTimeMatch.Success)
            {
                return DateTime.Parse(dateTimeMatch.Value);
            }

            return DateTime.MinValue;
        }

        private double ExtractPlaytime(string playtimeHtml)
        {
            try
            {
                double totalPlaytime = 0;
                var playtimeStrings = playtimeHtml.Split(new[] { "<td class=\"font-weight-bold\">Overall</td>" },
                    StringSplitOptions.None);

                if (playtimeStrings.Length > 1)
                {
                    var timeString = playtimeStrings[1].Split(new[] { "<td>" }, StringSplitOptions.None)[1]
                        .Split(new[] { "</td>" }, StringSplitOptions.None)[0];
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

        public IBrowserContext GetContext()
        {
            return _context;
        }

        // Method to calculate the ban time served so far
        public int CalculateBanTimeServed(Userdetails userDetails)
        {
            var oldestActiveBan = userDetails.Banlist
                .Where(ban => ban.IsActive)
                .OrderBy(ban => ban.BanTime) // Sort by BanTime in ascending order
                .FirstOrDefault();

            if (oldestActiveBan != null)
            {
                return (DateTime.Now - oldestActiveBan.BanTime).Days;
            }

            return 0;
        }

        public int CalculateRoleBanTimeServed(Userdetails userDetails)
        {
            var oldestRoleBan = userDetails.RoleBanlist
                .Where(ban => ban.IsActive || ban.IsExpired)
                .OrderBy(b => b.BanTime) // Sort by BanTime in ascending order
                .FirstOrDefault();

            if (oldestRoleBan != null)
            {
                return (DateTime.Now - oldestRoleBan.BanTime).Days;
            }

            return 0;
        }

        public string GetLatestBanReason(Userdetails userDetails)
        {
            var oldestBan = userDetails.Banlist
                .OrderBy(ban => ban.BanTime) // Sort by BanTime in ascending order
                .FirstOrDefault();

            return oldestBan != null ? oldestBan.Reason : "No ban reason found.";
        }

        public string GetLatestRoleBanReason(Userdetails userDetails)
        {
            var oldestRoleBan = userDetails.RoleBanlist
                .Where(ban => ban.IsActive || ban.IsExpired)
                .OrderBy(ban => ban.BanTime) // Sort by BanTime in ascending order
                .FirstOrDefault();

            return oldestRoleBan?.Reason ?? "No active or expired role bans found.";
        }

        public string GetLatestRoundNumber(Userdetails userDetails)
        {
            var oldestBan = userDetails.Banlist
                .OrderBy(ban => ban.BanTime) // Sort by BanTime in ascending order
                .FirstOrDefault();

            return oldestBan != null ? oldestBan.RoundNumber : "No round number found.";
        }

        public string GetLatestRoleRoundNumber(Userdetails userDetails)
        {
            var oldestRoleBan = userDetails.RoleBanlist
                .Where(ban => ban.IsActive || ban.IsExpired)
                .OrderBy(ban => ban.BanTime) // Sort by BanTime in ascending order
                .FirstOrDefault();

            return oldestRoleBan?.RoundNumber ?? "No active or expired role round number found.";
        }
    }
}