using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using TAP22_23.AuctionSite.Interface;

namespace Menghini {
    public static class Helpers {
        // Dimensione del salt in byte
        private const int SaltSize = 16; 

        public static string HashPassword(string password) {
            byte[] salt = GenerateSalt();
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltedPasswordBytes = new byte[salt.Length + passwordBytes.Length];

            Array.Copy(salt, 0, saltedPasswordBytes, 0, salt.Length);
            Array.Copy(passwordBytes, 0, saltedPasswordBytes, salt.Length, passwordBytes.Length);

            using (SHA512 sha512 = SHA512.Create()) {
                byte[] hashBytes = sha512.ComputeHash(saltedPasswordBytes);
                byte[] hashWithSaltBytes = new byte[salt.Length + hashBytes.Length];

                Array.Copy(salt, 0, hashWithSaltBytes, 0, salt.Length);
                Array.Copy(hashBytes, 0, hashWithSaltBytes, salt.Length, hashBytes.Length);

                return Convert.ToBase64String(hashWithSaltBytes);
            }
        }

        public static bool VerifyHashPassword(string hashPass, string password) {
            byte[] hashWithSaltBytes = Convert.FromBase64String(hashPass);
            byte[] salt = new byte[SaltSize];
            Array.Copy(hashWithSaltBytes, 0, salt, 0, SaltSize);

            string hashedPassword = HashPasswordWithSalt(password, salt);
            return string.Equals(hashPass, hashedPassword);
        }


        public static void CanConnectToDb(ContextDB ctx) {
            if (!ctx.Database.CanConnect())
                throw new AuctionSiteUnavailableDbException(
                    "The DB server is not responding or returns an unexpected error.");
        }

        public static void CheckWebSite(ContextDB ctx, int SiteId) {
            var querySite = from site in ctx.Sites where site.SiteID == SiteId select site;
            if (querySite.SingleOrDefault() == null)
                throw new AuctionSiteInvalidOperationException("No website with id " + SiteId + " has been found.");
        }

        private static byte[] GenerateSalt() {
            using (var rng = new RNGCryptoServiceProvider()) {
                byte[] salt = new byte[SaltSize];
                rng.GetBytes(salt);
                return salt;
            }
        }

        private static string HashPasswordWithSalt(string password, byte[] salt) {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltedPasswordBytes = new byte[salt.Length + passwordBytes.Length];

            Array.Copy(salt, 0, saltedPasswordBytes, 0, salt.Length);
            Array.Copy(passwordBytes, 0, saltedPasswordBytes, salt.Length, passwordBytes.Length);

            using (SHA512 sha512 = SHA512.Create()) {
                byte[] hashBytes = sha512.ComputeHash(saltedPasswordBytes);
                byte[] hashWithSaltBytes = new byte[salt.Length + hashBytes.Length];

                Array.Copy(salt, 0, hashWithSaltBytes, 0, salt.Length);
                Array.Copy(hashBytes, 0, hashWithSaltBytes, salt.Length, hashBytes.Length);

                return Convert.ToBase64String(hashWithSaltBytes);
            }
        }
    }
}

