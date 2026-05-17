using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Data;
using SuperMarket.Models;

namespace SuperMarket.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _db;
        public UsersController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var users = await _db.Users.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync();
            return View(users);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(string fullName, string username, string password, string role)
        {
            if (await _db.Users.AnyAsync(u => u.Username == username))
            {
                TempData["Error"] = "اسم المستخدم موجود مسبقاً";
                return RedirectToAction(nameof(Create));
            }

            var user = new AppUser
            {
                FullName = fullName,
                Username = username,
                PasswordHash = AppDbContext.HashPassword(password),
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم إضافة المستخدم!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditSave(int id, string fullName, string username, string? newPassword, string role, bool isActive)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (await _db.Users.AnyAsync(u => u.Username == username && u.Id != id))
            {
                TempData["Error"] = "اسم المستخدم موجود مسبقاً";
                return RedirectToAction(nameof(Edit), new { id });
            }

            user.FullName = fullName;
            user.Username = username;
            user.Role = role;
            user.IsActive = isActive;

            if (!string.IsNullOrWhiteSpace(newPassword))
                user.PasswordHash = AppDbContext.HashPassword(newPassword);

            await _db.SaveChangesAsync();
            TempData["Success"] = "تم تحديث المستخدم!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                // منع تعطيل آخر Admin
                if (user.Role == "Admin" && user.IsActive)
                {
                    var adminCount = await _db.Users.CountAsync(u => u.Role == "Admin" && u.IsActive);
                    if (adminCount <= 1)
                    {
                        TempData["Error"] = "لا يمكن تعطيل آخر مدير في النظام!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                user.IsActive = !user.IsActive;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                if (user.Role == "Admin")
                {
                    var adminCount = await _db.Users.CountAsync(u => u.Role == "Admin");
                    if (adminCount <= 1)
                    {
                        TempData["Error"] = "لا يمكن حذف آخر مدير في النظام!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
            }
            TempData["Success"] = "تم حذف المستخدم";
            return RedirectToAction(nameof(Index));
        }
    }
}
