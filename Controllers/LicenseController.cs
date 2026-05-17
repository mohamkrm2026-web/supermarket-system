using Microsoft.AspNetCore.Mvc;
using SuperMarket.Services;

namespace SuperMarket.Controllers
{
    public class LicenseController : Controller
    {
        public IActionResult Expired()
        {
            ViewBag.ExpiryDate = LicenseService.GetExpiry().ToString("yyyy-MM-dd");
            return View();
        }
    }
}
