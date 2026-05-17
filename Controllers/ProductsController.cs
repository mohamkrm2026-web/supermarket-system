using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Data;
using SuperMarket.Models;

namespace SuperMarket.Controllers
{
    [Authorize(Roles = "Admin,مدير مخزن,مشاهدة فقط")]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _db;
        public ProductsController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index(string? search, string? category)
        {
            var q = _db.Products.AsQueryable();
            if (!string.IsNullOrEmpty(search)) q = q.Where(p => p.Name.Contains(search) || (p.Barcode != null && p.Barcode.Contains(search)));
            if (!string.IsNullOrEmpty(category)) q = q.Where(p => p.Category == category);
            ViewBag.Categories = await _db.Products.Select(p => p.Category).Distinct().ToListAsync();
            ViewBag.Search = search; ViewBag.CurrentCat = category;
            return View(await q.OrderBy(p => p.Name).ToListAsync());
        }

        [Authorize(Roles = "Admin,مدير مخزن")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "Admin,مدير مخزن")]
        public async Task<IActionResult> Create(Product p)
        {
            _db.Products.Add(p);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم إضافة المنتج!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,مدير مخزن")]
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p == null) return NotFound();
            ViewBag.Product = p;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,مدير مخزن")]
        public async Task<IActionResult> EditSave(Product p)
        {
            _db.Update(p);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم تحديث المنتج!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,مدير مخزن")]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p != null) { _db.Products.Remove(p); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> GetByBarcode(string barcode)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Barcode == barcode && x.IsActive);
            if (p == null) return NotFound();
            return Json(new { id = p.Id, name = p.Name, price = p.Price, stock = p.Stock, unit = p.Unit });
        }
    }
}
