namespace EcomPhash.AdminSetup
{
    public class PasswordHashGenerator
    {
        public static string GenerateAdminPasswordHash()
        {
            string password = "Admin@123";

            string hash = BCrypt.Net.BCrypt.HashPassword(password);

            return hash;
        }
    }
}
