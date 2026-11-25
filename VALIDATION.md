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

## Build و آزمون (GitHub Actions، Commit d94fcbb)
شاهد: https://github.com/JamalRoshan/sepehr/actions/runs/45703046348

| مورد | وضعیت | نتیجه |
|---|---|---|
| Restore و Build (windows-latest) | Passed | 0 خطا، 0 هشدار |
| Sepehr.Tests.Unit، net10.0 | Passed | 33 از 33 |
| Sepehr.Tests.Compat، net10.0 | Passed | 13 از 13 |
| Sepehr.Tests.Compat، net48 | Passed | 13 از 13 (روی Runner؛ نه Windows 7/8.1) |
| Migration روی SQL Server 2022 (کانتینر Developer)، دو بار | Passed | بار دوم رد شد: already applied |
| smoke.sql | Passed | SMOKE OK؛ عمق بیش از 3 و هم‌زمانی Sync را نمی‌آزماید |
| بسته‌های آسیب‌پذیر (dotnet list package) | Passed | در منابع فعلی موردی گزارش نشد |

## ماتریس سند 06
| ID | وضعیت |
|---|---|
| T01 تا T17 | NotRun به‌عنوان سناریوی کامل (بیشتر قابلیت‌ها هنوز پیاده نشده‌اند) |
| T18 معماری | NotRun کامل؛ آزمون جزئی ارجاع پروژه‌ها Passed |
| Windows 7 SP1 و 8.1 | Blocked |

## امنیت
گزارش امنیتی یا آزمون نفوذ انجام نشده است. فقط بازبینی کد توسط نویسنده آن صورت گرفته است.
