using System.Text;
using BanAppealManager.UI.Models;
using Newtonsoft.Json;

namespace BanAppealManager.UI.Services
{
    public class ApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClientService> _logger;

        public ApiClientService(HttpClient httpClient, ILogger<ApiClientService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<Player>> GetPlayers(string baseUrl, string watchdogToken)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("WatchdogToken", watchdogToken);

            var response = await _httpClient.GetFromJsonAsync<List<Player>>($"{baseUrl}/playerlist");
            return response ?? new List<Player>();
        }

        public async Task<bool> KickPlayer(string baseUrl, string watchdogToken, string userId, string reason)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("WatchdogToken", watchdogToken);

            var kickRequest = new KickPlayerRequest { UserId = userId, Reason = reason };
            
            _logger.LogInformation("Sending kick request to {Url} with token {Token} for user {UserId} with reason {Reason}", 
                $"{baseUrl}/kickplayer", watchdogToken, userId, reason);

            // Serialize the request using Newtonsoft.Json
            var jsonString = JsonConvert.SerializeObject(kickRequest);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{baseUrl}/kickplayer", content);


            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Kick request successful for user {UserId}", userId);
                return true;
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Kick request failed with status {StatusCode} and message {Message}", response.StatusCode, responseBody);
                return false;
            }
        }
    }

    public class KickPlayerRequest
    {
        public string UserId { get; set; }
        public string Reason { get; set; }
    }
}
