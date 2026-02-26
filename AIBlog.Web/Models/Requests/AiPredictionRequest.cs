namespace AIBlog.Web.Models.Requests
{
    public class AiPredictionRequest
    {
        public string Text { get; set; } = string.Empty;
        public int Count { get; set; } = 5;
    }
}
