# PROJECT_RESUME_STATE

## PROJECT / NON-NEGOTIABLE RULES
- پیام‌رسان سازمانی سپهر؛ مرجع: docs/spec/01 تا 08. نسخه 4.8-Lite ممنوع.
- کلاینت WPF باید Windows 7 SP1 و 8.1 (و 10 و 11) را پشتیبانی کند؛ حذف یا تنزل ممنوع.
- ي→ی و ك→ک در متن؛ هرگز روی رمز، توکن، شناسه یا داده باینری.
- هیچ مدیری محتوای خصوصی یا فضای شخصی دیگران را نمی‌بیند.
- بدون Placeholder؛ تست اجرانشده Passed نیست؛ گزارش مرحله‌ای از کارفرما خواسته نشود.

## LAST VERIFIED CHECKPOINT
- 2026-11-26 / CP-003
- مخزن: https://github.com/JamalRoshan/sepehr (عمومی)، شاخه main
- Commit آزموده‌شده: f50de7d؛ شاهد: https://github.com/JamalRoshan/sepehr/actions/runs/45763085895

## CURRENT PHASE / SUBTASK
- API احراز هویت تأیید شد. گام بعد T-012. T-001 همچنان Blocked.

## COMPLETED (with paths/evidence)
- T-002 و T-011. Build با 0 خطا و 0 هشدار؛ 35 Unit، 13+13 Compat، 20 Integration (SQL Server 2022).

## IN PROGRESS / PARTIAL
- T-003 تا T-008، T-010 و T-022: بخش نوشته‌شده Passed، دامنه Taskها ناقص.
- Realtime، Desktop و Web وجود ندارند. هنوز راهی جز SQL برای ساخت کاربر نیست.

## FILES CHANGED
- src/Sepehr.Application، src/Sepehr.Api، src/Sepehr.Infrastructure/Data، tests/Sepehr.Tests.Integration، Contracts، CI، Sepehr.sln

## BUILD / TEST STATUS
- Command: .github/workflows/build.yml
- Status: Passed. Windows 7/8.1: Blocked. سناریوهای کامل سند 06: NotRun.
- Evidence: VALIDATION.md

## OPEN ISSUES / BLOCKERS
- B-2: VM یا دستگاه Windows 7 SP1 و 8.1.
- محیط نشست SDK ندارد؛ Build از طریق CI مخزن انجام می‌شود.
- قالب صوت (D-14)، نصب‌کننده و Encryption at rest تصمیم‌گیری نشده‌اند.
- ریسک‌های R-07 تا R-10.

## DECISIONS MADE
- C-01 تا C-06 و D-01 تا D-20 در DECISIONS.md.

## NEXT ACTIONS
1. T-012: Wizard راه‌اندازی (اتصال SQL، اجرای Migration) و ساخت اولین مدیرکل.
2. T-013 و T-014: کاربران، ورود Excel، گروه و مجوز.
3. T-015 و T-016: پیام، SignalR و Sync.
4. T-017 تا T-019.
5. T-001 و T-020: WPF net48 (آزمون Win7/8.1 و تصمیم صوت).
6. T-021.
7. T-022 تا T-025.

## RESUME COMMAND
- متن RESUME_AI_INSTRUCTIONS.txt را همراه نشانی مخزن ارسال کنید.
