using System.Text;

namespace Sepehr.Shared
{
    /// <summary>
    /// نرمال‌سازی فارسی. هرگز برای رمز عبور، توکن، شناسه فنی یا داده باینری استفاده نشود.
    /// </summary>
    public static class PersianText
    {
        private const char ArabicYeh = 'ي';
        private const char PersianYeh = 'ی';
        private const char ArabicKaf = 'ك';
        private const char PersianKaf = 'ک';
        private const char ZeroWidthNonJoiner = '‌';
        private const char Tatweel = 'ـ';

        /// <summary>برای متن ذخیره‌شونده: فقط ي→ی و ك→ک.</summary>
        public static string? NormalizeForStorage(string? text)
        {
            if (text == null) return null;
            return text.Replace(ArabicYeh, PersianYeh).Replace(ArabicKaf, PersianKaf);
        }

        /// <summary>
        /// کلید جست‌وجو (ستون SearchText جدا از متن اصلی). قواعد: ي→ی، ك→ک،
        /// ارقام فارسی و عربی به لاتین، حذف اعراب و کشیده و نیم‌فاصله،
        /// یکی‌کردن فاصله‌های متوالی و حروف کوچک لاتین.
        /// همین تابع باید روی عبارت جست‌وجو هم اعمال شود.
        /// </summary>
        public static string NormalizeForSearch(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var result = new StringBuilder(text!.Length);
            var pendingSpace = false;

            foreach (var original in text)
            {
                var c = original;
                if (IsDiacritic(c) || c == Tatweel || c == ZeroWidthNonJoiner) continue;

                if (char.IsWhiteSpace(c))
                {
                    pendingSpace = result.Length > 0;
                    continue;
                }

                if (c == ArabicYeh) c = PersianYeh;
                else if (c == ArabicKaf) c = PersianKaf;
                else if (c >= '۰' && c <= '۹') c = (char)('0' + (c - '۰'));
                else if (c >= '٠' && c <= '٩') c = (char)('0' + (c - '٠'));
                else c = char.ToLowerInvariant(c);

                if (pendingSpace)
                {
                    result.Append(' ');
                    pendingSpace = false;
                }

                result.Append(c);
            }

            return result.ToString();
        }

        private static bool IsDiacritic(char c) =>
            (c >= 'ً' && c <= 'ٟ') || c == 'ٰ';
    }
}
