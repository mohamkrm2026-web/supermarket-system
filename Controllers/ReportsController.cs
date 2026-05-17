using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Data;
using SuperMarket.Models;

namespace SuperMarket.Controllers
{
    [Authorize(Roles = "Admin,مشاهدة فقط")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _db;
        public ReportsController(AppDbContext db) { _db = db; }

        // ========== Dashboard ==========
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var month = new DateTime(today.Year, today.Month, 1);
            var lastMonth = month.AddMonths(-1);

            var allSales = await _db.Sales.Include(s => s.Customer).ToListAsync();
            var allItems = await _db.SaleItems.ToListAsync();
            var allProducts = await _db.Products.ToListAsync();

            ViewBag.TodayRevenue = allSales.Where(s => s.SaleDate.Date == today).Sum(s => s.Total);
            ViewBag.MonthRevenue = allSales.Where(s => s.SaleDate >= month).Sum(s => s.Total);
            ViewBag.LastMonthRevenue = allSales.Where(s => s.SaleDate >= lastMonth && s.SaleDate < month).Sum(s => s.Total);
            ViewBag.TotalSales = allSales.Count;

            ViewBag.TopProducts = allItems
                .GroupBy(i => i.ProductName)
                .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.Total) })
                .OrderByDescending(x => x.Revenue).Take(5).ToList();

            ViewBag.MonthlySales = allSales
                .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new { Month = g.Key.Month, Year = g.Key.Year, Total = g.Sum(x => x.Total), Count = g.Count() })
                .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).Take(6).ToList();

            ViewBag.LowStock = allProducts.Where(p => p.Stock <= p.MinStock).ToList();

            return View();
        }

        // ========== تقارير المبيعات ==========
        public async Task<IActionResult> Sales(string period = "monthly", DateTime? from = null, DateTime? to = null)
        {
            var today = DateTime.Today;
            var fromDate = from ?? today.AddDays(-30);
            var toDate = to ?? today;

            var sales = await _db.Sales.Include(s => s.Customer).Include(s => s.Items)
                .Where(s => s.SaleDate.Date >= fromDate && s.SaleDate.Date <= toDate)
                .OrderByDescending(s => s.SaleDate).ToListAsync();

            var allItems = sales.SelectMany(s => s.Items).ToList();
            var allProducts = await _db.Products.ToListAsync();

            // إجماليات
            ViewBag.TotalRevenue = sales.Sum(s => s.Total);
            ViewBag.TotalDiscount = sales.Sum(s => s.Discount);
            ViewBag.TotalProfit = allItems.Sum(i => {
                var p = allProducts.FirstOrDefault(x => x.Id == i.ProductId);
                return p != null ? (i.Price - p.CostPrice) * i.Quantity : 0;
            });
            ViewBag.TotalCount = sales.Count;

            // مقارنة بالشهر السابق
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);
            var allSales = await _db.Sales.ToListAsync();
            ViewBag.ThisMonthRevenue = allSales.Where(s => s.SaleDate >= thisMonth).Sum(s => s.Total);
            ViewBag.LastMonthRevenue = allSales.Where(s => s.SaleDate >= lastMonth && s.SaleDate < thisMonth).Sum(s => s.Total);

            // مبيعات يومية في النطاق
            ViewBag.DailySales = sales
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.Total), Count = g.Count() })
                .OrderBy(x => x.Date).ToList();

            // ساعات الذروة
            ViewBag.PeakHours = sales
                .GroupBy(s => s.SaleDate.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count(), Total = g.Sum(x => x.Total) })
                .OrderByDescending(x => x.Count).Take(24).OrderBy(x => x.Hour).ToList();

            // مبيعات حسب طريقة الدفع
            ViewBag.PaymentMethods = sales
                .GroupBy(s => s.PaymentMethod)
                .Select(g => new { Method = g.Key, Count = g.Count(), Total = g.Sum(x => x.Total) })
                .OrderByDescending(x => x.Total).ToList();

            // مبيعات أسبوعية
            ViewBag.WeeklySales = sales
                .GroupBy(s => System.Globalization.ISOWeek.GetWeekOfYear(s.SaleDate))
                .Select(g => new { Week = g.Key, Total = g.Sum(x => x.Total), Count = g.Count() })
                .OrderBy(x => x.Week).ToList();

            ViewBag.Sales = sales;
            ViewBag.From = fromDate.ToString("yyyy-MM-dd");
            ViewBag.To = toDate.ToString("yyyy-MM-dd");
            ViewBag.Period = period;

            return View();
        }

        // ========== تقارير المخزون ==========
        public async Task<IActionResult> Inventory()
        {
            var products = await _db.Products.ToListAsync();
            var allItems = await _db.SaleItems.ToListAsync();
            var cutoff = DateTime.Today.AddDays(-30);
            var sales = await _db.Sales.Where(s => s.SaleDate >= cutoff).Include(s => s.Items).ToListAsync();
            var recentItems = sales.SelectMany(s => s.Items).ToList();

            var soldProductIds = recentItems.Select(i => i.ProductId).Distinct().ToHashSet();

            // مخزون حالي
            ViewBag.Products = products;
            ViewBag.TotalStockValue = products.Sum(p => p.Stock * p.CostPrice);
            ViewBag.TotalRetailValue = products.Sum(p => p.Stock * p.Price);

            // أقل مبيعاً
            ViewBag.LeastSold = allItems
                .GroupBy(i => new { i.ProductId, i.ProductName })
                .Select(g => new { g.Key.ProductName, Qty = g.Sum(x => x.Quantity) })
                .OrderBy(x => x.Qty).Take(10).ToList();

            // أكثر مبيعاً
            ViewBag.TopSold = allItems
                .GroupBy(i => new { i.ProductId, i.ProductName })
                .Select(g => new { g.Key.ProductName, Qty = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.Total) })
                .OrderByDescending(x => x.Qty).Take(10).ToList();

            // منتجات قاربت على الانتهاء
            ViewBag.LowStock = products.Where(p => p.Stock <= p.MinStock && p.Stock > 0).OrderBy(p => p.Stock).ToList();
            ViewBag.OutOfStock = products.Where(p => p.Stock == 0).ToList();

            // منتجات راكدة (لم تباع في آخر 30 يوم)
            ViewBag.SlowMoving = products.Where(p => !soldProductIds.Contains(p.Id) && p.Stock > 0).ToList();

            return View();
        }

        // ========== تقارير الأرباح ==========
        public async Task<IActionResult> Profits(DateTime? from = null, DateTime? to = null)
        {
            var today = DateTime.Today;
            var fromDate = from ?? new DateTime(today.Year, today.Month, 1);
            var toDate = to ?? today;

            var sales = await _db.Sales.Include(s => s.Items)
                .Where(s => s.SaleDate.Date >= fromDate && s.SaleDate.Date <= toDate)
                .ToListAsync();
            var products = await _db.Products.ToListAsync();
            var allItems = sales.SelectMany(s => s.Items).ToList();

            decimal CalcProfit(SaleItem i) {
                var p = products.FirstOrDefault(x => x.Id == i.ProductId);
                return p != null ? (i.Price - p.CostPrice) * i.Quantity : 0;
            }

            // إجمالي
            ViewBag.TotalRevenue = sales.Sum(s => s.Total);
            ViewBag.TotalCost = allItems.Sum(i => {
                var p = products.FirstOrDefault(x => x.Id == i.ProductId);
                return p != null ? p.CostPrice * i.Quantity : 0;
            });
            ViewBag.TotalProfit = allItems.Sum(i => CalcProfit(i));
            ViewBag.ProfitMargin = ViewBag.TotalRevenue > 0
                ? Math.Round((double)ViewBag.TotalProfit / (double)ViewBag.TotalRevenue * 100, 1) : 0;

            // ربح يومي
            ViewBag.DailyProfit = sales
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.Total),
                    Profit = g.SelectMany(x => x.Items).Sum(i => CalcProfit(i))
                })
                .OrderBy(x => x.Date).ToList();

            // هامش ربح لكل منتج
            ViewBag.ProductMargins = products
                .Where(p => p.Price > 0)
                .Select(p => new {
                    p.Name, p.Category,
                    p.Price, p.CostPrice,
                    Margin = p.Price > 0 ? Math.Round((double)(p.Price - p.CostPrice) / (double)p.Price * 100, 1) : 0,
                    Profit = p.Price - p.CostPrice
                })
                .OrderByDescending(x => x.Margin).ToList();

            // أكثر المنتجات ربحاً
            ViewBag.TopProfitable = allItems
                .GroupBy(i => new { i.ProductId, i.ProductName })
                .Select(g => new {
                    g.Key.ProductName,
                    TotalProfit = g.Sum(i => CalcProfit(i)),
                    Qty = g.Sum(i => i.Quantity)
                })
                .OrderByDescending(x => x.TotalProfit).Take(10).ToList();

            ViewBag.From = fromDate.ToString("yyyy-MM-dd");
            ViewBag.To = toDate.ToString("yyyy-MM-dd");
            return View();
        }

        // ========== تقارير العملاء ==========
        public async Task<IActionResult> Customers()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var customers = await _db.Customers.ToListAsync();
            var sales = await _db.Sales.Include(s => s.Customer).ToListAsync();

            // أكثر العملاء شراءً
            ViewBag.TopCustomers = customers
                .OrderByDescending(c => c.TotalPurchases).Take(10).ToList();

            // العملاء الجدد هذا الشهر
            ViewBag.NewCustomers = customers
                .Where(c => c.CreatedAt >= thisMonth).OrderByDescending(c => c.CreatedAt).ToList();
            ViewBag.NewCustomersCount = ViewBag.NewCustomers.Count;

            // نقاط الولاء
            ViewBag.TopPoints = customers
                .Where(c => c.Points > 0)
                .OrderByDescending(c => c.Points).Take(10).ToList();

            // عدد الزيارات لكل عميل
            ViewBag.CustomerVisits = sales
                .Where(s => s.CustomerId != null)
                .GroupBy(s => s.CustomerId)
                .Select(g => new {
                    Customer = customers.FirstOrDefault(c => c.Id == g.Key),
                    Visits = g.Count(),
                    LastVisit = g.Max(x => x.SaleDate)
                })
                .Where(x => x.Customer != null)
                .OrderByDescending(x => x.Visits).Take(10).ToList();

            ViewBag.TotalCustomers = customers.Count;
            return View();
        }

        // ========== تقارير المشتريات ==========
        public async Task<IActionResult> Purchases()
        {
            var orders = await _db.PurchaseOrders
                .Include(o => o.Supplier).Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate).ToListAsync();

            var suppliers = await _db.Suppliers.ToListAsync();

            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalReceived = orders.Where(o => o.Status == "مستلم").Sum(o => o.Total);
            ViewBag.PendingOrders = orders.Count(o => o.Status == "معلق");

            // أكثر الموردين تعاملاً
            ViewBag.TopSuppliers = orders
                .Where(o => o.Status == "مستلم")
                .GroupBy(o => o.SupplierId)
                .Select(g => new {
                    Supplier = suppliers.FirstOrDefault(s => s.Id == g.Key),
                    OrderCount = g.Count(),
                    TotalValue = g.Sum(o => o.Total)
                })
                .Where(x => x.Supplier != null)
                .OrderByDescending(x => x.TotalValue).ToList();

            // فواتير الشراء
            ViewBag.Orders = orders;

            // مشتريات شهرية
            ViewBag.MonthlyPurchases = orders
                .Where(o => o.Status == "مستلم")
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Total), Count = g.Count() })
                .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).Take(6).ToList();

            return View();
        }
    }
}
