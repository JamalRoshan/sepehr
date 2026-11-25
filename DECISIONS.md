# Decision Log (تاریخ همه: 2026-11-26)

## تعارض‌ها
| ID | تعارض | حل | وضعیت |
|---|---|---|---|
| C-01 | 05: `Announcement.Manage` در برابر 04: `Create/Schedule` | دو مجوز دقیق سند 04 | Approved |
| C-02 | MessageType در 03 دارای System و بدون Announcement | اجتماع هر دو (7 نوع) | Approved |
| C-03 | 01§12 «مسئول گروه» | همان GroupCoordinator | Approved |
| C-04 | 02§15 اجازه HTTP | پیوست اصلاحات: HTTPS در محیط نهایی | Approved |
| C-05 | توقف و تأیید فازها در 07/08 | الحاقیه اجرای خودکار | Approved |
| C-06 | Gate اجباری Legacy و ادامه خودکار | توسعه ادامه می‌یابد؛ Gate و پذیرش Blocked می‌ماند | Approved |

## تصمیم‌ها
| ID | تصمیم | دلیل / اثر | وضعیت |
|---|---|---|---|
| D-01 | Backend روی .NET 10؛ Desktop روی net48 | LTS و الزام Win7/8.1 | Approved برای Backend و Shared (CI)؛ Desktop تا Build آن Proposed |
| D-02 | Win7: فقط SSE/LongPolling | نبود ClientWebSocket | Proposed تا آزمون |
| D-03 | گواهی ECDSA P-256 | TLS GCM با Windows 7 | Proposed تا آزمون |
| D-04 | توکن Opaque، بدون Refresh Token؛ 30 دقیقه idle و سقف 12 ساعت | ساده و قابل ابطال؛ `/auth/refresh` اختیاری 05 ساخته نمی‌شود | Approved |
| D-05 | PBKDF2-SHA256 با 600000 تکرار | داخلی .NET، بدون وابستگی | Approved |
| D-06 | Cursor از ردیف قفل‌شونده `SyncState` تا Commit | ترتیب Seq = ترتیب Commit؛ نوشتن‌ها سریال می‌شوند، باید آزمون بار شود | Proposed |
| D-07 | GroupCoordinator مجوز پیش‌فرض مدیریتی ندارد؛ جدول `GroupMemberPermissions` | 04: فقط مجوز صریح | Approved |
| D-08 | حذف برای خود در `MessageHidden` | از حذف برای همه جداست | Approved |
| D-09 | جست‌وجو: حذف نیم‌فاصله و اعراب و کشیده، ارقام لاتین | «می‌روم» با «میروم» منطبق است، با «می روم» نه | Approved |
| D-10 | رمز جدید برابر کد پرسنلی یا رمز فعلی پذیرفته نمی‌شود | جلوگیری از حفظ رمز اولیه | Approved |
| D-11 | فایل‌های Office دارای ماکرو و ISO/VHD اجرایی محسوب می‌شوند | کاهش ریسک | Approved |
| D-12 | فونت وزیرمتن 33.003 (OFL): TTF و WOFF2 در سه وزن | تحویل کارفرما، اجرای آفلاین | Approved |
| D-13 | پروژه خالی برای Api/Realtime/Application/Desktop/Web ساخته نمی‌شود | منع Placeholder | Approved |
| D-14 | تصمیم قالب صوتی | نیازمند آزمون WPF و مرورگر | باز |

| D-15 | مخزن jamalroshan/sepehr عمومی است و همه فایل‌ها شامل docs/spec ارسال شد | انتخاب صریح کارفرما. هیچ رمز یا کلید واقعی نباید Commit شود | Approved |

## Risk Register
| ID | ریسک | اثر | اقدام |
|---|---|---|---|
| R-01 | پوشش آزمون محدود به هسته | رفتار یکپارچه آزموده نشده | آزمون یکپارچه همراه API |
| R-02 | Win7/8.1 آزمون نشده | پذیرش نهایی ممکن نیست | VM واقعی |
| R-03 | LongPolling در Win7 بار بیشتر دارد | کارایی | آزمون بار با ترکیب واقعی |
| R-04 | سیستم‌عامل‌ها و Runtimeهای قدیمی وصله امنیتی کامل ندارند | امنیت | تفکیک شبکه؛ ارزیابی سازمان |
| R-05 | هم‌زمانی واقعی ممکن است بیش از 200 باشد | ظرفیت | عدد واقعی پیش از آزمون بار تعیین شود |
| R-06 | Encryption at rest طراحی نشده | حریم خصوصی در سطح زیرساخت | ارزیابی TDE و رمزنگاری دیسک فایل‌ها |
