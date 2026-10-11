using EcomPhash.AdminSetup;
using Microsoft.AspNetCore.Mvc;

namespace EcomPhash.Controllers
{
    public class AdminSetupController : Controller
    {
        public IActionResult GeneratePasswordHash()
        {
            string hash =
                PasswordHashGenerator.GenerateAdminPasswordHash();

            return Content(hash);
        }
    }
}