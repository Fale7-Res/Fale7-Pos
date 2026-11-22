# مراجعة مشاكل `All Issues/` مقابل الكود الحالي — 2026-11-22

## الطريقة

- **المصدر:** كل صف في [01_MASTER_ISSUE_REGISTRY.md](01_MASTER_ISSUE_REGISTRY.md)، وعددها 245 مشكلة. اتقرت مع ملف كل مشكلة في `ISSUES/`.
- **الكود اللي اتراجع:** شجرة العمل على الفرع `closeout-20261117`، وآخر commit فيها `d386164`، بالإضافة لتعديلات Setup اللي لسه ما اتعملهاش commit.
- **حدود الملف ده:** **ما اتصلحش ولا اتغيّر أي كود.** الملف ده هو الحاجة الوحيدة اللي اتكتبت.
- **إزاي اتحددت حالة كل مشكلة:**
  - **المشاكل المفتوحة (75):** كل واحدة اتفحصت في الكود نفسه، بالملف ورقم السطر، وما اتاخدش بكلام السجل.
  - **المشاكل المصلّحة (159):** اتأكدنا إن التست اللي بيثبت الإصلاح موجود فعلًا في الكود. واللي ملهاش تست مذكور، اتفحص الكود نفسه عند السطر اللي كانت فيه المشكلة. القسم 5 بيوضح طريقة التأكد من كل واحدة.
  - **"مش مشكلة أصلًا" (11):** ما اتراجعتش تاني.
- **اللي الملف ده ما يثبتهوش:**
  - **التستات ما اتشغّلتش في المراجعة دي.** آخر تشغيل كامل كان 2026-11-18: ‏1051 من 1053 نجحوا.
  - **سلوك الأجهزة** (الطابعات، ودرج الفلوس، وHTTPS على أجهزة المطعم) ما يتثبتش من الكود.

## الملخص

| الحالة بعد الفحص | العدد |
|---|---|
| موجودة في الكود ومتصلحتش (القسم 1) | 27 |
| اتصلح جزء منها، وجزء لسه في الكود (القسم 2) | 18 |
| مقبولة كما هي أو تصميم معماري، مش عطل بيأثر على الحسابات (القسم 3)، ومنها OPS-0001 وOPS-0003 اللي في السجل "ما اتأكدتش" | 12 |
| **الكود اتصلح**، والباقي تشغيل في المطعم، والسجل لسه بيقول إنها مفتوحة (القسم 4) | 7 |
| مخاطر ما اتأكدتش، ومعاها اللي الكود بيقوله النهارده (القسم 6) | 11 |
| مصلّحة ومتأكد منها (القسم 5) | 159 |
| مش مشكلة أصلًا (القسم 7) | 11 |

**أهم المشاكل اللي لسه موجودة في الكود:**

