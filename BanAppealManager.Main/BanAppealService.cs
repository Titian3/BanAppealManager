using BanAppealManager.Main.API;
using BanAppealManager.Main.ResponseTemplates;
using BanAppealManager.Main.Scrapers.Forums;
using BanAppealManager.Main.Scrapers.Forums.Category;
using BanAppealManager.Main.Scrapers.Forums.Topic;
using BanAppealManager.Main.Scrapers.SS14Admin;
using BanAppealManager.Main.Summarizers;

namespace BanAppealManager.Main
{
    public class BanAppealService
    {
        private readonly UsernameLookupService _usernameLookupService;
        private readonly SS14AdminScraper _ss14AdminScraper;
        private readonly ForumBanAppealScraper _forumBanAppealScraper;
        private readonly GPTConnector _gptConnector;
        private readonly ForumBanAppealSummary _forumBanAppealSummary;
        private int? _cachedAppealsCount;
        private int? _cachedDiscussionsCount;
        private DateTime _lastScrapeTimeAppeals;
        private DateTime _lastScrapeTimeDiscussions;

        public BanAppealService(SS14AdminScraper ss14AdminScraper, ForumBanAppealScraper forumBanAppealScraper, ForumBanAppealSummary forumBanAppealSummary, string apiKey)
        {
            _usernameLookupService = new UsernameLookupService();
            _ss14AdminScraper = ss14AdminScraper;
            _forumBanAppealScraper = forumBanAppealScraper;
            _forumBanAppealSummary = forumBanAppealSummary;
            _gptConnector = new GPTConnector(apiKey);
        }

        public async Task<string> ProcessAppealAsync(string appealUrl, string ahelpMessages, string ahelpLink)
        {
            return await ProcessAppealInternalAsync(appealUrl, ahelpMessages, ahelpLink);
        }

        public async Task ProcessAppealAsync(string appealUrl)
        {
            var template = await ProcessAppealInternalAsync(appealUrl, "No aHelp occurred.", "No aHelp");
            Console.WriteLine(template);
            System.Console.WriteLine(template);
        }

        private async Task<string> ProcessAppealInternalAsync(string appealUrl, string ahelpMessages, string ahelpLink)
        {
            Console.WriteLine("Starting appeal processing...");
            Console.WriteLine($"Appeal URL: {appealUrl}");
            Console.WriteLine("Scraping appeal data...");
            var appealData = await _forumBanAppealScraper.ScrapeAppealData(appealUrl);
            Console.WriteLine($"Appeal data scraped successfully. Username extracted: {appealData.Username}");

            Console.WriteLine("Looking up user information...");
            var userData = await _usernameLookupService.GetUserDataAsync(appealData.Username);
            Console.WriteLine($"User information retrieved: {appealData.Username} - {userData.UserId}");

            Console.WriteLine("Retrieving user details from SS14 admin scraper...");
            var userDetails = await _ss14AdminScraper.GetUserDetailsAsync(userData.UserId);
            Console.WriteLine("User details retrieved successfully.");

            Console.WriteLine("Grouping role bans...");
            userDetails.GroupedRoleBans = RoleBanGrouper.GroupRoleBans(userDetails.RoleBanlist);
            Console.WriteLine("Role bans grouped successfully.");

            Console.WriteLine($"Preparing summarization for user: {userDetails.Username}...");
            var gptResponse = await _gptConnector.GetSummaryAsync(appealData, userDetails, ahelpMessages);
            Console.WriteLine("Summarization completed successfully.");

            Console.WriteLine("Generating appeal vote template...");
            string template = ProcessAppealTemplate.Generate(appealData, userDetails, gptResponse, ahelpLink);
            Console.WriteLine("Appeal vote template generated successfully.");

            Console.WriteLine("Appeal processing completed.");
            return template;
        }
        
        public async Task<List<AppealSummary>> GetOutstandingAppealsAsync()
        {
            return await _forumBanAppealSummary.GetAppealSummariesAsync();
        }
    }
}
