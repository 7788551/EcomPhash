using Microsoft.AspNetCore.Mvc;

namespace EcomPhash.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
