# Validation (2026-11-26)

محیط: Ubuntu 24.04، Python 3. بدون .NET، SQL Server و Windows.

## بررسی ایستا (اجرا شد)
اسکریپت موقت Python؛ این بررسی‌ها **جای کامپایل یا آزمون نیستند**.
| بررسی | نتیجه |
|---|---|
| XML فایل‌های csproj و props، و global.json | Passed |
| وجود 6 پروژه Solution و مسیر ProjectReferenceها | Passed |
| تطابق 24 کد مجوز در C# و Seed SQL | Passed |
| توازن پرانتز و آکولاد فایل‌های C# | Passed |
| تجزیه YAML گردش‌کار | Passed |

## Build و آزمون
| مورد | وضعیت | دلیل |
|---|---|---|
| Restore و Build | Blocked | SDK در محیط نیست؛ نسخه بسته‌ها هم تأیید نشده |
| آزمون واحد، معماری و سازگاری (net10.0) | NotRun | نیاز به SDK |
| آزمون Shared روی net48 | NotRun | نیاز به Windows |
| Migration و smoke.sql | NotRun | SQL Server نیست |
| گردش‌کار CI | NotRun | هنوز روی GitHub اجرا نشده |
| بررسی آسیب‌پذیری وابستگی‌ها | NotRun | نیاز به Restore |

## ماتریس سند 06
| ID | وضعیت |
|---|---|
| T01 تا T17 | NotRun (بیشتر قابلیت‌ها هنوز پیاده نشده‌اند) |
| T18 معماری | NotRun (آزمون جزئی نوشته شده است) |
| Windows 7 SP1 و 8.1 | Blocked |

## امنیت
گزارش امنیتی یا آزمون نفوذ انجام نشده است. فقط بازبینی کد توسط نویسنده آن صورت گرفته است.
