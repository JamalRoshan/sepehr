using System;
using System.Globalization;
using System.Security.Cryptography;

namespace Sepehr.Infrastructure.Security
{
    /// <summary>PBKDF2-HMAC-SHA256 با نمک تصادفی. قالب: pbkdf2-sha256$iterations$salt$hash</summary>
    public static class PasswordHasher
    {
        private const string Algorithm = "pbkdf2-sha256";
        public const int Iterations = 600_000;
        private const int MaximumAcceptedIterations = 5_000_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string Hash(string password)
        {
            ArgumentNullException.ThrowIfNull(password);
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Derive(password, salt, Iterations);
            return string.Join('$', Algorithm, Iterations.ToString(CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public static bool Verify(string password, string encoded)
        {
            ArgumentNullException.ThrowIfNull(password);
            var parts = (encoded ?? string.Empty).Split('$');
            if (parts.Length != 4 || parts[0] != Algorithm) return false;
            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)) return false;
            if (iterations < Iterations || iterations > MaximumAcceptedIterations) return false;

            var salt = new byte[SaltSize];
            var expected = new byte[HashSize];
            if (!Convert.TryFromBase64String(parts[2], salt, out var saltLength) || saltLength != SaltSize) return false;
            if (!Convert.TryFromBase64String(parts[3], expected, out var hashLength) || hashLength != HashSize) return false;

            return CryptographicOperations.FixedTimeEquals(Derive(password, salt, iterations), expected);
        }

        private static byte[] Derive(string password, byte[] salt, int iterations) =>
            Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);
    }
}
