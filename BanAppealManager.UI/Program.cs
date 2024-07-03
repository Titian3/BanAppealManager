using BanAppealManager.Main;
using BanAppealManager.Main.Scrapers.Forums;
using BanAppealManager.Main.Scrapers.Forums.Category;
using BanAppealManager.Main.Scrapers.Forums.Topic;
using BanAppealManager.Main.Scrapers.SS14Admin;
using BanAppealManager.UI.Components;
using DotNetEnv;
using Microsoft.Playwright;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
Env.Load();

string adminUsername = Environment.GetEnvironmentVariable("SS14_ADMIN_USERNAME");
string adminPassword = Environment.GetEnvironmentVariable("SS14_ADMIN_PASSWORD");
string gptKey = Environment.GetEnvironmentVariable("GPT_Api_Key");

if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword))
{
    Console.WriteLine("Environment variables for SS14 admin credentials are not set.");
    return;
}

if (string.IsNullOrEmpty(gptKey))
{
    Console.WriteLine("Environment variables for GPT credentials are not set.");
    return;
}

// Initialize Playwright and browsers
var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true,
    Timeout = 20000 // Set global timeout here
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register custom services
builder.Services.AddSingleton(browser); // Register the browser instance
builder.Services.AddSingleton<SS14AdminScraper>(_ => new SS14AdminScraper(adminUsername, adminPassword, browser));
builder.Services.AddSingleton<ForumBanAppealScraper>(_ => new ForumBanAppealScraper(browser));
builder.Services.AddSingleton<ForumBanAppealSummary>(_ => new ForumBanAppealSummary(browser));
builder.Services.AddSingleton<BanAppealService>(provider =>
{
    var adminScraper = provider.GetRequiredService<SS14AdminScraper>();
    var forumScraper = provider.GetRequiredService<ForumBanAppealScraper>();
    var forumSummaryScraper = provider.GetRequiredService<ForumBanAppealSummary>();
    return new BanAppealService(adminScraper, forumScraper, forumSummaryScraper, gptKey);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
