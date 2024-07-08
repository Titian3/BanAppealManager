﻿using BanAppealManager.Main;
using BanAppealManager.Main.Scrapers.Forums;
using BanAppealManager.Main.Scrapers.Forums.Category;
using BanAppealManager.Main.Scrapers.Forums.Topic;
using BanAppealManager.Main.Scrapers.SS14Admin;
using DotNetEnv;
using Microsoft.Playwright;
using System;
using System.Threading.Tasks;
using BanAppealManager.Main.Scrapers.Forums.Topic.BanDiscussions;
using BanAppealManager.Main.Summarizers;

namespace BanAppealManager.Main
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Env.Load();

            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions 
            { 
                Headless = true,
                Timeout = 20000 // Set global timeout here
            });

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
            
            Console.WriteLine($"Welcome {adminUsername}!");
            
            Console.WriteLine("Enter the URL of the appeal:");
            string appealUrl = Console.ReadLine();

            var authHandler = new AuthHandler(adminUsername, adminPassword, browser);
            var ss14AdminScraper = new SS14AdminScraper(authHandler);
            var gptConnector = new GPTConnector(gptKey);

            var forumAppealScraper = new ForumBanAppealScraper(browser);
            var forumAppealScraperSummary = new ForumBanAppealSummary(browser);
            var forumAppealDiscussionScraperSummary = new ForumBanAppealDiscussionSummary(browser, authHandler);
            
            var forumAppealDiscussionScraper = new BanAppealDiscussionScraper(authHandler, appealUrl, gptConnector); // Adding missing initialization

            var banAppealService = new BanAppealService(
                ss14AdminScraper, 
                forumAppealScraper, 
                forumAppealScraperSummary, 
                forumAppealDiscussionScraperSummary,
                gptKey,
                authHandler
            );

            await banAppealService.ProcessAppealAsync(appealUrl);

            await browser.CloseAsync();
        }
    }
}
