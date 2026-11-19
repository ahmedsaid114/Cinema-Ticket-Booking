# إعداد CineVerse محليًا

## إنشاء حسابَي الإدارة والجداول

1. تأكد أن خدمة SQL Server تعمل، وأن `ConnectionStrings:CinemaProject` في `appsettings.json` يشير إلى قاعدة البيانات التي تريدها.
2. افتح PowerShell داخل مجلد المشروع الذي يحتوي `TaskCinema.csproj`.
3. شغّل:

   ```powershell
   .\setup-local.ps1
   ```

4. اكتب بريدًا وكلمة مرور مختلفين لكل من SuperAdmin وAdmin. كلمة المرور لازم تكون 8 أحرف أو أكثر، وبها حرف كبير وحرف صغير ورقم. الإدخال مخفي.
5. شغّل المشروع باستخدام إعداد **https** في Visual Studio أو `dotnet run`. في Development سيطبّق التطبيق migrations عند بدء التشغيل، ثم ينشئ أدوار Customer وAdmin وSuperAdmin وحسابَي الإدارة.
6. افتح `/Admin/Account/Login`. حساب SuperAdmin يذهب إلى `/Admin/SuperAdmin`، وحساب Admin يذهب إلى `/Admin/Home`.

لو الجداول لا تظهر، تأكد أن SQL Server Management Studio متصل بنفس قاعدة البيانات المحددة في `ConnectionStrings:CinemaProject`. القيمة الحالية تستخدم `Data Source=.` واسم قاعدة `CinemaProject`.

## تشغيل الدفع في وضع Stripe التجريبي

كود Checkout وWebhook موجود. لتجربته تحتاج حساب Stripe مفعلًا في بلد مدعوم، ومفتاح API تجريبيًا. لا تضع المفاتيح في ملفات المشروع أو Git.

1. احصل على `sk_test_...` من Stripe Dashboard.
2. ثبّت Stripe CLI، ثم افتح نافذة طرفية وشغّل:

   ```powershell
   stripe listen --forward-to https://localhost:7209/payments/stripe/webhook
   ```

3. انسخ قيمة `whsec_...` التي يطبعها Stripe CLI.
4. في PowerShell داخل مجلد المشروع احفظ المفتاحين محليًا:

   ```powershell
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
   dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
   ```

5. سجّل الدخول كعميل، افتح فيلمًا سعره أكبر من صفر، وابدأ الدفع. استخدم بطاقة الاختبار التي توصي بها لوحة Stripe التجريبية؛ لا تستخدم بيانات بطاقة حقيقية أثناء الاختبار.

القيمة الافتراضية للعملة هي EGP. تأكد من أن بلد حساب Stripe نفسه يسمح بعرض السعر بهذه العملة قبل استخدام الدفع الحي. إنشاء حساب Stripe يتطلب أن يكون نشاطك في بلد تدعمه Stripe؛ راجع [قائمة Stripe الحالية للدول المدعومة](https://stripe.com/global).

## قبل النشر الحي

- انشر على استضافة تدعم ASP.NET Core و.NET 10، مع نطاق HTTPS وقاعدة SQL Server يمكن الوصول إليها من التطبيق.
- اضبط `ConnectionStrings__CinemaProject` و`SuperAdmin__Email` و`SuperAdmin__Password` و`Admin__Email` و`Admin__Password` في مخزن أسرار الاستضافة.
- اضبط `Stripe__SecretKey` بمفتاح Live، و`Stripe__WebhookSecret` بسر Webhook المسجل على عنوان HTTPS العام `/payments/stripe/webhook`.
- اضبط مفاتيح `Email__Smtp__...` حتى تعمل رسائل استعادة كلمة المرور.
- قبل تشغيل النسخة الإنتاجية طبّق migrations على قاعدة الإنتاج من عملية النشر باستخدام `dotnet ef database update`، أو أنشئ SQL script وطبّقه في قاعدة الإنتاج. التطبيق لا يغيّر schema الإنتاج تلقائيًا.
- بعد النشر اختبر حجزًا تجريبيًا في Stripe Test Mode أولًا. فعّل Live Mode فقط بعد اكتمال التحقق التجاري والبنكي لحساب Stripe.
