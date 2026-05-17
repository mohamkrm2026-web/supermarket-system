using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Data;
using SuperMarket.Models;

namespace SuperMarket.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var allSales = await _db.Sales.Include(s => s.Customer).ToListAsync();
            var allProducts = await _db.Products.ToListAsync();
            var today = DateTime.Today;

            ViewBag.TotalProducts = allProducts.Count;
            ViewBag.TotalCustomers = await _db.Customers.CountAsync();
            ViewBag.TodaySales = allSales.Where(s => s.SaleDate.Date == today).Count();
            ViewBag.TodayRevenue = allSales.Where(s => s.SaleDate.Date == today).Sum(s => s.Total);
            ViewBag.MonthRevenue = allSales.Where(s => s.SaleDate.Month == today.Month).Sum(s => s.Total);
            ViewBag.LowStock = allProducts.Where(p => p.Stock <= p.MinStock).Count();
            ViewBag.RecentSales = allSales.OrderByDescending(s => s.SaleDate).Take(5).ToList();
            ViewBag.LowStockProducts = allProducts.Where(p => p.Stock <= p.MinStock).Take(5).ToList();

            return View();
        }
    }
}