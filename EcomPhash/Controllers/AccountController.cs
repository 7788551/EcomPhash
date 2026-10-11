using EcomPhash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EcomPhash.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Generate JWT after successful login
        private string GenerateJwtToken(
            int userId,
            string userName,
            string email,
            int roleId)
        {
            string role = roleId == 1 ? "Admin" : "User";

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    userName
                ),

                new Claim(
                    ClaimTypes.Email,
                    email
                ),

                new Claim(
                    ClaimTypes.Role,
                    role
                )
            };

            // Read the Base64-encoded secret and decode it
            var key = new SymmetricSecurityKey(
                Convert.FromBase64String(
                    _configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException(
                        "JWT Key is missing from configuration."
                    )
                )
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    _configuration.GetValue<int>(
                        "Jwt:ExpiryMinutes"
                    )
                ),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public IActionResult Register(User obj)
        {
            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            string connectionString =
                _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection string is missing."
                );

            // Hash the password using BCrypt
            string passwordHash =
                BCrypt.Net.BCrypt.HashPassword(obj.PasswordHash);

            string query = @"
                INSERT INTO Users
                (
                    UserName,
                    Email,
                    PasswordHash,
                    RoleId
                )
                VALUES
                (
                    @UserName,
                    @Email,
                    @PasswordHash,
                    @RoleId
                )";

            using (SqlConnection con =
                new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue(
                    "@UserName", obj.UserName);

                cmd.Parameters.AddWithValue(
                    "@Email", obj.Email);

                cmd.Parameters.AddWithValue(
                    "@PasswordHash", passwordHash);

                // New registrations are ordinary users
                cmd.Parameters.AddWithValue("@RoleId", 2);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(User obj)
        {
            string connectionString =
                _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection string is missing."
                );

            string query = @"
                SELECT
                    UserId,
                    UserName,
                    Email,
                    PasswordHash,
                    RoleId
                FROM Users
                WHERE Email = @Email";

            using (SqlConnection con =
                new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Email", obj.Email);

                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        string storedPasswordHash =
                            dr["PasswordHash"].ToString()!;

                        // Verify entered password against BCrypt hash
                        bool isPasswordValid =
                            BCrypt.Net.BCrypt.Verify(
                                obj.PasswordHash,
                                storedPasswordHash
                            );

                        if (isPasswordValid)
                        {
                            int userId =
                                Convert.ToInt32(dr["UserId"]);

                            string userName =
                                dr["UserName"].ToString()!;

                            string email =
                                dr["Email"].ToString()!;

                            int roleId =
                                Convert.ToInt32(dr["RoleId"]);

                            // Generate JWT after BCrypt succeeds
                            string token = GenerateJwtToken(
                                userId,
                                userName,
                                email,
                                roleId
                            );

                            // Store JWT in an HttpOnly cookie
                            Response.Cookies.Append(
                                "jwt",
                                token,
                                new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = true,
                                    SameSite = SameSiteMode.Strict,

                                    Expires = DateTimeOffset.UtcNow
                                        .AddMinutes(
                                            _configuration.GetValue<int>(
                                                "Jwt:ExpiryMinutes"
                                            )
                                        )
                                }
                            );

                            // Keep existing Session temporarily
                            HttpContext.Session.SetString(
                                "UserId",
                                userId.ToString()
                            );

                            HttpContext.Session.SetString(
                                "UserName",
                                userName
                            );

                            HttpContext.Session.SetString(
                                "Email",
                                email
                            );

                            HttpContext.Session.SetString(
                                "RoleId",
                                roleId.ToString()
                            );

                            // Redirect based on role
                            if (roleId == 1)
                            {
                                return RedirectToAction(
                                    "AdminDashboard",
                                    "Dashboard"
                                );
                            }

                            return RedirectToAction(
                                "UserDashboard",
                                "Dashboard"
                            );
                        }
                    }
                }
            }

            ViewBag.Error = "Invalid Email or Password";

            return View(obj);
        }

        // GET: /Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            // Remove JWT cookie
            Response.Cookies.Delete(
                "jwt",
                new CookieOptions
                {
                    Secure = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict
                }
            );

            return RedirectToAction("Login");
        }
    }
}
