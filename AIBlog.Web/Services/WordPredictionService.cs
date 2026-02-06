using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AIBlog.Web.Services
{
    /// <summary>
    /// Response model for word predictions from Python API
    /// </summary>
    public class PredictionResponse
    {
        public bool Success { get; set; }
        public List<string> Predictions { get; set; } = new();
        public string InputText { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    /// <summary>
    /// Response model for text completion from Python API
    /// </summary>
    public class CompletionResponse
    {
        public bool Success { get; set; }
        public string Completion { get; set; } = string.Empty;
        public string InputText { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    /// <summary>
    /// Service to communicate with Python GPT-2 Word Prediction API
    /// </summary>
    public class WordPredictionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public WordPredictionService(string baseUrl = "http://localhost:5002")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Check if the Python AI service is running
        /// </summary>
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

        /// <summary>
        /// Get word predictions for the given text
        /// </summary>
        public async Task<PredictionResponse> GetPredictionsAsync(string text, int count = 5)
        {
            try
            {
                var requestBody = new { text = text, count = count };
                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody, _jsonOptions),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/predict", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<PredictionResponse>(responseContent, _jsonOptions) 
                        ?? new PredictionResponse { Success = false, Error = "Failed to parse response" };
                }

                return new PredictionResponse
                {
                    Success = false,
                    Error = $"API returned status {response.StatusCode}: {responseContent}"
                };
            }
            catch (HttpRequestException ex)
            {
                return new PredictionResponse
                {
                    Success = false,
                    Error = $"Connection error: {ex.Message}. Is the Python AI service running?"
                };
            }
            catch (Exception ex)
            {
                return new PredictionResponse
                {
                    Success = false,
                    Error = $"Unexpected error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get text completion for the given text
        /// </summary>
        public async Task<CompletionResponse> GetCompletionAsync(string text, int maxLength = 10)
        {
            try
            {
                var requestBody = new { text = text, max_length = maxLength };
                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody, _jsonOptions),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/complete", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<CompletionResponse>(responseContent, _jsonOptions)
                        ?? new CompletionResponse { Success = false, Error = "Failed to parse response" };
                }

                return new CompletionResponse
                {
                    Success = false,
                    Error = $"API returned status {response.StatusCode}: {responseContent}"
                };
            }
            catch (HttpRequestException ex)
            {
                return new CompletionResponse
                {
                    Success = false,
                    Error = $"Connection error: {ex.Message}. Is the Python AI service running?"
                };
            }
            catch (Exception ex)
            {
                return new CompletionResponse
                {
                    Success = false,
                    Error = $"Unexpected error: {ex.Message}"
                };
            }
        }
    }
}
