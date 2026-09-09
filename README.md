# 📦 nuget-utils (ICOT Shared Packages)

این مخزن شامل کتابخانه‌های هسته مشترک پلتفرم **ICOT** بر بستر **.NET 10.0** است که به صورت بسته‌های NuGet برای استفاده در تمامی میکروسرویس‌های پلتفرم در GitHub Packages منتشر می‌شوند.

- **آدرس مخزن:** [https://github.com/pourghadiriHossein/nuget-utils](https://github.com/pourghadiriHossein/nuget-utils)

---

## 📚 پکیج‌های موجود

### ۱. `Icot.Shared.Kernel`
شامل ماژول‌های اساسی مورد استفاده در تمام سرویس‌ها:
- **مدیریت ارتباط و صف با RabbitMQ**: اینترفیس `IRabbitMqPublisher` و سرویس شنونده `RabbitMqListenerService`.
- **موجودیت‌ها و مدل‌های پایه (Entities & DTOs)**: کلاس‌های پایه شناسه‌ها، فیلترها و پاسخ‌های استاندارد API (`ApiResponse`).
- **میدلورها و فیلترها (Middlewares & Filters)**: مدیریت خطاها، ولیدیشن و لاگینگ.
- **توابع کمکی و اکستنشن‌ها (Extensions)**: ابزارهای کمکی LINQ و فرمت‌بندی JSON.

### ۲. `Icot.Shared.Messaging`
ماژول اختصاصی مدیریت پیام‌رسانی پیشرفته با **MassTransit** و **RabbitMQ**.

---

## 💻 نحوه استفاده در میکروسرویس‌ها

برای استفاده از این پکیج‌ها که روی GitHub Packages قرار دارند، باید فایل `nuget.config` را در کنار میکروسرویس خود قرار دهید (توجه کنید که گیت‌هاب برای دانلود پکیج‌ها نیازمند Token است):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="GitHub" value="https://nuget.pkg.github.com/pourghadiriHossein/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <GitHub>
      <add key="Username" value="pourghadiriHossein" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_PERSONAL_ACCESS_TOKEN" />
    </GitHub>
  </packageSourceCredentials>
</configuration>
```

سپس پکیج‌ها را به آسانی نصب کنید:

```bash
dotnet add package Icot.Shared.Kernel
dotnet add package Icot.Shared.Messaging
```

---

## 🚀 انتشار خودکار نسخه جدید (GitHub Actions)

این مخزن مجهز به اکشن‌های گیت‌هاب (GitHub Actions) است. با هر بار **پوش کردن کد روی برنچ `main`**، پایپ‌لاین به طور خودکار نسخه‌ی جدیدی از پکیج‌ها را ساخته و در GitHub Packages منتشر می‌کند (نسخه‌بندی به طور خودکار انجام می‌شود).
