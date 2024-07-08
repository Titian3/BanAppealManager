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

        public async Task<IPage> EnsureAuthenticatedAsync(string url, bool isForum = false)
        {
            var context = await CreateOrLoadContextAsync();
            var page = await context.NewPageAsync();

            try
            {
                var response = await page.GotoAsync(url);

                if (response.Status == 404 && isForum)
                {
                    Console.WriteLine("Page not found. Starting login process...");
                    await page.GotoAsync("https://forum.spacestation14.com/login");
                    
                    //force a wait here for 5s
                    await Task.Delay(5000);
                    
                    // Check if we are on the account login page
                    if (page.Url.Contains("account.spacestation14.com"))
                    {
                        await ForumLoginAsync(page);

                        if (await page.Locator("text='Two-factor authentication'").CountAsync() > 0)
                        {
                            Console.WriteLine("Two-factor authentication required.");
                            await HandleTwoFactorAuthenticationAsync(page);
                            await SaveAuthStateAsync(context);
                        }

                        // Ensure we are on the categories page after login
                        await page.GotoAsync("https://forum.spacestation14.com/categories");
                    }

                    // Navigate back to the original URL after login
                    response = await page.GotoAsync(url);
                }
                else if (response.Status == 200 && page.Url.Contains("categories"))
                {
                    Console.WriteLine("Already logged in, navigating to the desired page.");
                    response = await page.GotoAsync(url);
                }
                else if (page.Url.Contains("/Identity/Account/Login"))
                {
                    Console.WriteLine("Admin login page detected. Starting login process...");
                    await AdminLoginAsync(page);

                    if (await page.Locator("text='Two-factor authentication'").CountAsync() > 0)
                    {
                        Console.WriteLine("Two-factor authentication required.");
                        await HandleTwoFactorAuthenticationAsync(page);
                        await SaveAuthStateAsync(context);
                    }

                    // Navigate back to the original URL after login
                    response = await page.GotoAsync(url);
                }

                Console.WriteLine("Authentication successful.");
                return page;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during authentication: {ex.Message}");
                throw;
            }
        }

        private async Task AdminLoginAsync(IPage page)
        {
            try
            {
                await page.FillAsync("#Input_EmailOrUsername", _adminUsername);
                await page.FillAsync("#Input_Password", _adminPassword);
                await page.CheckAsync("#Input_RememberMe");
                await page.ClickAsync("button[type='submit']");
                Console.WriteLine("Admin login completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during admin login: {ex.Message}");
                throw;
            }
        }

        private async Task ForumLoginAsync(IPage page)
        {
            try
            {
                await page.FillAsync("#login-username", _adminUsername);
                await page.FillAsync("#login-password", _adminPassword);
                await page.ClickAsync("button[type='submit']");
                Console.WriteLine("Forum login completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during forum login: {ex.Message}");
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
