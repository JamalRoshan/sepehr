using Sepehr.Application.Auth;

namespace Sepehr.Infrastructure.Security
{
    public sealed class Pbkdf2PasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => PasswordHasher.Hash(password);
        public bool Verify(string password, string encodedHash) => PasswordHasher.Verify(password, encodedHash);
    }

    public sealed class OpaqueSessionTokens : ISessionTokens
    {
        public (string Token, byte[] TokenHash) Create() => SessionToken.Create();
        public byte[] ComputeHash(string token) => SessionToken.ComputeHash(token);
    }
}
