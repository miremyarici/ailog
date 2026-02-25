using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AIBlog.Web.Data;

namespace AIBlog.Web.Controllers;

public abstract class BaseController : Controller
{
    protected readonly ApplicationDbContext _context;

    protected BaseController(ApplicationDbContext context)
    {
        _context = context;
    }

    protected int GetCurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId") ?? 0;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        
        // Bu bilgileri her view'da kullandığın için burada set etmek mantıklı
        var author = _context.Authors.FirstOrDefault(a => a.Id == userId.Value);
        ViewBag.CurrentUserAvatar = author?.Avatar;
        ViewBag.CurrentUserName = author?.Name ?? "User";
    }
}