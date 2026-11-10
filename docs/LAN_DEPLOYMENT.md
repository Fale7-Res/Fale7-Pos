# تشغيل فالح على أكثر من جهاز في الشبكة المحلية

قرار المالك (2026-10-24): الكاشير والكول سنتر ووكلاء الطباعة قد يعملون على أجهزة مختلفة في نفس الشبكة.
الإعداد الافتراضي لا يسمح بذلك: السيرفر يستمع على `http://localhost:5151` فقط، أي من نفس الجهاز.
هذا الملف يشرح ما يتغير، بالترتيب. لا يحتاج أي تعديل في الكود: كل ما يلي إعدادات.

> **تحذير:** بدون HTTPS تمر أرقام الـ PIN ورموز الجلسات على الشبكة كنص مقروء. نفّذ الخطوة ٤ قبل
> تشغيل أي جهاز آخر غير السيرفر، أو اقصر الشبكة على أجهزة المطعم وحدها حتى تنفذها.

---

## ١. السيرفر يستمع على عنوان الشبكة

في `appsettings.json` الخاص بالسيرفر المنشور (`E:\falah-restaurant\FalahServer\appsettings.json`):

```json
"Urls": "http://0.0.0.0:5151"
```

`0.0.0.0` تعني كل كروت الشبكة في الجهاز. الأدق استخدام عنوان السيرفر الثابت نفسه، مثل
`http://192.168.1.10:5151`. **يجب أن يكون عنوان السيرفر ثابتًا** (حجز DHCP في الراوتر أو عنوان يدوي)،
وإلا تغيّر وفقدته كل الأجهزة.

أعد تشغيل الخدمة:

```
Restart-Service FalahServer
```

## ٢. جدار الحماية: الشبكة الخاصة فقط

PowerShell بصلاحية المسؤول على جهاز السيرفر:

```
New-NetFirewallRule -DisplayName "Falah Server" -Direction Inbound -Protocol TCP -LocalPort 5151 `
    -Profile Private -RemoteAddress LocalSubnet -Action Allow
```

`-Profile Private` و`-RemoteAddress LocalSubnet` يمنعان الوصول من خارج شبكة المطعم. تأكد أن شبكة
المطعم مصنفة «خاصة» في إعدادات ويندوز، لا «عامة».

## ٣. كل جهاز يعرف عنوان السيرفر

| الجهاز | الإعداد |
|---|---|
| الكاشير (WinUI) | متغير البيئة `FALAH_SERVER_URL` = `http://192.168.1.10:5151` |
| الكول سنتر (WinUI) | نفس المتغير `FALAH_SERVER_URL` |
| وكيل الطباعة | `ServerBaseAddress` في `appsettings.json` الخاص بالوكيل |

ضبط متغير البيئة للجهاز كله (PowerShell كمسؤول)، ثم أعد تشغيل البرنامج:

```
[Environment]::SetEnvironmentVariable("FALAH_SERVER_URL", "http://192.168.1.10:5151", "Machine")
```

وكيل الطباعة: ضع رقم الـ PIN **لحساب له صلاحية `print_agent` فقط** في متغير البيئة
`FALAH_PRINT_AGENT_PIN` على الجهاز نفسه أو في `appsettings.local.json` بجانب الوكيل، ولا تضعه في
`appsettings.json` (راجع `DEPLOYMENT.md` §٥ و`PRINT_AGENT.md`). إن خدم أكثر من جهاز نفس المسار (مثلاً طابعتان للشواية)،
فكل وكيل يحجز التذاكر التي سيطبعها ولا تُطبع تذكرة مرتين. معرّف الوكيل الافتراضي هو اسم الجهاز
والمسار، فلا يلزم تغييره إلا إذا تكرر اسم الجهاز.

تحقق من كل جهاز:

```
Invoke-WebRequest http://192.168.1.10:5151/health
```

## ٤. HTTPS

يعرّف Kestrel نقطة HTTPS من الإعدادات وحدها. استخدم شهادة من مخزن شهادات ويندوز بدل ملف بكلمة
مرور داخل الإعدادات:

1. أصدر شهادة للسيرفر باسمه (أو عنوانه) في مخزن `LocalMachine\My`، وثبّت الجزء العام منها في
   «Trusted Root Certification Authorities» على **كل** جهاز يتصل بالسيرفر. بدون هذه الخطوة يرفض
   البرنامج الاتصال، وهذا هو السلوك الصحيح.
