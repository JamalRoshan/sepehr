using System;
using Sepehr.Shared;
using Xunit;

namespace Sepehr.Tests.Compat
{
    public class SharedTests
    {
        [Fact]
        public void Storage_normalizes_only_yeh_and_kaf()
        {
            Assert.Equal("علی کریمی ۱۲‌ها", PersianText.NormalizeForStorage("علي كريمي ۱۲‌ها"));
            Assert.Null(PersianText.NormalizeForStorage(null));
        }

        [Theory]
        [InlineData("علي  كريمي", "علی کریمی")]
        [InlineData("۱۲۳ ١٢٣ 123", "123 123 123")]
        [InlineData("می‌روم", "میروم")]
        [InlineData("مُحَمَّد", "محمد")]
        [InlineData("سـلام", "سلام")]
        [InlineData("  ABC  ", "abc")]
        [InlineData("", "")]
        public void Search_normalization_follows_documented_rules(string input, string expected) =>
            Assert.Equal(expected, PersianText.NormalizeForSearch(input));

        [Fact]
        public void Jalali_format_handles_new_year()
        {
            Assert.Equal("1405/01/01", JalaliDate.Format(new DateTime(2026, 3, 21)));
        }

        [Theory]
        [InlineData(6, 1, false)] // Windows 7
        [InlineData(6, 2, true)]
        [InlineData(6, 3, true)] // Windows 8.1
        [InlineData(10, 0, true)]
        public void Websocket_transport_depends_on_windows_version(int major, int minor, bool expected) =>
            Assert.Equal(expected, ClientTransportPolicy.CanUseWebSockets(new Version(major, minor)));
    }
}