| المشكلة | المكان في الكود |
|---|---|
| PAYR-0004: الموظف ممكن يتصرفله مرتين، مرة من "دفع مستحقات" ومرة من "مسير الرواتب" | [CreatePayrollHandler.cs:62](../src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs#L62) و[PayPayrollHandler.cs:151](../src/Falah.Infrastructure/Employees/PayPayrollHandler.cs#L151) |
| SEC-0037: تسجيل الحضور والانصراف من غير أي صلاحية | [Program.cs:1001](../src/Falah.Server/Program.cs#L1001) |
| FIN-0020: عدّ فلوس اليوم بيتقارن برقم "متوقع" بيكتبه المستخدم بإيده | [SaveCashCountHandler.cs:41](../src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs#L41) |
| FIN-0051: طلب توصيل الدفع عند الاستلام، اللي لسه ما خرجش، ما ينفعش يتلغى | مفيش أي endpoint بيعمل كده |
| FIN-0005: الطلب المدفوع بأكتر من طريقة ما ينفعش يترجّع | [Order.cs:256](../src/Falah.Domain/Orders/Order.cs#L256) |

ومعظم الباقي قرارات للمالك أو للمحاسب، والقرارات دي متسجلة في [docs/GO_LIVE_ACCEPTANCE_20261114.md](../docs/GO_LIVE_ACCEPTANCE_20261114.md) §4.

---

## 1. موجودة في الكود ومتصلحتش

| ID | المشكلة | الدليل من الكود | القرار عند |
|---|---|---|---|
| PAYR-0004 | "دفع مستحقات" و"مسير الرواتب" ممكن الاتنين يصرفوا نفس المستحقات | المسير بيتحسب و"السلف" بصفر: `Money.Zero, Money.Zero` في [CreatePayrollHandler.cs:62](../src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs#L62). وبيصرف `NetPayablePiastres` المحسوب وقت إنشاء المسير من غير ما يراجع رصيد الموظف الحالي [PayPayrollHandler.cs:151-159](../src/Falah.Infrastructure/Employees/PayPayrollHandler.cs#L151-L159). لحد ما المالك يقرر، القاعدة المؤقتة: طريقة صرف واحدة للموظف في الشهر (قبول التشغيل §4) | المالك |
| SEC-0037 | تسجيل الحضور والانصراف من غير صلاحية | الـ endpoints [Program.cs:1001-1012](../src/Falah.Server/Program.cs#L1001-L1012) ما بتعملش أي فحص صلاحية، والـ handlers `BiometricCheckInHandler` و`BiometricCheckOutHandler` في `AttendanceHandlers.cs` مفيهاش `AuthorizeCommand`. معرّف الحدث (`BiometricEventId`) اختياري | المالك |
| UI-0025 | الاستلام على مرحلتين (يتجهز الأول ويتدفع وقت الاستلام) مش مدعوم | مفيش حالة "منتظر الدفع": `enum OrderStatus { Pending, Preparing, Ready, Completed, Cancelled, Refunded }` في [OrderEnums.cs:11](../src/Falah.Domain/Orders/OrderEnums.cs#L11). والشيك أوت بيرفض أي مبلغ أقل من الإجمالي، إلا في توصيل الدفع عند الاستلام [CheckoutRules.cs:114-115](../src/Falah.Domain/Orders/CheckoutRules.cs#L114-L115) | المالك (متطلب جديد) |
| FIN-0005 | الطلب المدفوع بأكتر من طريقة (مقسّم) ما ينفعش يترجّع | `throw new DomainException("مرتجع الدفع المتعدد غير مدعوم...")` في [Order.cs:256](../src/Falah.Domain/Orders/Order.cs#L256) | المالك |
| FIN-0020 | عدّ فلوس اليوم بيتقارن بـ"متوقع" مكتوب باليد | السيرفر بياخد `request.ExpectedPiastres` [SaveCashCountHandler.cs:41-42](../src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs#L41-L42)، والشاشة بتقراه من خانة بيكتب فيها المستخدم `ExpectedText` [CashCountViewModel.cs:151](../src/Falah.Client.CashierWinUI/ViewModels/CashCountViewModel.cs#L151) | تطوير، بعد موافقة المالك |
| FIN-0030 | الضريبة بتتحسب قبل خصم الضيافة | `taxableBase = subtotal - discount` وبعدها `ComputePayableTotal(cartTotal, deliveryFee, compAmount)` في [CheckoutRules.cs:105-109](../src/Falah.Domain/Orders/CheckoutRules.cs#L105-L109) | المحاسب |
| FIN-0051 | طلب الدفع عند الاستلام اللي لسه ما خرجش ما ينفعش يتلغى | الانتقال `New → Cancelled` مسموح في [DeliveryStatus.cs:15](../src/Falah.Domain/Delivery/DeliveryStatus.cs#L15)، بس مفيش endpoint بيستخدمه. الموجود بس: `/api/orders/{checkout, refund, confirm-electronic-payment, cancel-pending-electronic-payment}` و`/api/delivery/{id}/{ready, assign, reassign, result}` | المالك |
| PAYR-0005 | تصحيح الحضور بيعيد حساب الجزاء القديم بأجر وإعدادات النهارده | `employee.DailyWagePiastres` والإعدادات الحالية بيتطبقوا على الجزاء القديم والجديد الاتنين [CorrectAttendanceHandler.cs:93-99](../src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs#L93-L99) | المالك والمحاسب |
| PAYR-0006 | التعويض عن أيام قديمة في الراتب الشهري بيستخدم الراتب الحالي | `Money.FromPiastres(employee.MonthlySalaryPiastres!.Value)` لكل يوم بيتعوّض [SalaryAccrual.cs:60](../src/Falah.Infrastructure/Employees/SalaryAccrual.cs#L60) | المالك والمحاسب |
| PAYR-0007 | الموظف اللي اشتغل جزء من الشهر بياخد الراتب كامل في المسير | `emp.MonthlySalary ?? Money.Zero` كأساس [PayrollRules.cs:117-118](../src/Falah.Domain/Payroll/PayrollRules.cs#L117-L118) | المالك والمحاسب |
| PAYR-0021 | تكلفة الأجور في لوحة المالك مش شاملة تعديلات تصحيح الحضور | قائمة الأنواع فيها `Deduction` ومفيهاش `Adjustment` [OwnerDashboardQueryService.cs:253-256](../src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L253-L256) | المحاسب |
| PERF-0003 | قفل اليوم بيحمّل كل الورديات والعهد وقيود الدفتر | `db.Shifts...ToListAsync` و`db.DriverCustodies...ToListAsync` و`db.FinancialJournalEntries...ToListAsync`، من غير فلتر باليوم [BusinessDayCloseEvaluator.cs:28-30](../src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs#L28-L30) | تطوير |
| PERF-0004 | قائمة الموردين بتحمّل دفتر الموردين كله | `Suppliers...ToListAsync` و`SupplierLedgerEntries...ToListAsync` [PurchasingQueryService.cs:18-19](../src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs#L18-L19) | تطوير |
| PERF-0006 | شاشة الموظفين بتبعت طلب رصيد لكل موظف، ولو طلب واحد فشل بتوقف | `GetEmployeeBalanceAsync` جوه `foreach`، و`return` عند أول فشل [EmployeesViewModel.cs:102-105](../src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs#L102-L105) | تطوير |
| PRT-0005 | إرسال الورقة لطابور الويندوز متزامن، وما بيقفش لو اتطلب منه يقف | `Send(...)` متزامن، ومن غير `ct`، في [WindowsSpoolerTicketTransport.cs:50](../src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs#L50). مراقبة الورقة بعد كده بس هي اللي بتحترم طلب الإيقاف (السطر 51) | تطوير |
| PRT-0013 | أمر الطباعة اللي فشل عمره ما بيتعاد لوحده | الحجز بيمسك أوامر `Status = Pending` بس [PrintJobQueryService.cs:57](../src/Falah.Infrastructure/Printing/PrintJobQueryService.cs#L57). الفاشل محتاج إعادة طباعة يدوي، وده قرار المالك (2026-11-18) | المالك |
| PRT-0016 | تذكرة المطبخ المتعلّقة ما بتطلعش لوحدها لما التحويل يتأكد | التذكرة بتتعلّق والتعليق بيقول "تتبعت من البحث عن الطلب" [OrderAutoPrintService.cs:62-70](../src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs#L62-L70). لا الـ endpoint [Program.cs:1679-1693](../src/Falah.Server/Program.cs#L1679-L1693) ولا شاشة الكاشير [CashierViewModel.Checkout.cs:160-184](../src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs#L160-L184) بيبعتوا تذكرة | المالك |
| OPS-0006 | مفيش مراقبة لمساحة الهارد | مفيش `DriveInfo` ولا `AvailableFreeSpace` في `src` | تطوير |
| INV-0011 | تكلفة الأكل في التقارير القديمة بتتحسب بوصفات وأسعار النهارده | بيستخدم `RecipeId` الحالي للمنتج والحجم [ReportingQueryService.cs:378-404](../src/Falah.Infrastructure/Reports/ReportingQueryService.cs#L378-L404). الشاشة بتقول ده صراحة | المالك (مؤجّل) |
| FIN-0044 | قفل اليوم مش بيحفظ أرباح وخساير اليوم | مفيش أي حقل ربح أو خسارة في `CloseBusinessDayHandler` ولا في DTOs يوم العمل | المالك (مؤجّل) |
| CUS-0002 | مفيش طريقة لمسح بيانات عميل أو إخفائها | الـ `MapDelete` الوحيد هو صورة المنتج [Program.cs:1815](../src/Falah.Server/Program.cs#L1815). مفيش endpoint لحذف عميل | المالك |
| DB-0009 | حالتين في دفتر العمليات المالية متعرّفين ومش مستخدمين | `JournalEntryStatus { Pending, Complete, Failed, RecoveryRequired }` [SupportingEntities.cs:140](../src/Falah.Infrastructure/Persistence/Entities/SupportingEntities.cs#L140)، ومفيش أي مكان بيحط `Failed` ولا `RecoveryRequired` | تطوير |
| PAYR-0019 | معاملات الأجر الإضافي والجزاء أرقام عشرية، والتقريب بنظام البنوك | `(long)Math.Round(rate.Piastres * hours * latePenaltyMultiplier)` [PayrollRules.cs:54](../src/Falah.Domain/Payroll/PayrollRules.cs#L54) و[:64](../src/Falah.Domain/Payroll/PayrollRules.cs#L64)، بالإعداد الافتراضي ToEven | المحاسب |
| PERF-0002 | سجل الرقابة بيتبحث بنوع الحدث، والفهرس على الوقت بس | `HasIndex(e => e.CreatedAtUtc)` بس [FalahDbContext.cs:441](../src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L441) | تطوير |
| PRT-0008 | عرض الورق وعدد نسخ الإيصال اللي في الإعدادات مش بيأثروا على الطباعة | `PrinterWidth` و`NumberOfReceiptCopies` بيتحفظوا ويتقروا بس (`UpdateSettingsHandler` و`SettingsQueryService` وشاشة الإعدادات). مفيش أي استخدام ليهم في `src/Falah.PrintAgent` ولا في `Infrastructure/Printing` | تطوير |
| SEC-0007 | الجلسة بتنتهي بعد 16 ساعة خمول بس، ومن غير حد أقصى مطلق | `IdleLifetime = TimeSpan.FromHours(16)` بس [SessionToken.cs:35-38](../src/Falah.Domain/Authorization/SessionToken.cs#L35-L38) | تطوير |
| UI-0026 | الصلاحيات في البرنامج بتفضل زي ما كانت لحد ما المستخدم يدخل تاني | بيقرا `CurrentUser.Permissions` من وقت الدخول [AppSession.cs:39](../src/Falah.Client.CashierWinUI/Services/AppSession.cs#L39). السيرفر نفسه بيراجع الصلاحية مع كل طلب | تطوير |

## 2. اتصلح جزء منها، وجزء لسه في الكود

| ID | اللي اتصلح (من الكود) | اللي لسه موجود (من الكود) |
|---|---|---|
| FIN-0042 | لوحة المالك بتعدّ طلبات التوصيل المدفوعة اللي فشلت ولسه ما اترجّعتش، وبتعرض المبلغ [OwnerDashboardQueryService.cs:410-411](../src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L410-L411) | مفيش أي تنبيه وقت تسجيل نتيجة التوصيل، ولا شرط قبل قفل اليوم. المرتجع لسه يدوي |
| PERF-0001 | قراءة الرصيد بقت بقفل التطبيق، فشغل الكاشير مش بيستنى (`TreasuryMapper.cs:35-38`) | الـ SUM لسه بيلف على تاريخ الحساب كله. أعمدة الحساب `nvarchar(max)`، فما ينفعش تتعمل عليها فهرس من غير migration مش إضافية، ودي متسجلة سؤال للمالك |
| API-0004 | أقفال البيع بتستنى 5 ثواني بدل 15 | مهلة الكاشير لسه 10 ثواني [ApiClient.cs:35](../src/Falah.Client.CashierWinUI/Services/ApiClient.cs#L35)، والكول سنتر نفس الحكاية [ApiClient.cs:30](../src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs#L30)، والتصدير بس 3 دقايق. وفيه قراءات تانية لسه بتلف على التاريخ كله |
| OPS-0002 | فيه إعداد اختياري للنسخة التانية `Backup:CopyToDirectory` [CreateBackupHandler.cs:116-123](../src/Falah.Infrastructure/Backup/CreateBackupHandler.cs#L116-L123)، وSetup بيسأل عليه (`-BackupCopyDir`) | مقفول افتراضيًا. **والنسخ اللي بتتبعت للمكان التاني عمرها ما بتتمسح:** الحذف في `ScheduledBackupService.PruneAsync` بيمسح الملف الأساسي بس |
| RPT-0001 | الفترة المعكوسة بقت بتتعدل في كل التقارير | لو مفيش فترة، التقرير بيجيب التاريخ كله (حسب سجل المشكلة، والقرار للمالك) |
| PAYR-0017 | الراتب الشهري بيتحسب يوم بيوم (`PayrollRules.DailySalaryAccrual`، [PayrollRules.cs:97-103](../src/Falah.Domain/Payroll/PayrollRules.cs#L97-L103)). واختيار فترة المسير بقى بأيام القاهرة (PAYR-0008 اتصلح) | التعويض عن الأيام القديمة بالراتب الحالي (PAYR-0006)، والراتب الكامل لجزء من الشهر (PAYR-0007)، والاتنين في القسم 1 |
| PAYR-0015 | المقارنة بقت بـ `nameof(...)` ومن غير حساسية لحالة الحروف [PayrollRules.cs:74-76](../src/Falah.Domain/Payroll/PayrollRules.cs#L74-L76) | `OutstandingAdvance` لسه مش مستخدمة في أي مكان في `src`، بس في التستات |
| SEC-0011 | اختيار الرمز عليه حد للمحاولات وبيتسجّل (`PinChoiceGuard`) | الفحص بيعمل PBKDF2 مع كل مستخدم نشط [PinUniquenessChecker.cs:26-28](../src/Falah.Infrastructure/Users/PinUniquenessChecker.cs#L26-L28)، و210000 تكرار لكل واحد [PinHasher.cs:21](../src/Falah.Domain/Authorization/PinHasher.cs#L21) |
| DB-0003 | `CreateUserHandler` بقى بيقارن بعد `Trim().ToLower()` (السطر 45) | الاعتماد على إن قاعدة البيانات مش حساسة لحالة الحروف ما اتراجعش في كل الاستعلامات |
| DB-0005 | حاجز صيانة: كل طلب بياخد قفل مشترك (`Program.cs:399`)، والنسخ التلقائي برضه (`ScheduledBackupService.cs:90`)، والاسترجاع بياخد قفل حصري وبعده SINGLE_USER ([DatabaseActivityScope.cs](../src/Falah.Infrastructure/Persistence/DatabaseActivityScope.cs) و`RestoreBackupHandler.cs:45`) | مش متجرب، حسب سجل المشكلة: سيرفرين على قاعدة واحدة، والكتابة وقت التشغيل والـ migration، واسترجاع تحت ضغط |
| PRT-0004 | طابور الويندوز بقى بيتراقب بعد الإرسال (`SpoolerJobWatch`، و9 تستات) | طرق الشبكة والـ serial مفيهاش أي استعلام عن حالة الطابعة ([Transports.cs](../src/Falah.PrintAgent/Transports.cs)). ورسايل "الورق خلص" و"الغطا مفتوح" محتاجة تجربة على XP-80C في المطعم |
| HW-0002 | فتح درج الفلوس موجود (`CashDrawerKicker`) | مقفول افتراضيًا (`CashDrawerKickEnabled`، [AgentOptions.cs:98](../src/Falah.PrintAgent/AgentOptions.cs#L98)) وعمره ما اتجرب على درج حقيقي |
| UI-0021 | لون النص المعطّل في الوضع الغامق اتغير لـ `#5B6472` (Colors.xaml:249) | الوضع الفاتح لسه `#94A3B8` (Colors.xaml:105)، وتباينه 2.34:1 حسب سجل المشكلة |
| UI-0027 | شاشة الرقابة المالية موجودة (`FinancialControlView.xaml`) | مفيش سكربت اختبار واجهة (winapp) في المشروع |
| RPT-0003 | الشاشات موجودة (`FinancialControlView`) ومعاها تستات الـ view model | إن الشاشة اتفتحت على الجهاز ده حاجة ما تتثبتش من الكود |
| OPS-0005 | فيه تستات سيناريو: `FullTradingDayScenarioTests` و`OperatingDayScenarioTests` و`StatementsAcrossMidnightTests` و`BusinessDayMembershipTests` و`SalaryAccrualTests`/`SalaryAccrualDatingTests`، ومن ناحية البرامج `CashierDurabilityTests` و`CallCenterDurabilityTests` وغيرهم | سيناريو إعادة تشغيل السيرفر في نص يوم كامل مش موجود كتست واحد. وكلام السجل ("B–F مش موجودين") قديم |
| TEST-0002 | تستات تأكيد الدفع المقسّم ([ConfirmElectronicPaymentTests.cs:228,248,261](../tests/Falah.Server.IntegrationTests/ConfirmElectronicPaymentTests.cs#L228))، وتست التوقيت الصيفي (`BusinessClockTests.cs:117`) | تستات المدخلات السالبة وحدود الشهر في المسير ما اتراجعتش في الفحص ده |
| DOC-0001 | ملف إعدادات برنامج الطباعة مفيهوش رمز ([appsettings.json:4](../src/Falah.PrintAgent/appsettings.json#L4)) | [GO_LIVE_CLEANUP_CHECKLIST.md:164](../docs/GO_LIVE_CLEANUP_CHECKLIST.md) لسه بيقول إن الملف فيه الرمز 4321. و[DAILY_OPERATIONS_VERIFICATION.md:15](../docs/DAILY_OPERATIONS_VERIFICATION.md) بيقول إن المستخدمين التجريبيين بيتعملوا في أي قاعدة فاضية، والكود بيعملهم في Development أو مع `Seed:DemoUsers` بس |

## 3. مقبولة كما هي أو تصميم معماري (مش عطل بيأثر على الحسابات)

| ID | الحالة في الكود النهارده |
|---|---|
| ARCH-0001 | `Program.cs` فيه 2869 سطر و214 `app.Map*`، والصلاحيات متكتبة بأكتر من طريقة |
| ARCH-0002 | فيه برنامجين WinUI لكل واحد ApiClient لوحده، والبرنامج القديم WPF لسه في `Falah.sln:22` |
| ARCH-0003 | ترتيب الأقفال وفحوصات المبالغ متكتبة يدوي في كل handler. ما اتراجعتش handler بـ handler |
| ARCH-0004 | معرّفات الخزن (`"tre-drawer"` و`"tre-main"`) مكتوبة ثابتة في 10 ملفات في البرنامجين |
| API-0001 | `catch (InvalidOperationException` موجودة 106 مرة في `Program.cs`، منهم 63 بيرجّعوا `NotFound` |
| API-0002 | `string actorUserId` في 46 endpoint. الـ middleware بيراجعه على الجلسة |
| UI-0011 | البرنامج القديم WPF: `long.TryParse(AmountPaidText...)` في `Falah.Client.Cashier/.../CashierViewModel.Checkout.cs:33`، مقابل `amountEgp * 100m` في السطر 94. البرنامج ده مش بيتصان |
| UI-0024 | البرنامج القديم WPF: `RefundRequestId: $"rreq-{Guid.NewGuid():N}"` مع كل محاولة (`CashierViewModel.OrderLookup.cs:82`). مش بيتصان |
| DOC-0002 | حالة المشاكل في السجل متأخرة عن الكود. الفحص ده لقى 7 مشاكل الكود بتاعها اتصلح وهي لسه مفتوحة (القسم 4)، و`REMEDIATION_PLAN.md` اللي المشكلة بتشاور عليه اتمسح يوم 2026-11-19 |
| DOC-0004 | دليل الأداة القديمة مرجع تاريخي، ومش كود |
| OPS-0001 | .NET 8 (الـ runtime ‏8.0.30، وEF Core ‏8.0.11). الدعم الرسمي انتهى 2026-11-10، وده خطر متقبّل (قبول التشغيل §6). ما اتعملش فحص ثغرات للمكتبات |
| OPS-0003 | جهاز السيرفر المخطط النهارده HP Z440 (desktop). إن الجهاز ما ينامش وإن الـ write cache آمن حاجة بتتظبط في المطعم |

## 4. الكود اتصلح، والباقي تشغيل في المطعم (السجل لسه بيقول مفتوحة)

| ID | الدليل إن الكود اتصلح | الباقي |
|---|---|---|
| DB-0001 | كل الـ migrations في git (84 ملف). والإنتاج بيرفض يشتغل لو فيه migration ما اتطبقش [Program.cs:264-267](../src/Falah.Server/Program.cs#L264-L267)، و`MigrateOnStartup: false` في `appsettings.Production.json` | تطبيق السكربت على قاعدة `Falah` الحقيقية، بعد نسخة احتياطية اتأكدنا منها |
| PRT-0012 | `AgentLogin` بيعيد محاولة الدخول، وبيستنى أطول لو الرمز اترفض. والخدمة بتتسجل بإعادة تشغيل تلقائية `sc.exe failure ... restart` ([install-falah-print-agent-service.ps1:78-81](../tools/install-falah-print-agent-service.ps1#L78-L81)) | تجربة قطع الكهربا في المطعم |
| SEC-0006 | البرامج بترفض أي عنوان مش HTTPS لجهاز تاني (`ServerEndpointPolicy.RequireSecureAddress`). والسيرفر بيرفض الـ HTTP من جهاز تاني ([ServerTransportGuard.cs:27](../src/Falah.Server/ServerTransportGuard.cs#L27)). الإعداد الافتراضي `http://localhost:5151` على نفس الجهاز بس | تركيب الشهادة على الأجهزة، وSetup بيعمل ده |
| SEC-0022 | المستخدمين التجريبيين بيتعملوا في Development أو مع `Seed:DemoUsers` بس ([Program.cs:279](../src/Falah.Server/Program.cs#L279)) | أي قاعدة اتعمل فيها مستخدمين تجريبيين قبل كده لازم رموزهم تتغير |
| SEC-0023 | `/pending` محتاج صلاحية `print_agent` أو `settings_manage` ([Program.cs:2545-2546](../src/Falah.Server/Program.cs#L2545-L2546)). و`/claim` و`/start` و`/ticket` و`report-result` محتاجين `RequirePrintAgent`. والنتيجة بتتقبل من صاحب الحجز بس ([ReportPrintJobResultHandler.cs:46](../src/Falah.Infrastructure/Printing/ReportPrintJobResultHandler.cs#L46)) | عمل حساب `print_agent`. **ملاحظة:** `/api/print-jobs/overview` و`/api/print-jobs/{id}` مفتوحين لأي حد عامل دخول ([Program.cs:2542](../src/Falah.Server/Program.cs#L2542) و[:2568](../src/Falah.Server/Program.cs#L2568)) |
| SEC-0024 | مفيش رمز في ملف إعدادات برنامج الطباعة. الرموز التجريبية في `DemoDataSeeder.cs:24,44-45`، وده متعمّد عشان بيئة التطوير | تغيير الرموز التجريبية في أي قاعدة فيها مستخدمين تجريبيين |
| DOC-0003 | `CLAUDE.md` و`.claude/` و`docs/*` و`All Issues/` كلهم في git دلوقتي | ما يتعملش commit لتعديلات Setup إلا لما المالك يطلب |

## 5. المشاكل المصلّحة (159)، وطريقة التأكد من كل واحدة

**ما لقيناش ولا مشكلة مكتوب إنها مصلّحة ورجعت تاني في الكود.**

**أ. التست المذكور في ملف المشكلة موجود في الكود (66):**
FIN-0008 FIN-0032 FIN-0034 FIN-0036 SEC-0015 SEC-0016 SEC-0017 SEC-0019 SEC-0020 SEC-0021 ARCH-0005 FIN-0009 FIN-0033 FIN-0035 FIN-0037 FIN-0038 FIN-0039 FIN-0041 INV-0006 PAYR-0018 PRT-0006 SEC-0026 FIN-0040 SEC-0025 UI-0006 FIN-0001 FIN-0002 FIN-0010 FIN-0011 FIN-0012 FIN-0013 FIN-0028 SEC-0035 TEST-0001 UI-0001 UI-0012 UI-0028 UI-0032 DB-0006 DB-0011 FIN-0006 FIN-0016 INV-0009 MENU-0001 PAYR-0010 PAYR-0013 PRT-0001 PRT-0003 UI-0031 UI-0033 UI-0036 UI-0037 UI-0040 UI-0041 UI-0043 UI-0044 FIN-0023 INV-0003 PRT-0011 PRT-0017 SEC-0032 UI-0039 UI-0042 UI-0045 UI-0046 UI-0047

**ب. التستات المذكورة في سجل الدفعة (`REMEDIATION/BATCHES/`) موجودة في الكود (23):**
FIN-0007 PAYR-0001 SEC-0001 SEC-0009 UI-0014 CUS-0001 FIN-0015 FIN-0019 FIN-0025 PAYR-0014 PRT-0010 SEC-0002 SEC-0003 SEC-0004 SEC-0008 SEC-0014 SEC-0033 SEC-0034 INV-0008 PAYR-0020 SEC-0005 UI-0029 UI-0030

**ج. مراجعة 2026-11-17 (12)، والتستات المذكورة في [REVIEW_20261117_WHOLE_PROJECT.md](REVIEW_20261117_WHOLE_PROJECT.md) كلها موجودة:**
SEC-0036 FIN-0052 FIN-0053 FIN-0054 INV-0014 OPS-0007 PRT-0019 UI-0048 UI-0049 UI-0050 UI-0051 UI-0052

**د. ملهاش تست مذكور، فاتفحص الكود نفسه عند السطر اللي كانت فيه المشكلة (54):**

| ID | اللي موجود في الكود دلوقتي |
|---|---|
| UI-0002 | طلبات الفلوس بتتحفظ قبل ما تتبعت (`CashierViewModel.Durability.cs`) |
| DB-0008 | `Backup:Automatic:Enabled = false` في `FalahApiFactory.cs:75` |
| DB-0007 | `MigrateOnStartup` بيتقرا من الإعدادات، والإنتاج بيرفض لو فيه migration ما اتطبقش (`Program.cs:264-267`) |
| DB-0002 | الـ `Down` بقى بيعمل `throw NotSupportedException` بدل ما يعيد كتابة الطلبات |
| DB-0004 | اسم ملف النسخة فيه `Guid` (`CreateBackupHandler.cs:58`) |
| FIN-0003 | `AmountToCollect: outstanding` (`DeliveryFactsComposer.cs:35`) |
| FIN-0004 | بيرفض أكتر من تحويل إلكتروني في نفس الطلب (`CheckoutRules.cs:61-62`) |
| FIN-0014 | التأكيد بيقارن يوم وردية التحصيل (`ConfirmElectronicPaymentHandler.cs:77`) |
| FIN-0021 | فهرس unique على `(BusinessDayId, CloseRevision)` (`FalahDbContext.cs:432`) |
| FIN-0022 | فتح اليوم جوه transaction ومعاه `FinancialAdmission.AcquireAsync(closing: true)` (`OpenBusinessDayHandler.cs:27-28`) |
| FIN-0026 | `checked(...)` في عمليات `Money` (`Money.cs:28-37`) |
| FIN-0029 | `netSales = sales.NetSalesPiastres - tax.NetTaxPiastres` (`OwnerDashboardQueryService.cs:272`) |
| FIN-0031 | `Enum.IsDefined(mode)` (`TaxRuleHandlers.cs:36,88`) |
| MENU-0002 | الحجم اللي اتشال بيتقفل (`Active = false`)، مش بيتمسح (`ProductHandlers.cs:129-130`) |
| PAYR-0002 | رصيد التعديلات بيتخصم من الاستقطاعات (`PayrollRules.cs:123-124`) |
| PAYR-0003 | `PayrollRuns WITH (UPDLOCK, HOLDLOCK)` وبعده فحص الحالة (`PayPayrollHandler.cs:69,81`) |
| PAYR-0008 | `.ForDays(periodStart, periodEnd)`، يعني أيام بتوقيت القاهرة (`CreatePayrollHandler.cs:46`) |
| PAYR-0009 | العرض بيمر على `AttendanceInstants.Display(...)` من اللحظة المحفوظة (`EmployeeQueryService.cs:101`) |
| PAYR-0011 / PAYR-0012 | `Enum.IsDefined` وبيتحفظ `compensationType.ToString()` |
| PERF-0005 | الرصيد بـ `GroupBy/Sum` جوه قاعدة البيانات (`InventoryQueryService.cs`) |
| PRT-0002 | فحص `ClaimedBy == request.ClaimId` (`ReportPrintJobResultHandler.cs:46`) |
| PRT-0007 | أداة التحقق بتمسح أمر التست بتاعها بس (`$mine`، `verify-windows-printer-transport.ps1:65-72`) |
| PRT-0014 | `IncludeKitchenRoutes: false` (`CallCenterViewModel.Checkout.cs:399`) |
| PRT-0015 | الستة مسارات كلهم بأسامي عربي (`PrintingViewModel.cs:80-85`) |
| SEC-0010 | `UserIdentityLock` في إنشاء المستخدم وتغيير الرمز والتفعيل والصلاحيات |
| SEC-0012 | إعادة التفعيل بترفض وبتطلب رمز جديد (`SetUserActiveHandler.cs`) |
| INV-0001 / INV-0002 | transaction ومعاه `InventoryWriteLock.AcquireAsync` |
| INV-0004 | `UnknownValueMovementCount` و`UnknownValueByType` (`InventoryQueryService.cs:70-71`) |
| INV-0007 | ورقة الجرد بتبعت `CountedTotalMilliUnits` (`InventoryViewModel.cs:475`) |
| INV-0012 | `ListStockMovementRowsAsync(..., from, to)` (`InventoryQueryService.cs:458`) |
| ARCH-0006 | `Find` بقت متنفذة (`EfFinancialJournal.cs:56`) |
| API-0003 | `OutcomeUnknown` بقى `>= 500 && retryable != false` (`ApiClient.cs:91`) |
| UI-0004 | مفيش `WhiteBrush` في `CashierView.xaml` |
| UI-0005 | الاشتراك في `ActualThemeChanged` (`ThemeResources.cs:20-22`) |
| UI-0007 | `ResetDelivery` بيمسح `PickupDate` و`PickupTime` (`CashierViewModel.Delivery.cs:136`) |
| UI-0008 / UI-0015 / UI-0017 | فحص رقم الإصدار والعنصر المختار بعد كل await |
| UI-0009 | الاختصارات مربوطة بـ `CanUseWorkspaceShortcuts` (`CashierView.xaml.cs:28`) |
| UI-0010 | `ClearResolvedPrint` بقت دالة واحدة (`CallCenterViewModel.Checkout.cs:440`) |
| UI-0013 | إلغاء الاشتراك `-= OnCartChanged`، وفيه `Unloaded` |
| UI-0016 | الكسر بيترفض (`ratioMilli % 1000 != 0`) قبل القسمة |
| UI-0018 | فيه `IngredientSearchText` وفلتر |
| UI-0019 / UI-0020 | لون النص الباهت اتغير لـ `#5B6576`، وفيه `SuccessTextBrush #047857` و`WarningTextBrush #92400E` |
| UI-0022 / UI-0023 | أسامي طرق الدفع بالعربي، والطلب الملغي مكتوب "ملغى" (`CallCenterViewModel.Checkout.cs:301-311`) |
| UI-0035 / UI-0038 | فيه `ScrollViewer` في شاشات الوردية، والـ Escape متعامل معاه في ملفين |
| OPS-0004 / RPT-0004 / DOC-0006 | `DAILY_OPERATIONS_VERIFICATION.md` و`METRIC_DEFINITIONS.md` و`CLAUDE.md` و`.claude/skills` موجودين |

**ما اتفحصوش واحدة واحدة في المراجعة دي (4):**
- HW-0001 وUI-0003 وUI-0034. السجل بيقول إنهم مصلّحين.
- TEST-0003. صفها التاني في السجل بيقول FIXED ومن غير دليل، وصفها الأول بيقول UNVERIFIED (القسم 6).

المجموع 66 + 23 + 12 + 54 + 4 = 159.

## 6. مخاطر ما اتأكدتش، ومعاها اللي الكود بيقوله النهارده

| ID | اللي اتشاف في الكود | اللي لسه مش معروف |
|---|---|---|
| DB-0010 | التحويل بيقفل حساب المصدر بس (`TransferHandler.cs:71`)، وتعارض الأقفال بيتعرف (`BalanceLockContention`) | سلوك الـ deadlock مع كذا كاشير في نفس الوقت تحت ضغط |
| FIN-0045 | — | مين اللي بيتسجل كمعتمد للموافقة. ما اتتبعش |
| FIN-0046 | تكلفة التغليف مش داخلة في ربح لوحة المالك أصلًا (مفيش كلمة "packaging" في `OwnerDashboardQueryService`). واكتمال الحساب بيتقاس بسطور الوصفات بس (`ProfitabilityRules.cs:38`) | قرار المالك: هل التغليف يدخل في الربح؟ |
| INV-0013 | فيه مصدرين للربط: `Product.RecipeId` (`MenuEntities.cs:32`) و`Recipe.ProductId` (`InventoryEntities.cs:6`) | سلوك نقل وصفة لمنتج تاني. ما اتتبعش |
| MENU-0003 | السيرفر بيحسب السعر وقت الشيك أوت، ولو المدفوع أقل بيرفض برسالة عامة (`CheckoutRules.cs:114-115`) | مصدر السعر في سلة البرنامج. ما اتتبعش |
| PRT-0009 | التذكرة بتترسم كصورة واحدة (`BitmapTicketRenderer.cs`) | الرامات اللي بتستهلكها التذاكر الطويلة جدًا. ما اتقاستش |
| PRT-0018 | — | شكل الطباعة على الطابعة الحقيقية. محتاج تجربة في المطعم |
| RPT-0002 | مفيش أي `IsolationLevel` ولا snapshot في `Infrastructure/Reports` | إن الأرقام في أقسام التقرير الواحد ممكن تختلف لو حصل بيع في نص قراءة التقرير. ما اتجربش |
| RPT-0005 | — | ما اتتبعش |
| SEC-0030 | إعادة طباعة الطلب (`/api/orders/{id}/print-jobs`، [Program.cs:2496](../src/Falah.Server/Program.cs#L2496)) وإعادة طباعة المستندات (`/api/print-jobs/reprint-document`، [:2474](../src/Falah.Server/Program.cs#L2474)) محتاجين دخول بس، والـ handlers مفيهاش `AuthorizeCommand`. إعادة الطباعة بتتسجل | قرار المالك: هل إعادة الطباعة محتاجة صلاحية؟ |
| TEST-0003 | — | ما اتتبعش. **والسجل كاتب المشكلة دي في صفين** |
| OPS-0001 / OPS-0003 | انظر القسم 3 | — |

## 7. مش مشكلة أصلًا (11، ما اتراجعوش تاني)

PAYR-0016، SEC-0028، SEC-0029، FIN-0043، INV-0005، SEC-0018، DOC-0005، FIN-0024، FIN-0050، SEC-0013، SEC-0031.

## 8. ملاحظات جديدة من الفحص ده، ومش موجودة في السجل

1. **شباك PowerShell على ويندوز 10 بيعرض العربي مربعات** (اتجرب واتصوّر يوم 2026-11-22). فأسئلة ملفات `deploy/*.cmd` ورسايلها مش بتتقري. `Setup.exe` (`tools/setup/FalahSetup.iss`) بيسأل كل حاجة في شباكه هو، وده لسه ما اتعملهوش commit.
2. **النسخ الاحتياطية في `Backup:CopyToDirectory` عمرها ما بتتمسح.** لو المكان ده فلاشة، هتتملي، وبعدها النسخ بيفشل. الفشل بيتسجل `critical`، والبيع مش بيقف.
3. **كشف اليوم بيبطأ أسرع من عدد الطلبات.** اتقاس يوم 2026-11-21 على قاعدة تجريبية: 1.2 ثانية ليوم فيه 500 طلب، و13.4 ثانية ليوم فيه 3000 طلب.
4. **إعادة الطباعة من غير صلاحية**، والتفاصيل في SEC-0030 في القسم 6. و`/api/print-jobs/overview` و`/api/print-jobs/{id}` مفتوحين لأي حد عامل دخول.
5. **مشاكل في السجل نفسه:**
   - TEST-0003 مكتوبة في صفين.
   - 12 مشكلة من القسم 9 ملهاش ملف في `ISSUES/`.
   - 7 مشاكل الكود بتاعها اتصلح وهي لسه مفتوحة (القسم 4).
   - مستندين في `docs/` كلامهم قديم عن الرموز التجريبية (DOC-0001 في القسم 2).

---

## 9. اللي اتصلح بعد الفحص (نفس اليوم، 2026-11-22)

السجل اتحدّث بالحالات دي. وكل إصلاح ليه تست اتشاف بيفشل الأول وبعدين نجح، إلا لو مكتوب غير كده.

### قرارات المالك اللي اتنفذت
| ID | اللي اتعمل | التست |
|---|---|---|
| PAYR-0004 | المسير بيخصم اللي اتصرف للموظف خارجه في نفس الشهر ("دفع مستحقات" و"سحب")، وعند الصرف ما يدفعش أكتر من رصيد الموظف (من غير السلف). عمود جديد اسمه `PaidOutsidePayrollPiastres`، والـ migration بتاعه إضافة بس (`20261122142055_AddPayrollLinePaidOutsidePayroll`) | `PayrollRulesTests` (2)، و`PayrollVerticalSliceTests` (2). من غير الحد الأقصى المسير كان بيدفع 20000 بدل 5000 |
| SEC-0037 | الحضور والانصراف محتاجين صلاحية `attendance_manage` ورقم عملية إلزامي | `AttendanceVerticalSliceTests` (2 جداد، والكلاس كله 35/35) |
| FIN-0051 | `POST /api/orders/cancel-undispatched-delivery`، وزرار "إلغاء الطلب" في شاشة التوصيل. بيحتاج `delivery_override` أو موافقة مدير، وسبب، وتحديد مصير الأكل، وبيشتغل جوه يوم العمل المفتوح بس | `OrderVoidDispositionTests` (4)، و`DeliveryCancelBeforeDispatchTests` (7) |
| SEC-0030 | إعادة الطباعة محتاجة صلاحية `print_reprint` الجديدة أو رمز مدير، في 5 شاشات | `SlipPrintingAndReprintTests` (1)، وتست الكول سنتر (1) |

### إصلاحات فنية
| ID | اللي اتعمل | التست |
|---|---|---|
| OPS-0008 (جديدة) | **السبب الحقيقي لبطء كشف اليوم.** كل استعلام تقرير كان بيستنى 25 ثانية لحد ما SQL Server يديله ذاكرة (`RESOURCE_SEMAPHORE`)، لأن `min server memory` كان 16 ميجا. تثبيت السيرفر بقى يضبطه على 1024 حد أدنى و2048 حد أقصى | قياس على يوم فيه 3000 طلب: المبيعات من 25.4 ثانية لـ 0.3، والورديات من 50.5 لـ 0.3، والطلبات في الثانية من 39 لـ 67. الإعداد اتجرب مؤقتًا على اللابتوب ده ورجع لـ 16 |
| OPS-0009 (جديدة) | أسئلة ملفات `.cmd` ورسايلها مش بتتقري على ويندوز 10. `Setup.exe` بيسأل كل حاجة في شباكه هو | اتجرب واتصوّر |
| OPS-0002 | النسخ الاحتياطية القديمة بتتمسح من المكان التاني كمان | `BackupVerticalSliceTests` (1، والكلاس كله 15/15) |
| OPS-0006 | لوحة المالك بتعرض المساحة الفاضية على هارد النسخ الاحتياطية، وبتنبّه لو قليلة | `OwnerDashboardVerticalSliceTests` (1) |
| PERF-0003 / PERF-0004 / PERF-0006 | قفل اليوم بيقرا الصفوف المفتوحة بس، وأرصدة الموردين بتتجمع جوه قاعدة البيانات، وأرصدة الموظفين بقت في طلب واحد | تست جديد لأرصدة الموظفين، والكلاسات اللي حواليه نجحت |
| PRT-0005 | إرسال الورقة لطابور الويندوز ما بقاش يوقّف برنامج الطباعة لو الطابور علّق | `SpoolerJobWatchTests` (1) |
| DB-0009 / DOC-0001 | اتشالت حالتين ما بيستخدمهمش أي حاجة، واتصلحت المستندين القدام | قراءة الكود |

### تصحيحات لكلام كتبته فوق في الملف ده
- **`/api/print-jobs/overview` و`/api/print-jobs/{id}` مش مشكلة.** الاتنين مفتوحين **عن قصد**، ومكتوبين في `EndpointReadGateCoverageTests` ومعاهم السبب: "حالة طابور الطباعة، مش محتوى التذاكر". ما اتغيّروش.
- **سبب بطء كشف اليوم هو ذاكرة SQL Server** (OPS-0008)، مش الكود، والدليل في الجدول اللي فوق.

### اللي لسه مستني قرار، واتسجل في `docs/ASSESSMENT.md` و`state.json` من غير ما يتغير
FIN-0005، وFIN-0020، وFIN-0030، وFIN-0042 (فيه منه جزء متنفذ)، وPAYR-0005، وPAYR-0006، وPAYR-0007، وPAYR-0019، وPAYR-0021، وPRT-0008، وPRT-0013، وPRT-0016، وUI-0025، وCUS-0002، وINV-0011، وFIN-0044، وSEC-0007.

### نتيجة التستات بعد الإصلاحات (2026-11-22)
- **Domain:** ‏566 من 566.
- **Client:** ‏449 من 449.
- **تشغيل كامل لتستات السيرفر:** 1062 من 1066 نجحوا، في ساعتين و9 دقايق.
- **الـ 4 اللي فشلوا** كانوا تستات إعادة طباعة (`AuditWiringCoverageTests` و`PrintingVerticalSliceTests`)، والمستخدم فيهم كاشير معندوش الصلاحية الجديدة `print_reprint`. يعني السيرفر رفض زي ما المفروض بعد قرار SEC-0030. ادّينا المستخدم ده في التستين الصلاحية، وبعد التعديل الكلاسين نجحوا 40 من 40.
- **النتيجة النهائية:** 1066 من 1066.