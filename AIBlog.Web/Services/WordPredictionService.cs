using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AIBlog.Web.Services
{
    public class PredictionResponse
    {
        public bool Success { get; set; }
        public List<string> Predictions { get; set; } = new();
        public string InputText { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    public class CompletionResponse
    {
        public bool Success { get; set; }
        public string Completion { get; set; } = string.Empty;
        public string InputText { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    public class WordPredictionService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public WordPredictionService(string baseUrl = "http://localhost:5002")
        {
            _baseUrl = baseUrl;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<bool> IsServiceHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<PredictionResponse> GetPredictionsAsync(string text, int count = 5)
        {
            var requestBody = new { text, count };
            var response = await PostToApiAsync<PredictionResponse>("/api/predict", requestBody);
            
            return response ?? new PredictionResponse { Success = false, Error = "Failed to communicate with AI service." };
        }

        public async Task<CompletionResponse> GetCompletionAsync(string text, int maxLength = 10)
        {
            var requestBody = new { text, max_length = maxLength };
            var response = await PostToApiAsync<CompletionResponse>("/api/complete", requestBody);
            
            return response ?? new CompletionResponse { Success = false, Error = "Failed to communicate with AI service." };
        }

        private async Task<T?> PostToApiAsync<T>(string endpoint, object payload) where T : class
        {
            try
            {
                if (!_httpClient.DefaultRequestHeaders.Contains("ngrok-skip-browser-warning"))
                {
                    _httpClient.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
                }
                var content = new StringContent(
                    JsonSerializer.Serialize(payload, _jsonOptions),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_baseUrl}{endpoint}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}