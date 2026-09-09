# 📦 nuget-utils (ICOT Shared Packages)

این مخزن شامل کتابخانه‌های هسته مشترک پلتفرم **ICOT** بر بستر **.NET 10.0** است که به صورت بسته‌های NuGet برای استفاده در تمامی میکروسرویس‌های پلتفرم به صورت عمومی در سایت رسمی **NuGet.org** منتشر می‌شوند.

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

از آنجایی که پکیج‌ها به صورت عمومی و استاندارد روی سایت رسمی `Nuget.org` قرار دارند، **هیچ نیازی به تنظیمات فایل `nuget.config` یا توکن گیت‌هاب نیست!** کافیست دستورات زیر را در پوشه پروژه خود اجرا کنید:

```bash
dotnet add package Icot.Shared.Kernel
dotnet add package Icot.Shared.Messaging
```

یا اگر می‌خواهید (مثل معماری فعلی پلتفرم) پروژه‌ها همیشه به صورت خودکار آخرین نسخه را دریافت کنند، از ورژن شناور در فایل `.csproj` استفاده کنید:

```xml
<PackageReference Include="Icot.Shared.Kernel" Version="1.0.*" />
<PackageReference Include="Icot.Shared.Messaging" Version="1.0.*" />
```

---

## 🚀 انتشار خودکار نسخه جدید (GitHub Actions)

این مخزن مجهز به اکشن‌های گیت‌هاب (GitHub Actions) است. با هر بار **پوش کردن کد روی برنچ `main`**، اتفاقات زیر به صورت خودکار رخ می‌دهد:
۱. بیلدهای جدید شماره‌گذاری می‌شوند (نسخه‌بندی داینامیک).
۲. پکیج‌های `.nupkg` ساخته می‌شوند.
۳. گیت‌هاب با روش فوق‌امنیتی **Trusted Publishing (OIDC)** و کاملاً بدون نیاز به پسورد یا Secretهای دستی، پکیج‌های جدید را مستقیماً روی سایت Nuget.org آپلود می‌کند!
