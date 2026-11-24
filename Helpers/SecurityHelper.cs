using System.Security.Cryptography;
using System.Text;

namespace Web_Food_App.Helpers
{
    public static class SecurityHelper
    {
        // SHA-256 dạng hex in hoa, khớp với CONVERT(varchar(64), HASHBYTES('SHA2_256', ...), 2) trong script SQL
        public static string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? ""));
                var sb = new StringBuilder(64);
                foreach (byte b in bytes) sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
        }
    }
}
