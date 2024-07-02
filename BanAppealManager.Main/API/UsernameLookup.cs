using BanAppealManager.Main.Models;
using Newtonsoft.Json;

namespace BanAppealManager.Main.API
{
    public class UsernameLookupService
    {
        private readonly HttpClient _httpClient;

        public UsernameLookupService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<usernameQueryResponse> GetUserDataAsync(string username)
        {
            var response = await _httpClient.GetAsync($"https://auth.spacestation14.com/api/query/name?name={username}");
            var content = await response.Content.ReadAsStringAsync();
            var userData = JsonConvert.DeserializeObject<usernameQueryResponse>(content);
            return userData;
        }
    }
}