using System;
using Sepehr.Infrastructure.Files;
using Sepehr.Infrastructure.Security;
using Xunit;

namespace Sepehr.Tests.Unit
{
    public class SecurityTests
    {
        [Fact]
        public void Password_hash_is_salted_and_verifiable()
        {
            var first = PasswordHasher.Hash("علي-12345");
            var second = PasswordHasher.Hash("علي-12345");
            Assert.NotEqual(first, second);
            Assert.True(PasswordHasher.Verify("علي-12345", first));
            // رمز نرمال‌سازی نمی‌شود
            Assert.False(PasswordHasher.Verify("علی-12345", first));
        }

        [Theory]
        [InlineData("")]
        [InlineData("plain")]
        [InlineData("pbkdf2-sha256$1$AAAAAAAAAAAAAAAAAAAAAA==$AAAA")]
        public void Malformed_or_weak_hash_is_rejected(string encoded) =>
            Assert.False(PasswordHasher.Verify("password1", encoded));

        [Fact]
        public void Token_is_random_and_only_hash_matches()
        {
            var (token, hash) = SessionToken.Create();
            var (other, _) = SessionToken.Create();
            Assert.Equal(43, token.Length);
            Assert.NotEqual(token, other);
            Assert.Equal(hash, SessionToken.ComputeHash(token));
        }

        [Theory]
        [InlineData("a.exe", FileVerdict.Executable)]
        [InlineData("a.EXE. ", FileVerdict.Executable)]
        [InlineData("report.pdf.bat", FileVerdict.Executable)]
        [InlineData("macro.docm", FileVerdict.Executable)]
        [InlineData("..\\evil.txt", FileVerdict.InvalidName)]
        [InlineData("a.txt:evil.exe", FileVerdict.InvalidName)]
        [InlineData("gpj.‮exe", FileVerdict.InvalidName)]
        [InlineData("...", FileVerdict.InvalidName)]
        public void Dangerous_names(string name, FileVerdict expected) =>
            Assert.Equal(expected, FileInspector.Inspect(name, ReadOnlySpan<byte>.Empty));

        [Fact]
        public void Content_is_checked_not_only_extension()
        {
            Assert.Equal(FileVerdict.Executable, FileInspector.Inspect("photo.jpg", "MZ\0\0"u8));
            Assert.Equal(FileVerdict.ContentMismatch, FileInspector.Inspect("photo.png", "hello"u8));
            Assert.Equal(FileVerdict.Accepted, FileInspector.Inspect("doc.pdf", "%PDF-1.7"u8));
        }
    }
}