2. في `appsettings.json` للسيرفر، بدل `Urls`:

```json
"Kestrel": {
  "Endpoints": {
    "Https": {
      "Url": "https://0.0.0.0:5443",
      "Certificate": { "Subject": "falah-server", "Store": "My", "Location": "LocalMachine", "AllowInvalid": false }
    }
  }
}
```

3. حساب خدمة FalahServer يحتاج صلاحية قراءة المفتاح الخاص للشهادة.
4. افتح المنفذ 5443 في جدار الحماية بنفس قاعدة الخطوة ٢، واقفل 5151.
5. غيّر `FALAH_SERVER_URL` و`ServerBaseAddress` إلى `https://<اسم-السيرفر>:5443`. الاسم يجب أن يطابق
   الشهادة.
6. سكربت التثبيت يتحقق من `http://localhost:5151/health` افتراضيًا. مرّر له العنوان الجديد عبر
   `-HealthUrl`.

## ٥. قيود يجب معرفتها

- **قاعدة البيانات:** تبقى على جهاز السيرفر. لا يتصل بها أي جهاز آخر مباشرة، ولا يجب أن يتصل.
- **ترتيب الحروف (collation):** بعض الاستعلامات تعتمد على أن SQL Server لا يفرق بين الحروف الكبيرة
  والصغيرة (الإعداد الافتراضي للتثبيت). لا تُنشئ قاعدة البيانات بترتيب حساس لحالة الحروف (`_CS_`).
- **انقطاع الشبكة:** الكاشير يحفظ أي عملية لم يصل ردها على قرصه ويعيد إرسالها بنفس المفتاح عند
  إعادة المحاولة، فلا تتكرر. لكنه لا يبيع أثناء انقطاع الاتصال بالسيرفر.
- **السكون (sleep):** لا تسمح لجهاز السيرفر بالدخول في وضع السكون أثناء ساعات العمل: كل الأجهزة
  تعتمد عليه.
- **حماية رقم الدخول تعتمد على عنوان كل جهاز:** السيرفر يعدّ محاولات الرقم الخاطئ لكل جهاز على حدة
  (عنوان IPv4، أو شبكة IPv6 كاملة، وجهاز السيرفر نفسه جهاز واحد). جهاز تكررت عليه الأخطاء يتوقف عنه
  الدخول مؤقتًا **وحده** وتبقى باقي الأجهزة شغالة؛ ويقدر المسؤول يرجّعه من «حالة أمان الدخول». لذلك:
  - **لا تضع proxy أو موزّع أحمال أمام السيرفر**: كل الأجهزة هتبان كأنها جهاز واحد، وجهاز واحد بيغلط هيوقف الكل.
  - أعطِ كل جهاز عنوانًا ثابتًا (حجز DHCP) - الجهاز اللي بيتعرف عليه السيرفر هو اللي اتسجل منه دخول ناجح قبل كده.
  - لو كل البرامج على جهاز السيرفر نفسه، فهي كلها «جهاز واحد» في هذا العدّ.
- **طلبات المتصفح (CORS) مرفوضة افتراضيًا:** البرامج نفسها لا تتأثر. لا تضف أي عنوان في
  `Cors:AllowedOrigins` إلا لو ظهر برنامج يعمل من المتصفح فعلًا.


## Enforced transport guard (2026-11-07)

Remote stations now refuse non-HTTPS server addresses before opening the HTTP transport. Loopback HTTP remains available for local development. Certificate validation stays enabled; no certificate-trust bypass was added. Configure the installed server certificate and set FALAH_SERVER_URL to its HTTPS name on both stations; use that same HTTPS ServerBaseAddress in agent settings.

The server rejects remote HTTP bindings (Urls, Kestrel endpoints, HTTP_PORTS) before database startup, and rejects a remote HTTP request even if it supplies X-Forwarded-Proto: https. The narrow server recovery override is Security__Transport__AllowInsecureRemoteHttp=true and the agent override is AllowInsecureRemoteHttp=true; both log warnings and should be removed after TLS setup. Station clients provide no insecure remote override. A migration-script-only invocation starts no listener and bypasses binding validation.

On-site acceptance remains required: trust the intended certificate on each station, verify login/checkout/agent polling with HTTPS, verify a wrong/untrusted certificate is rejected, and remove any obsolete remote HTTP listener. No certificate, service or production database was changed by the remediation session.
