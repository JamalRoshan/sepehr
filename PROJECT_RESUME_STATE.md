# PROJECT_RESUME_STATE

## PROJECT / NON-NEGOTIABLE RULES
- پیام‌رسان سازمانی سپهر؛ مرجع: docs/spec/01 تا 08. نسخه 4.8-Lite ممنوع.
- کلاینت WPF باید Windows 7 SP1 و 8.1 (و 10 و 11) را پشتیبانی کند؛ حذف یا تنزل ممنوع.
- ي→ی و ك→ک در متن؛ هرگز روی رمز، توکن، شناسه یا داده باینری.
- هیچ مدیری محتوای خصوصی یا فضای شخصی دیگران را نمی‌بیند.
- بدون Placeholder؛ تست اجرانشده Passed نیست؛ گزارش مرحله‌ای از کارفرما خواسته نشود.

## LAST VERIFIED CHECKPOINT
- 2026-11-26 / CP-002
- مخزن: https://github.com/JamalRoshan/sepehr (عمومی)، شاخه main
- Commit آزموده‌شده: d94fcbb؛ شاهد: https://github.com/JamalRoshan/sepehr/actions/runs/45703046348

## CURRENT PHASE / SUBTASK
- هسته تأیید شد. گام بعد T-010 و T-011. T-001 همچنان Blocked.

## COMPLETED (with paths/evidence)
- T-002: Sepehr.sln، Build با 0 خطا و 0 هشدار.
- هسته: 33 آزمون Unit، 13 آزمون Compat روی net10.0 و 13 روی net48؛ Migration و smoke.sql روی SQL Server 2022.

## IN PROGRESS / PARTIAL
- T-003 تا T-008: بخش نوشته‌شده Passed، دامنه Taskها ناقص.
- Application، Api، Realtime، Desktop و Web وجود ندارند.

## FILES CHANGED
- اولین ارسال همه فایل‌ها؛ سپس به‌روزرسانی فایل‌های وضعیت.

## BUILD / TEST STATUS
- Command: .github/workflows/build.yml
- Status: Passed برای هسته. Windows 7/8.1: Blocked. بقیه سناریوهای سند 06: NotRun.
- Evidence: VALIDATION.md

## OPEN ISSUES / BLOCKERS
- B-2: VM یا دستگاه Windows 7 SP1 و 8.1.
- محیط نشست SDK ندارد؛ Build از طریق CI مخزن انجام می‌شود.
- قالب صوت (D-14)، نصب‌کننده و Encryption at rest تصمیم‌گیری نشده‌اند.

## DECISIONS MADE
- C-01 تا C-06 و D-01 تا D-14 در DECISIONS.md.

## NEXT ACTIONS
1. T-010: Sepehr.Application و دسترسی داده.
2. T-011: Api با ورود، تغییر رمز، نشست، قفل، Rate Limit، Audit و آزمون یکپارچه با SQL Server در CI.
3. T-012: Wizard راه‌اندازی و اولین مدیرکل.
4. افزودن آزمون عمق گروه و هم‌زمانی ترتیب Commit در Sync.
5. T-013 و T-014.
6. T-015 و T-016.
7. T-017 تا T-019.
8. T-001 و T-020: WPF net48 (آزمون Win7/8.1 و تصمیم صوت).
9. T-021.
10. T-022 تا T-025.

## RESUME COMMAND
- متن RESUME_AI_INSTRUCTIONS.txt را همراه نشانی مخزن ارسال کنید.
