using System;
using System.Globalization;

namespace Sepehr.Shared
{
    /// <summary>تبدیل نمایشی تاریخ. ذخیره‌سازی همیشه UTC است.</summary>
    public static class JalaliDate
    {
        private static readonly PersianCalendar Calendar = new PersianCalendar();

        /// <param name="localTime">زمان محلی (تبدیل منطقه زمانی با فراخواننده است).</param>
        public static string Format(DateTime localTime) =>
            string.Format(
                CultureInfo.InvariantCulture,
                "{0:0000}/{1:00}/{2:00}",
                Calendar.GetYear(localTime),
                Calendar.GetMonth(localTime),
                Calendar.GetDayOfMonth(localTime));
    }
}
