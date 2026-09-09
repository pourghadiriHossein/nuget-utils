# 📦 ICOT Shared NuGet Packages

این مخزن شامل کتابخانه‌های هسته مشترک پلتفرم **ICOT** بر بستر **.NET 10.0** است که به صورت بسته‌های NuGet برای استفاده در تمامی میکروسرویس‌های پلتفرم منتشر می‌شوند.

- **آدرس مخزن:** [https://hamgit.ir/icot/icot-nuget](https://hamgit.ir/icot/icot-nuget)
- **رجیستری پکیج‌ها (Package Registry):** در بخش `Deploy > Package Registry` پروژه در هم‌گیت در دسترس است.

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

چون مخزن عمومی (Public) است، کافیست فایل `nuget.config` را در کنار میکروسرویس خود قرار دهید:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="Hamgit" value="https://hamgit.ir/api/v4/projects/icot%2Ficot-nuget/packages/nuget/index.json" />
  </packageSources>
</configuration>
```

سپس پکیج‌ها را به آسانی نصب کنید:

```bash
dotnet add package Icot.Shared.Kernel
dotnet add package Icot.Shared.Messaging
```

---

## 🚀 انتشار خودکار نسخه جدید (CI/CD)

این مخزن مجهز به پایپ‌لاین GitLab CI است. برای انتشار یک نسخه جدید:

1. ورژن را در فایل‌های `.csproj` پروژه‌ها به‌روزرسانی کنید.
2. کامیت کرده و یک `Tag` روی مخزن ثبت کنید (مثلاً `v1.0.1`):
   ```bash
   git tag v1.0.1
   git push origin v1.0.1
   ```
3. پایپ‌لاین به طور خودکار بسته‌های NuGet را ساخته و در Package Registry هم‌گیت منتشر می‌کند.
*(همچنین در برنچ `main` می‌توانید از پنل CI/CD هم‌گیت مرحله `publish_packages` را به صورت دستی Trigger کنید).*
