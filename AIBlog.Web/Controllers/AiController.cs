using Microsoft.AspNetCore.Mvc;
using AIBlog.Web.Services;

namespace AIBlog.Web.Controllers
{
    /// <summary>
    /// API Controller for AI-related endpoints
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly WordPredictionService _predictionService;

        public AiController(WordPredictionService predictionService)
        {
            _predictionService = predictionService;
        }

        /// <summary>
        /// Check if AI service is available
        /// GET /api/ai/health
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            var isHealthy = await _predictionService.IsServiceHealthyAsync();
            
            if (isHealthy)
            {
                return Ok(new { status = "healthy", message = "AI service is running" });
            }
            
            return StatusCode(503, new { status = "unavailable", message = "AI service is not running. Please start the Python service." });
        }

        /// <summary>
        /// Get word predictions
        /// POST /api/ai/predict
        /// </summary>
        [HttpPost("predict")]
        public async Task<IActionResult> Predict([FromBody] PredictRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Text))
            {
                return BadRequest(new { success = false, error = "Text is required" });
            }

            var result = await _predictionService.GetPredictionsAsync(request.Text, request.Count);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return StatusCode(500, result);
        }

        /// <summary>
        /// Get text completion
        /// POST /api/ai/complete
        /// </summary>
        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] CompleteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Text))
            {
                return BadRequest(new { success = false, error = "Text is required" });
            }

            var result = await _predictionService.GetCompletionAsync(request.Text, request.MaxLength);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return StatusCode(500, result);
        }
    }

    public class PredictRequest
    {
        public string Text { get; set; } = string.Empty;
        public int Count { get; set; } = 5;
    }

    public class CompleteRequest
    {
        public string Text { get; set; } = string.Empty;
        public int MaxLength { get; set; } = 10;
    }
}
