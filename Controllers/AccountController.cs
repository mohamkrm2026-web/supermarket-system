using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Data;
using System.Security.Claims;

namespace SuperMarket.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        public AccountController(AppDbContext db) { _db = db; }

        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe, string? returnUrl)
        {
            var hash = AppDbContext.HashPassword(password);
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Username == username && u.PasswordHash == hash && u.IsActive);

            if (user == null)
            {
                ViewBag.Error = "اسم المستخدم أو كلمة المرور غير صحيحة";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            user.LastLogin = DateTime.Now;
            await _db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new("Username", user.Username),
                new(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var props = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();
    }
}
