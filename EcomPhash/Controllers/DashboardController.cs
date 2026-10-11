using Microsoft.AspNetCore.Mvc;

namespace EcomPhash.Controllers
{
    public class DashboardController : Controller
    {
        // GET: /Dashboard/AdminDashboard
        public IActionResult AdminDashboard()
        {
            string? roleId = HttpContext.Session.GetString("RoleId");

            // User is not logged in
            // OR user is not an Admin
            if (roleId == null || roleId != "1")
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // GET: /Dashboard/UserDashboard
        public IActionResult UserDashboard()
        {
            string? roleId = HttpContext.Session.GetString("RoleId");

            // User is not logged in
            // OR user is not a normal User
            if (roleId == null || roleId != "2")
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }
    }
}