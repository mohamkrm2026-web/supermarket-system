using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Data;
using SuperMarket.Models;

namespace SuperMarket.Controllers
{
    [Authorize(Roles = "Admin,كاشير,مشاهدة فقط")]
    public class CustomersController : Controller
    {
        private readonly AppDbContext _db;
        public CustomersController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index(string? search)
        {
            var q = _db.Customers.AsQueryable();
            if (!string.IsNullOrEmpty(search)) q = q.Where(c => c.Name.Contains(search) || (c.Phone != null && c.Phone.Contains(search)));
            ViewBag.Search = search;
            return View(await q.OrderBy(c => c.Name).ToListAsync());
        }

        [Authorize(Roles = "Admin,كاشير")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "Admin,كاشير")]
        public async Task<IActionResult> Create(Customer c)
        {
            c.CreatedAt = DateTime.Now;
            _db.Customers.Add(c);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم إضافة العميل!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,كاشير")]
        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.Customers.FindAsync(id);
            if (c == null) return NotFound();
            ViewBag.Customer = c;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,كاشير")]
        public async Task<IActionResult> EditSave(Customer c)
        {
            _db.Update(c);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم تحديث العميل!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,كاشير")]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Customers.FindAsync(id);
            if (c != null) { _db.Customers.Remove(c); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var c = await _db.Customers.FindAsync(id);
            if (c == null) return NotFound();
            var sales = await _db.Sales.Where(s => s.CustomerId == id).OrderByDescending(s => s.SaleDate).ToListAsync();
            ViewBag.Customer = c;
            ViewBag.Sales = sales;
            return View();
        }
    }
}
