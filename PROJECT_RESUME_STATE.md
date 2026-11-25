# PROJECT_RESUME_STATE

## PROJECT / NON-NEGOTIABLE RULES
- پیام‌رسان سازمانی سپهر؛ مرجع: docs/spec/01 تا 08. نسخه 4.8-Lite ممنوع.
- کلاینت WPF باید Windows 7 SP1 و 8.1 (و 10 و 11) را پشتیبانی کند؛ حذف یا تنزل ممنوع.
- ي→ی و ك→ک در متن؛ هرگز روی رمز، توکن، شناسه یا داده باینری.
- هیچ مدیری محتوای خصوصی یا فضای شخصی دیگران را نمی‌بیند.
- بدون Placeholder؛ تست اجرانشده Passed نیست؛ گزارش مرحله‌ای از کارفرما خواسته نشود.

## LAST VERIFIED CHECKPOINT
- 2026-11-26 / CP-001
- Commit کد: 1cfda9b (این فایل در Commit بعدی ثبت شده است)
- تأییدشده فقط به روش ایستا (VALIDATION.md). پروژه ناقص است.

## CURRENT PHASE / SUBTASK
- T-002 تا T-008: کد نوشته شده، در انتظار نخستین Build. T-001: Blocked.

## COMPLETED (with paths/evidence)
- هیچ Task اجرایی هنوز با Build تأیید نشده است.
- مطالعه اسناد، تصمیم‌ها (DECISIONS.md)، یافته‌های Win7 (docs/PHASE0_COMPATIBILITY.md).

## IN PROGRESS / PARTIAL
- src/ (Shared، Contracts، Domain، Infrastructure)، database/، tests/، CI.
- Contracts فقط جزئی است. Application، Api، Realtime، Desktop و Web وجود ندارند.

## FILES CHANGED
- همه فایل‌های مخزن جدیدند (git ls-files).

## BUILD / TEST STATUS
- Command: dotnet test Sepehr.sln -c Release
- Status: Blocked (SDK موجود نیست). SQL و CI: NotRun.
- Evidence: VALIDATION.md

## OPEN ISSUES / BLOCKERS
- B-1: برای Build به محیط دارای .NET 10 SDK نیاز است (GitHub Actions، ویندوز محلی، یا محیطی با دسترسی بسته).
  فعال‌کردن دسترسی شبکه ممکن است نصب SDK را تضمین نکند؛ WPF در هر حال روی Windows ساخته شود.
- B-2: VM یا دستگاه Windows 7 SP1 و 8.1.
- B-3: SQL Server 2022 (CI آن را برای آزمون Migration فراهم می‌کند).
- قالب صوت (D-14)، نصب‌کننده و Encryption at rest هنوز تصمیم‌گیری نشده‌اند.

## DECISIONS MADE
- C-01 تا C-06 و D-01 تا D-14 در DECISIONS.md.

## NEXT ACTIONS
1. اجرای `.github/workflows/build.yml` (یا dotnet build/test) و رفع همه خطاها؛ ثبت نتیجه در VALIDATION.md.
2. رفع خطاهای Migration و smoke.sql روی SQL Server 2022.
3. T-001: اسپایک WPF net48 با SignalR، TLS و ضبط صوت؛ آزمون روی Win7/8.1 (حل D-14).
4. T-010: Sepehr.Application و دسترسی داده.
5. T-011: Api با ورود، تغییر رمز، نشست، قفل، Rate Limit، Audit + آزمون یکپارچه.
6. T-012: Wizard راه‌اندازی و اولین مدیرکل.
7. T-013 و T-014: کاربران، Excel، گروه‌ها.
8. T-015 و T-016: پیام، Hub، Sync (همراه آزمون هم‌زمانی ترتیب Commit).
9. T-017 تا T-019.
10. T-020 تا T-025.

## RESUME COMMAND
- متن RESUME_AI_INSTRUCTIONS.txt را همراه همین ZIP (و خروجی CI در صورت وجود) ارسال کنید.
