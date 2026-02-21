using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIBlog.Web.Data;
using AIBlog.Web.Models;
using AIBlog.Web.Services;

namespace AIBlog.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ApplicationDbContext context, EmailService emailService, ILogger<AccountController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    public async Task<IActionResult> Login(string emailOrPhone, string password)
    {
        if (string.IsNullOrEmpty(emailOrPhone) || string.IsNullOrEmpty(password))
        {
            TempData["Error"] = "Please fill in all fields.";
            return View();
        }

        var author = await _context.Authors.FirstOrDefaultAsync(a =>
            a.Email == emailOrPhone || a.PhoneNumber == emailOrPhone);

        if (author == null)
        {
            TempData["Error"] = "Invalid email/phone or password.";
            return View();
        }

        if (author.PasswordHash != password)
        {
            TempData["Error"] = "Invalid email/phone or password.";
            return View();
        }

        HttpContext.Session.SetInt32("UserId", author.Id);
        HttpContext.Session.SetString("UserName", author.Name);
        HttpContext.Session.SetString("UserAvatar", author.Avatar ?? "");

        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    public async Task<IActionResult> Register(string name, string email, string password)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            TempData["Error"] = "Please fill in all fields.";
            return View();
        }

        var existingAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.Email == email);
        if (existingAuthor != null)
        {
            TempData["Error"] = "An account with this email already exists.";
            return View();
        }

        // Generate 6-digit verification code
        var code = new Random().Next(100000, 999999).ToString();

        // Store registration data and code in session
        HttpContext.Session.SetString("RegName", name);
        HttpContext.Session.SetString("RegEmail", email);
        HttpContext.Session.SetString("RegPassword", password);
        HttpContext.Session.SetString("RegCode", code);
        HttpContext.Session.SetString("RegCodeTime", DateTime.Now.ToString());

        // Send verification email
        try
        {
            await _emailService.SendVerificationCodeAsync(email, code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", email);
            TempData["Error"] = "Failed to send verification email. Please try again.";
            return View();
        }

        return RedirectToAction("VerifyEmail");
    }

    // GET: /Account/VerifyEmail
    [HttpGet]
    public IActionResult VerifyEmail()
    {
        var email = HttpContext.Session.GetString("RegEmail");
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Register");
        }

        ViewBag.Email = email;
        return View();
    }

    // POST: /Account/VerifyEmail
    [HttpPost]
    public async Task<IActionResult> VerifyEmail(string code)
    {
        var email = HttpContext.Session.GetString("RegEmail");
        var savedCode = HttpContext.Session.GetString("RegCode");
        var codeTimeStr = HttpContext.Session.GetString("RegCodeTime");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(savedCode))
        {
            return RedirectToAction("Register");
        }

        // Check code expiry (10 minutes)
        if (DateTime.TryParse(codeTimeStr, out var codeTime) && DateTime.Now - codeTime > TimeSpan.FromMinutes(10))
        {
            TempData["Error"] = "Verification code expired. Please register again.";
            ViewBag.Email = email;
            return View();
        }

        if (code != savedCode)
        {
            TempData["Error"] = "Invalid verification code.";
            ViewBag.Email = email;
            return View();
        }

        // Code is valid - create the account
        var name = HttpContext.Session.GetString("RegName") ?? "";
        var password = HttpContext.Session.GetString("RegPassword") ?? "";

        var author = new Author
        {
            Name = name,
            Email = email,
            PasswordHash = password,
            CreatedAt = DateTime.Now,
            FollowersCount = 0,
            FollowingCount = 0,
            ProfileVisibility = "Public",
            SearchEngineVisibility = true,
            TwoFactorEnabled = false
        };

        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        // Clear registration session data
        HttpContext.Session.Remove("RegName");
        HttpContext.Session.Remove("RegEmail");
        HttpContext.Session.Remove("RegPassword");
        HttpContext.Session.Remove("RegCode");
        HttpContext.Session.Remove("RegCodeTime");

        TempData["Success"] = "Account created successfully! Please log in.";
        return RedirectToAction("Login");
    }

    // GET: /Account/ResendCode
    public async Task<IActionResult> ResendCode()
    {
        var email = HttpContext.Session.GetString("RegEmail");
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Register");
        }

        // Generate new code
        var code = new Random().Next(100000, 999999).ToString();
        HttpContext.Session.SetString("RegCode", code);
        HttpContext.Session.SetString("RegCodeTime", DateTime.Now.ToString());

        try
        {
            await _emailService.SendVerificationCodeAsync(email, code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend verification email to {Email}", email);
            TempData["Error"] = "Failed to send verification email. Please try again.";
        }

        return RedirectToAction("VerifyEmail");
    }

    // GET: /Account/Logout
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
