using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace prjFriendlyFoodWebAPI.Services.Member
{
    public class EncodeServices
    {
        public async Task<string> HashPassword(string p)
        {

            byte[] pBytes = Encoding.UTF8.GetBytes(p);
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            var argon2 = new Argon2id(pBytes)
            {
                Salt = salt,
                DegreeOfParallelism = 8,
                MemorySize = 65536,
                Iterations = 4
            };
            byte[] hash = await argon2.GetBytesAsync(32);
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }
        public async Task<bool> VerifyPassword(string p, string fPassword)
        {
            string[] parts = fPassword.Split('.');
            if (parts.Length != 2) return false;
            byte[] pBytes = Encoding.UTF8.GetBytes(p);
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] oHash = Convert.FromBase64String(parts[1]);
            var argon2 = new Argon2id(pBytes)
            {
                Salt = salt,
                DegreeOfParallelism = 8,
                MemorySize = 65536,
                Iterations = 4
            };
            byte[] nHash = await argon2.GetBytesAsync(32);

            return CryptographicOperations.FixedTimeEquals(nHash, oHash);
        }
    }
}
