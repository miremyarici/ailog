namespace AIBlog.Web.Models.Requests;

public class UpdateInterestsRequest 
{ 
    public List<int> CategoryIds { get; set; } = new(); 
}
