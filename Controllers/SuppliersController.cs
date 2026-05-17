using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Data;
using SuperMarket.Models;

namespace SuperMarket.Controllers
{
    [Authorize(Roles = "Admin,مدير مخزن,مشاهدة فقط")]
    public class SuppliersController : Controller
    {
        private readonly AppDbContext _db;
        public SuppliersController(AppDbContext db) { _db = db; }

        // ========== الموردين ==========

        public async Task<IActionResult> Index(string? search)
        {
            var q = _db.Suppliers.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                q = q.Where(s => s.Name.Contains(search) || (s.Phone != null && s.Phone.Contains(search)));
            ViewBag.Search = search;
            return View(await q.OrderBy(s => s.Name).ToListAsync());
        }

        [Authorize(Roles = "Admin,مدير مخزن")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "Admin,مدير مخزن")]
        public async Task<IActionResult> Create(Supplier s)
        {
            s.CreatedAt = DateTime.Now;
            _db.Suppliers.Add(s);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم إضافة المورد!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,مدير مخزن")]
        public async Task<IActionResult> Edit(int id)
        {
            var s = await _db.Suppliers.FindAsync(id);
            if (s == null) return NotFound();
            return View(s);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,مدير مخزن")]
        public async Task<IActionResult> EditSave(Supplier s)
        {
            _db.Update(s);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم تحديث المورد!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,مدير مخزن")]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _db.Suppliers.FindAsync(id);
            if (s != null) { _db.Suppliers.Remove(s); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var s = await _db.Suppliers.FindAsync(id);
            if (s == null) return NotFound();
            var orders = await _db.PurchaseOrders
                .Where(o => o.SupplierId == id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            ViewBag.Supplier = s;
            ViewBag.Orders = orders;
            return View();
        }

        // ========== أوامر الشراء ==========

        [Authorize(Roles = "Admin,مدير مخزن")]
        public async Task<IActionResult> NewOrder(int id)
        {
            var supplier = await _db.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();
            ViewBag.Supplier = supplier;
            ViewBag.Products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,مدير مخزن")]
        public async Task<IActionResult> SaveOrder(int supplierId, string? notes, string items)
        {
            var itemList = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemDto>>(items,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (itemList == null || !itemList.Any())
            {
                TempData["Error"] = "يجب إضافة منتج واحد على الأقل";
                return RedirectToAction(nameof(NewOrder), new { id = supplierId });
            }

            var order = new PurchaseOrder
            {
                OrderNumber = "PO-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                OrderDate = DateTime.Now,
                SupplierId = supplierId,
                Notes = notes,
                Status = "معلق",
                Items = new List<PurchaseOrderItem>()
            };

            decimal total = 0;
            foreach (var item in itemList)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product == null) continue;
                var orderItem = new PurchaseOrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    Total = item.Quantity * item.UnitCost
                };
                total += orderItem.Total;
                order.Items.Add(orderItem);
            }

            order.Total = total;
            _db.PurchaseOrders.Add(order);
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم إنشاء أمر الشراء!";
            return RedirectToAction(nameof(OrderDetails), new { id = order.Id });
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _db.PurchaseOrders
                .Include(o => o.Supplier)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,مدير مخزن")]
        public async Task<IActionResult> ReceiveOrder(int id)
        {
            var order = await _db.PurchaseOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (order.Status == "معلق")
            {
                // تحديث المخزون
                foreach (var item in order.Items)
                {
                    var product = await _db.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                        product.CostPrice = item.UnitCost; // تحديث سعر التكلفة
                    }
                }
                order.Status = "مستلم";
                await _db.SaveChangesAsync();
                TempData["Success"] = "تم استلام الطلب وتحديث المخزون!";
            }

            return RedirectToAction(nameof(OrderDetails), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,مدير مخزن")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _db.PurchaseOrders.FindAsync(id);
            if (order != null && order.Status == "معلق")
            {
                order.Status = "ملغي";
                await _db.SaveChangesAsync();
                TempData["Success"] = "تم إلغاء الطلب";
            }
            return RedirectToAction(nameof(OrderDetails), new { id });
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _db.PurchaseOrders
                .Include(o => o.Supplier)
                .OrderByDescending(o => o.OrderDate)
                .Take(50)
                .ToListAsync();
            return View(orders);
        }
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}
