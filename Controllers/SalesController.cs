using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Data;
using SuperMarket.Models;

namespace SuperMarket.Controllers
{
    [Authorize(Roles = "Admin,كاشير,مشاهدة فقط")]
    public class SalesController : Controller
    {
        private readonly AppDbContext _db;
        public SalesController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var sales = await _db.Sales.Include(s => s.Customer).OrderByDescending(s => s.SaleDate).Take(50).ToListAsync();
            return View(sales);
        }

        [Authorize(Roles = "Admin,كاشير")]
        public async Task<IActionResult> Cashier()
        {
            ViewBag.Customers = await _db.Customers.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Products = await _db.Products.Where(p => p.IsActive && p.Stock > 0).OrderBy(p => p.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,كاشير")]
        public async Task<IActionResult> CompleteSale(int customerId, string paymentMethod, string discount, string items)
        {
            decimal disc = decimal.TryParse(discount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dv) ? dv : 0;

            var itemList = System.Text.Json.JsonSerializer.Deserialize<List<SaleItemDto>>(items, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (itemList == null || !itemList.Any()) return BadRequest();

            var sale = new Sale
            {
                InvoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                SaleDate = DateTime.Now,
                CustomerId = customerId > 0 ? customerId : null,
                PaymentMethod = paymentMethod,
                Discount = disc,
                Items = new List<SaleItem>()
            };

            decimal subTotal = 0;
            foreach (var item in itemList)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product == null) continue;
                var saleItem = new SaleItem
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = item.Quantity,
                    Total = product.Price * item.Quantity
                };
                subTotal += saleItem.Total;
                sale.Items.Add(saleItem);
                product.Stock -= item.Quantity;
            }

            sale.SubTotal = subTotal;
            sale.Total = subTotal - sale.Discount;

            if (sale.CustomerId.HasValue)
            {
                var customer = await _db.Customers.FindAsync(sale.CustomerId.Value);
                if (customer != null)
                {
                    customer.TotalPurchases += sale.Total;
                    customer.Points += (int)(sale.Total / 10);
                }
            }

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();
            return Json(new { success = true, invoiceNumber = sale.InvoiceNumber, total = sale.Total, id = sale.Id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var sale = await _db.Sales.Include(s => s.Items).Include(s => s.Customer).FirstOrDefaultAsync(s => s.Id == id);
            if (sale == null) return NotFound();
            return View(sale);
        }

        public async Task<IActionResult> Print(int id)
        {
            var sale = await _db.Sales.Include(s => s.Items).Include(s => s.Customer).FirstOrDefaultAsync(s => s.Id == id);
            if (sale == null) return NotFound();
            return View(sale);
        }
    }

    public class SaleItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
