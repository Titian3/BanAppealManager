using Microsoft.Playwright;
using System;
using System.Threading;
using System.Threading.Tasks;
using BanAppealManager.Main.Models;

namespace BanAppealManager.Main.Scrapers.SS14Admin
{
    public class SS14AdminScraper
    {
        private readonly AuthHandler _authHandler;
        private readonly PageScraper _pageScraper;
        private readonly BanScraper _banScraper;
        private readonly RoleBanScraper _roleBanScraper;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public SS14AdminScraper(AuthHandler authHandler)
        {
            _authHandler = authHandler;
            _pageScraper = new PageScraper();
            _banScraper = new BanScraper();
            _roleBanScraper = new RoleBanScraper();
        }

        public async Task<Userdetails> GetUserDetailsAsync(string userID)
        {
            await _semaphore.WaitAsync();
            try
            {
                Console.WriteLine($"Starting authentication for user ID: {userID}");
                var ss14UserUrl = $"https://ss14-admin.spacestation14.com/Players/Info/{userID}";
                var page = await _authHandler.EnsureAuthenticatedAsync(ss14UserUrl);
                Console.WriteLine($"Authentication successful for user ID: {ss14UserUrl}");

                Console.WriteLine($"Starting data extraction for user ID: {userID}");
                
                var userDetails = await _pageScraper.ExtractUserDetailsAsync(page, userID);
                Console.WriteLine($"Data extraction successful for user ID: {userID}");

                return userDetails;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user details for {userID}: {ex.Message}");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}