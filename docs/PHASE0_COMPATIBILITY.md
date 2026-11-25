# Phase 0: محیط و سازگاری

**وضعیت Gate: Blocked.** هیچ آزمونی روی Windows 7 SP1 یا 8.1 اجرا نشده است.

## محیط ساخت این نشست
Ubuntu 24.04؛ بدون .NET SDK، SQL Server، Windows یا ابزار ساخت نصب‌کننده. نصب بسته‌ها غیرفعال بود.

## نسخه‌ها (Proposed؛ با Build تأیید نشده)
- Backend: .NET 10 (LTS)، SDK 10.0.1xx
- Desktop: WPF روی .NET Framework 4.8 (نه 4.8.1، چون Windows 7 و 8.1 را پشتیبانی نمی‌کند)
- Shared و Contracts: netstandard2.0

## یافته‌های اثرگذار

### F-1: WebSocket روی Windows 7
`ClientWebSocket` در .NET Framework فقط از Windows 8 به بعد کار می‌کند.
تصمیم: روی Windows 7 کلاینت فقط Server-Sent Events و LongPolling را فعال کند
(`Sepehr.Shared.ClientTransportPolicy`). بار سرور باید با درصد واقعی کلاینت‌های Windows 7 آزموده شود.
منبع: https://learn.microsoft.com/aspnet/signalr/overview/getting-started/supported-platforms
(این صفحه درباره SignalR کلاسیک است؛ رفتار کلاینت ASP.NET Core SignalR باید روی دستگاه واقعی تأیید شود.)

### F-2: رمزنگاری TLS در Windows 7
فهرست پیش‌فرض Windows 7 مجموعه `ECDHE_RSA + AES-GCM` ندارد، ولی `ECDHE_ECDSA + AES-GCM` دارد.
منبع: https://learn.microsoft.com/en-us/windows/win32/secauthn/tls-cipher-suites-in-windows-7
تصمیم پیشنهادی: گواهی سرور از CA داخلی با کلید ECDSA P-256.
بررسی لازم: فعال بودن این مجموعه در Windows Server 2022 سازمان و سیاست TLS آن.
TLS 1.2 باید روی Windows 7 آزموده شود (تنظیم SChannel یا انتخاب صریح در برنامه).

## موارد تأییدنشده که روی دستگاه واقعی باید بررسی شوند
- پیش‌نیازهای نصب .NET Framework 4.8 روی Windows 7 (به‌روزرسانی پشتیبانی امضای SHA-2 و ریشه‌های گواهی)
- نسخه سازگار `Microsoft.AspNetCore.SignalR.Client` با net48
- ضبط و پخش صوت. پیشنهاد اولیه: Opus در WebM برای مرورگر پخش بومی در WPF ندارد؛
  پیش از انتخاب قالب، هر دو کلاینت باید آزموده شوند. **تصمیم هنوز باز است.**
- نصب‌کننده آفلاین (ابزار هنوز انتخاب نشده است)
- فونت وزیرمتن: فایل‌های TTF ایستا (نه Variable) برای WPF در `assets/fonts` قرار دارند
- Collation با داده واقعی (پیشنهاد: `Persian_100_CI_AS_SC`)

## ماتریس آزمون OS
| سیستم | 32 بیت | 64 بیت |
|---|---|---|
| Windows 7 SP1 | Blocked | Blocked |
| Windows 8.1 | Blocked | Blocked |
| Windows 10 | NotRun | NotRun |
| Windows 11 | موجود نیست | NotRun |
