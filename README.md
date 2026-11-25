# سپهر: پیام‌رسان سازمانی

> **این نسخه کامل و قابل اجرا نیست.** فقط هسته اولیه است: قواعد دامنه و امنیت، نرمال‌سازی فارسی،
> طرح پایگاه داده، آزمون‌ها و CI. API، SignalR، کلاینت WPF، وب و نصب‌کننده هنوز ساخته نشده‌اند.
> هسته موجود در GitHub Actions با موفقیت Build و آزمون شده است؛ جزئیات در `VALIDATION.md`.

## Build و آزمون

### راه 1: GitHub Actions
1. با هر Push گردش‌کار خودکار اجرا می‌شود.
2. در تب Actions گردش‌کار `build-and-test` را ببینید.
   - `windows`: Restore، Build، آزمون روی net10.0 و بخش سازگاری روی net48، گزارش آسیب‌پذیری بسته‌ها
   - `database`: دو بار اجرای Migration روی SQL Server 2022 و آزمون قیود
3. در صورت خطا، متن Log را همراه ZIP برای ادامه کار بفرستید.

آزمون net48 روی Runner جای آزمون Windows 7 SP1 و 8.1 را نمی‌گیرد.

### راه 2: سیستم محلی ویندوز (Windows 10/11 با .NET 10 SDK)
```
dotnet restore Sepehr.sln
dotnet build Sepehr.sln -c Release
dotnet test Sepehr.sln -c Release
```
اجرای دیتابیس (SQL Server 2022 آزمایشی و خالی؛ Windows Authentication):
```
sqlcmd -S . -E -I -b -Q "CREATE DATABASE Sepehr COLLATE Persian_100_CI_AS_SC"
sqlcmd -S . -E -I -b -f 65001 -d Sepehr -i database\migrations\0001_initial.sql
sqlcmd -S . -E -I -b -f 65001 -d Sepehr -i database\tests\smoke.sql
```
انتظار: `SMOKE OK`. نسخه تولید باید SQL Server Agent داشته باشد (Express پذیرفته نیست).
پشتیبان‌گیری بر عهده زیرساخت است و پوشه فایل‌ها را هم باید شامل شود.

## ساختار
- `src/`: Shared و Contracts (netstandard2.0)، Domain و Infrastructure (net10.0)
- `database/`: Migration و آزمون قیود
- `tests/`: آزمون‌ها
- `assets/fonts/vazirmatn/`: فونت و مجوز OFL
- `docs/`: سازگاری Phase 0 و اسناد مرجع (`docs/spec`)
- `PROJECT_RESUME_STATE.md`، `TASKS.md`، `DECISIONS.md`، `VALIDATION.md`
