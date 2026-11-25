using System;
using System.Security.Cryptography;
using System.Text;

namespace Sepehr.Infrastructure.Security
{
    /// <summary>توکن Opaque با 256 بیت آنتروپی؛ فقط Hash آن در DB ذخیره می‌شود.</summary>
    public static class SessionToken
    {
        public static (string Token, byte[] TokenHash) Create()
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return (token, ComputeHash(token));
        }

        public static byte[] ComputeHash(string token)
        {
            ArgumentNullException.ThrowIfNull(token);
            return SHA256.HashData(Encoding.UTF8.GetBytes(token));
        }
    }
}
