using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace BanAppealManager.Main.Scrapers.SS14Admin
{
    public class AuthHandler
    {
        private readonly string _adminUsername;
        private readonly string _adminPassword;
        private readonly IBrowser _browser;

        public AuthHandler(string adminUsername, string adminPassword, IBrowser browser)
        {
            _adminUsername = adminUsername;
            _adminPassword = adminPassword;
            _browser = browser;
        }

        private async Task<IBrowserContext> CreateOrLoadContextAsync()
        {
            if (File.Exists("authState.json"))
            {
                var storageState = await File.ReadAllTextAsync("authState.json");
                var context = await _browser.NewContextAsync(new BrowserNewContextOptions { StorageState = storageState });
                Console.WriteLine("Authentication state loaded successfully.");
                return context;
            }
            else
            {
                var context = await _browser.NewContextAsync();
                Console.WriteLine("No authentication state file found. Initializing new context.");
                return context;
            }
        }

        private async Task SaveAuthStateAsync(IBrowserContext context)
        {
            try
            {
                var storageState = await context.StorageStateAsync();
                await File.WriteAllTextAsync("authState.json", storageState);
                Console.WriteLine("Authentication state saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving authentication state: {ex.Message}");
                throw;
            }
        }

        public async Task<IPage> EnsureAuthenticatedAsync(string userID)
        {
            var context = await CreateOrLoadContextAsync();
            var page = await context.NewPageAsync();

            try
            {
                await page.GotoAsync($"https://ss14-admin.spacestation14.com/Players/Info/{userID}");

                if (page.Url.Contains("/Identity/Account/Login"))
                {
                    Console.WriteLine("Login page detected. Starting login process...");
                    await LoginAsync(page);

                    if (await page.Locator("text='Two-factor authentication'").CountAsync() > 0)
                    {
                        Console.WriteLine("Two-factor authentication required.");
                        await HandleTwoFactorAuthenticationAsync(page);
                        await SaveAuthStateAsync(context);
                    }

                    await page.GotoAsync($"https://ss14-admin.spacestation14.com/Players/Info/{userID}");
                }

                Console.WriteLine("Authentication successful.");
                return page;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during authentication for user ID {userID}: {ex.Message}");
                throw;
            }
        }

        private async Task LoginAsync(IPage page)
        {
            try
            {
                await page.FillAsync("#Input_EmailOrUsername", _adminUsername);
                await page.FillAsync("#Input_Password", _adminPassword);
                await page.CheckAsync("#Input_RememberMe");
                await page.ClickAsync("button[type='submit']");
                Console.WriteLine("Login completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
                throw;
            }
        }

        private async Task HandleTwoFactorAuthenticationAsync(IPage page)
        {
            try
            {
                Console.WriteLine("Enter the 2FA code:");
                var twoFactorCode = Console.ReadLine();
                await page.FillAsync("#Input_TwoFactorCode", twoFactorCode);
                await page.CheckAsync("#Input_RememberMachine");
                await page.ClickAsync("button[type='submit']");
                Console.WriteLine("Two-factor authentication completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during two-factor authentication: {ex.Message}");
                throw;
            }
        }
    }
}
