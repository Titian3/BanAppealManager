using System.Text;
using BanAppealManager.Main.Models;
using Newtonsoft.Json;

namespace BanAppealManager.Main.Summarizers
{
    public class GPTConnector
    {
        private readonly HttpClient _httpClient;
        private const string BanProcessingAssistantId = "asst_0C1X6H3wEG4BgWqt7boAt6lJ";
        private const string DiscussionCommentProcessingAssistantId = "asst_93HDxlNV2YYkQSDexPRMiffL";

        public GPTConnector(string apiKey)
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(2),
            };
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
        }

        public async Task<string> CreateThreadAsync()
        {
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/threads", null);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var threadResponse = JsonConvert.DeserializeObject<CreateThreadResponse>(responseBody);
            return threadResponse.Id;
        }

        public async Task AddMessageToThreadAsync(string threadId, string message)
        {
            var messageContent = new
            {
                role = "user",
                content = message
            };

            var content = new StringContent(JsonConvert.SerializeObject(messageContent), Encoding.UTF8,
                "application/json");
            var response =
                await _httpClient.PostAsync($"https://api.openai.com/v1/threads/{threadId}/messages", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<string> CreateRunAsync(string threadId, string assistantId)
        {
            var runContent = new
            {
                assistant_id = assistantId
            };

            var content = new StringContent(JsonConvert.SerializeObject(runContent), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"https://api.openai.com/v1/threads/{threadId}/runs", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var runResponse = JsonConvert.DeserializeObject<CreateRunResponse>(responseBody);
            return runResponse.Id;
        }

        public async Task<string> ListMessagesAsync(string threadId)
        {
            var response = await _httpClient.GetAsync($"https://api.openai.com/v1/threads/{threadId}/messages");
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        public async Task<string> GetRunResultAsync(string threadId, string runId)
        {
            string status;
            RunResponse runResponse;

            do
            {
                var response = await _httpClient.GetAsync($"https://api.openai.com/v1/threads/{threadId}/runs/{runId}");
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                runResponse = JsonConvert.DeserializeObject<RunResponse>(responseBody);
                status = runResponse?.Status;

                if (status != "completed")
                {
                    await Task.Delay(1000); // Wait for a second before polling again
                }
            } while (status != "completed");

            var messagesResponse = await ListMessagesAsync(threadId);
            var messagesList = JsonConvert.DeserializeObject<MessageListResponse>(messagesResponse);

            // Find the message from the assistant role
            var assistantMessage = messagesList?.Data?.FirstOrDefault(m => m.Role == "assistant");
            if (assistantMessage == null)
            {
                throw new Exception("No assistant messages found in the run response.");
            }

            return assistantMessage.Content[0].Text.Value;
        }

        private string TransformBanDetails(List<BanDetails> banDetails)
        {
            var sb = new StringBuilder();
            foreach (var ban in banDetails)
            {
                sb.AppendLine($"Reason: {ban.Reason}");
                sb.AppendLine($"Ban Time: {ban.BanTime}");
                sb.AppendLine($"Is Permanent: {ban.IsPermanent}");
                sb.AppendLine($"Is Active: {ban.IsActive}");
                sb.AppendLine($"Is Expired: {ban.IsExpired}");
                sb.AppendLine($"Expire Time: {ban.ExpireTime}");
                sb.AppendLine($"Unban Time: {ban.UnbanTime}");
                sb.AppendLine($"Unbanned By: {ban.UnbannedBy}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string TransformNotes(List<NoteDetails> notes)
        {
            var sb = new StringBuilder();
            foreach (var note in notes)
            {
                sb.AppendLine($"Type: {note.Type}");
                sb.AppendLine($"Message: {note.Message}");
                sb.AppendLine($"Round: {note.Round}");
                sb.AppendLine($"Severity: {note.Severity}");
                sb.AppendLine($"Visible: {note.Visible}");
                sb.AppendLine($"Playtime: {note.Playtime}");
                sb.AppendLine($"Expires: {note.Expires}");
                sb.AppendLine($"Created: {note.Created}");
                sb.AppendLine($"Edited: {note.Edited}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string TransformRoleBanDetails(List<GroupedRoleBan> groupedRoleBans)
        {
            var sb = new StringBuilder();
            foreach (var roleBan in groupedRoleBans)
            {
                sb.AppendLine($"Reason: {roleBan.Reason}");
                sb.AppendLine($"Ban Time: {roleBan.BanTime}");
                sb.AppendLine($"Expire Time: {roleBan.ExpireTime}");
                sb.AppendLine($"Unban Time: {roleBan.UnbanTime}");
                sb.AppendLine($"Unbanned By: {roleBan.UnbannedBy}");
                sb.AppendLine($"Roles: {string.Join(", ", roleBan.Roles)}");
                sb.AppendLine($"Departments: {string.Join(", ", roleBan.Departments)}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public async Task<GPTResponseAppealProcessing> GetSummaryAsync(AppealData appealData, Userdetails userDetails,
            string aHelpDetails)
        {
            try
            {
                var threadId = await CreateThreadAsync();

                var banDetails = TransformBanDetails(userDetails.Banlist);
                var roleBanDetails = TransformRoleBanDetails(userDetails.GroupedRoleBans);
                var notes = TransformNotes(userDetails.Notes);

                var message = $@"
Type Of ban Appealing: {appealData.BanType},
Ban Pretext: {appealData.BanPretext}, 
Appeal Reason: {appealData.BanAppealReason}, 
Ban appeal Reason: {appealData.BanReason},
Ban Details reason: {banDetails},
Role Ban Details: {roleBanDetails},
Notes: {notes},
aHelp Details: {aHelpDetails},
overallPlaytime: {userDetails.PlaytimeOverall}";

                await AddMessageToThreadAsync(threadId, message);
                var runId = await CreateRunAsync(threadId, BanProcessingAssistantId);
                var result = await GetRunResultAsync(threadId, runId);

                // Deserialize the result
                var gptResponse = JsonConvert.DeserializeObject<GPTResponseAppealProcessing>(result);
                return gptResponse;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e.Message}");
                throw;
            }
        }


        public async Task<GPTResponseDiscussionCommentProcessing> AnalyzeComment(string discussionComment)
        {
            try
            {
                Console.WriteLine("Starting AnalyzeComment method.");
        
                var threadId = await CreateThreadAsync();
                Console.WriteLine($"Thread created with ID: {threadId}");

                var message = $@"Please analyze this comment: {discussionComment}";
                await AddMessageToThreadAsync(threadId, message);
                Console.WriteLine("Message added to thread.");

                var runId = await CreateRunAsync(threadId, DiscussionCommentProcessingAssistantId);
                Console.WriteLine($"Run created with ID: {runId}");

                var result = await GetRunResultAsync(threadId, runId);
                Console.WriteLine("Run result retrieved.");

                // Deserialize the result
                var gptResponse = JsonConvert.DeserializeObject<GPTResponseDiscussionCommentProcessing>(result);
                Console.WriteLine("Result deserialized successfully.");

                return gptResponse;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                if (e.InnerException is TimeoutException)
                {
                    Console.WriteLine("The request timed out.");
                }
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e.Message}");
                throw;
            }
        }
    }

    public class CreateThreadResponse
    {
        [JsonProperty("id")] public string Id { get; set; }
    }

    public class CreateRunResponse
    {
        [JsonProperty("id")] public string Id { get; set; }
    }

    public class RunResponse
    {
        [JsonProperty("status")] public string Status { get; set; }

        [JsonProperty("messages")] public RunMessage[] Messages { get; set; }
    }

    public class RunMessage
    {
        [JsonProperty("role")] public string Role { get; set; }

        [JsonProperty("content")] public RunContent[] Content { get; set; }
    }

    public class RunContent
    {
        [JsonProperty("text")] public RunText Text { get; set; }
    }

    public class RunText
    {
        [JsonProperty("value")] public string Value { get; set; }
    }

    public class MessageListResponse
    {
        [JsonProperty("data")] public RunMessage[] Data { get; set; }
    }

    public class GPTResponseAppealProcessing
    {
        [JsonProperty("SummarizedBanReason")] public string SummarizedBanReason { get; set; }

        [JsonProperty("AppealSummary")] public string AppealSummary { get; set; }
    }
    
    public class GPTResponseDiscussionCommentProcessing
    {
        [JsonProperty("CommentVoteType")] public string CommentVoteType { get; set; }

        [JsonProperty("ReductionLengthTimeInWeeks")] public int ReductionLengthTimeInWeeks { get; set; }
        
        [JsonProperty("CommentSentiment")] public string CommentSentiment { get; set; }

        [JsonProperty("VotingReasons")] public List<string> VotingReasons { get; set; }
        [JsonProperty("DiscussionOrDecision")] public string DiscussionOrDecision { get; set; }
    
    }
}