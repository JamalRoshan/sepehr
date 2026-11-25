# Tasks و ردیابی نیازمندی‌ها (شناسه‌ها ثابت‌اند)

Build و آزمون بخش‌های T-003 تا T-008 در CI موفق بوده است؛ InProgress یعنی دامنه آن Task هنوز کامل نیست.

وضعیت: NotStarted / InProgress / Blocked / Completed. «Completed» نیازمند شاهد اجراست،
بنابراین کد نوشته‌شده‌ای که Build نشده InProgress است.

| ID | کار | سند | وضعیت | مسیر / توضیح |
|---|---|---|---|---|
| T-001 | Phase 0 و اسپایک Win7/8.1 | 07 | Blocked | docs/PHASE0_COMPATIBILITY.md؛ اسپایک WPF ساخته نشده |
| T-002 | Solution و تنظیمات Build | 02 | Completed | Sepehr.sln (شش پروژه موجود)؛ CI Passed. افزودن پروژه‌های بعدی در Task خودشان |
| T-003 | نرمال‌سازی فارسی و تاریخ شمسی | 01§19 | InProgress | src/Sepehr.Shared |
| T-004 | قراردادها | 05 | InProgress | فقط enum، مسیرهای پایه، DTO ورود و پیام، رویدادهای Hub |
| T-005 | قواعد دامنه: مجوز، حریم خصوصی، زیرگروه، رمز، قفل، نشست | 04 | InProgress | src/Sepehr.Domain |
| T-006 | Hash رمز، توکن، بررسی فایل | 01§18 | InProgress | src/Sepehr.Infrastructure؛ اسکن بدافزار و ذخیره‌سازی ندارد |
| T-007 | Schema و Migration | 03 | InProgress | database/ |
| T-008 | آزمون واحد، معماری و CI | 06 | InProgress | tests/ و .github/workflows |
| T-010 | لایه Application و دسترسی داده (EF Core یا معادل) | 02 | NotStarted | |
| T-011 | API: احراز هویت، نشست، Rate Limit، Audit، خطا، CorrelationId | 05 | NotStarted | |
| T-012 | Wizard اتصال SQL و ساخت اولین مدیرکل | 08§13 | NotStarted | |
| T-013 | کاربران و ورود Excel | 05§3 | NotStarted | |
| T-014 | گروه، زیرگروه، عضویت و مجوز | 05§4 | NotStarted | |
| T-015 | گفتگو، پیام، رسید، ویرایش/حذف، جست‌وجو | 05§5 | NotStarted | |
| T-016 | SignalR Hub، حضور، Sync، Outbox | 05§9 | NotStarted | |
| T-017 | فایل، قرنطینه، اسکن، صوت | 05§7 | NotStarted | |
| T-018 | اعلان فوری/زمان‌بندی‌شده، Sticker/GIF | 05§6 | NotStarted | |
| T-019 | فضای شخصی، مخاطبان، Retention، تنظیمات، گزارش‌ها | 05§8 | NotStarted | |
| T-020 | WPF net48: پیام‌رسان، کنسول مدیریت، Tray، Outbox محلی | 01§6.1 | NotStarted | |
| T-021 | Web و پنل مدیریت (Bootstrap محلی، RTL) | 01§6.2 | NotStarted | فقط فونت آماده است |
| T-022 | OpenAPI و آزمون یکپارچه و مجوز Endpointها | 06 | NotStarted | |
| T-023 | آزمون امنیت و بار (100 و 200 هم‌زمان) | 06 | NotStarted | |
| T-024 | نصب‌کننده آفلاین، ارتقا و حذف | 07 | NotStarted | |
| T-025 | راهنمای نصب، مدیر و کاربر؛ گزارش امنیت | 07 | NotStarted | |
