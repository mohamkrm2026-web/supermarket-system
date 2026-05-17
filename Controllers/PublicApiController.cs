using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Data;
using SuperMarket.Models;
using SuperMarket.Models.Api;

namespace SuperMarket.Controllers
{
    [ApiController]
    [Route("api/[action]")]
    public class PublicApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PublicApiController(AppDbContext db)
        {
            _db = db;
        }

        // ══════════════════════════════════════════════
        // GET /api/products
        // يرجع كل المنتجات المتاحة للموقع
        // ══════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Products([FromQuery] string? category = null)
        {
            var query = _db.Products
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            var products = await query
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .Select(p => new ProductDto
                {
                    Id       = p.Id,
                    Name     = p.Name,
                    Category = p.Category,
                    Price    = p.Price,
                    Unit     = p.Unit,
                    InStock  = p.Stock > 0
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<ProductDto>>
            {
                Success = true,
                Data    = products
            });
        }

        // ══════════════════════════════════════════════
        // GET /api/categories
        // يرجع قائمة الأقسام الموجودة
        // ══════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var cats = await _db.Products
                .Where(p => p.IsActive)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Data    = cats
            });
        }

        // ══════════════════════════════════════════════
        // POST /api/placeorder
        // العميل يبعت طلب جديد من الموقع
        // ══════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest req)
        {
            if (req.Items == null || req.Items.Count == 0)
                return BadRequest(new PlaceOrderResponse
                {
                    Success = false,
                    Message = "الطلب فارغ!"
                });

            if (string.IsNullOrWhiteSpace(req.CustomerName) || string.IsNullOrWhiteSpace(req.CustomerPhone))
                return BadRequest(new PlaceOrderResponse
                {
                    Success = false,
                    Message = "الاسم ورقم التليفون مطلوبين"
                });

            // البحث عن العميل أو إنشاؤه
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.Phone == req.CustomerPhone);

            if (customer == null)
            {
                customer = new Customer
                {
                    Name  = req.CustomerName,
                    Phone = req.CustomerPhone,
                    Notes = req.Address
                };
                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();
            }

            // بناء الفاتورة
            decimal subTotal = 0;
            var saleItems = new List<SaleItem>();

            foreach (var item in req.Items)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product == null || !product.IsActive || product.Stock < item.Quantity)
                    continue;

                var lineTotal = product.Price * item.Quantity;
                subTotal += lineTotal;

                saleItems.Add(new SaleItem
                {
                    ProductId   = product.Id,
                    ProductName = product.Name,
                    Price       = product.Price,
                    Quantity    = item.Quantity,
                    Total       = lineTotal
                });

                // تقليل المخزون
                product.Stock -= item.Quantity;
            }

            if (saleItems.Count == 0)
                return BadRequest(new PlaceOrderResponse
                {
                    Success = false,
                    Message = "المنتجات المطلوبة غير متاحة أو نفد مخزونها"
                });

            var invoiceNumber = "WEB-" + DateTime.Now.ToString("yyyyMMddHHmmss");

            var sale = new Sale
            {
                InvoiceNumber = invoiceNumber,
                SaleDate      = DateTime.Now,
                CustomerId    = customer.Id,
                SubTotal      = subTotal,
                Discount      = 0,
                Total         = subTotal,
                PaymentMethod = "أونلاين",
                Items         = saleItems
            };

            _db.Sales.Add(sale);

            // تحديث إجمالي مشتريات العميل
            customer.TotalPurchases += subTotal;

            await _db.SaveChangesAsync();

            return Ok(new PlaceOrderResponse
            {
                Success     = true,
                Message     = $"تم استلام طلبك بنجاح! سيتم التواصل معك على {req.CustomerPhone}",
                OrderNumber = invoiceNumber,
                Total       = subTotal
            });
        }

        // ══════════════════════════════════════════════
        // GET /api/checkorder?phone=01012345678
        // العميل يتابع حالة طلبه
        // ══════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> CheckOrder([FromQuery] string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return BadRequest(new { error = "رقم التليفون مطلوب" });

            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.Phone == phone);

            if (customer == null)
                return NotFound(new { error = "مفيش طلبات لهذا الرقم" });

            var orders = await _db.Sales
                .Where(s => s.CustomerId == customer.Id)
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .Select(s => new
                {
                    s.InvoiceNumber,
                    s.SaleDate,
                    s.Total,
                    s.PaymentMethod,
                    ItemCount = s.Items.Count
                })
                .ToListAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data    = new { customer = customer.Name, orders }
            });
        }
    }
}
