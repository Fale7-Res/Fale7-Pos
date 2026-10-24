from ui_persistent import *
record(C+'Styles/Colors.xaml','CallCenter static light green palette and utility/status brushes.',[
 ('BrandPrimaryColor','Actual primary#156B5A and action#146C59 green contradict old blue comment. Follow actual values, not documentation. Utility WhiteBrush literalwhite valid selected brand foreground; not proof global replace is safe.'),
 ('CartBackgroundColor','Cart/header/item arewhite and footer#F9FAF7 despite dark-cart comments. TextPrimary#25342E and muted#66756C intended light surfaces. No ThemeDictionaries or high contrast override in file.'),
 ('SuccessPillBrush','Semantic pills use saturated success/danger/warning/neutral backgrounds, typography StatusPillText white11px. Warning#D97706 white pairing requires contrast validation; semantics not color-only when label retained.'),
 ('ShellNavSelectedColor','Management shell retains older blue selection while brand green; stylistic inconsistency not functional defect without rendered evidence.'),
 ('ProductImageGrillGradientBrush','Two abstract gradients remain defined despite comment claiming no photo pipeline; actual catalogue supports ImageUri. Treat stale comment, not missing feature.'),
 ('CategoryBadgeBrush','Light green#EAF2EC badge separate category glyph. All brushes resolve preceding Color static resource; this file stores no secrets/data/writes.')
 ],'Source resource review; no native/high contrast screenshots. No callable symbols. Need actual states/brush consumers before contrast finding severity.','Maintain semantic foreground/background pairs and remove stale palette/pipeline comments only in approved future change.')
record(C+'Styles/Typography.xaml','CallCenter explicit keyed Arabic text hierarchy.',[
 ('TextBase','Segoe UI, static TextPrimary, RTL common base; explicit keys not implicit default for all TextBlocks. Local overrides may differ.'),
 ('TotalAmountText','Total24Black/title20Black and section14ExtraBold; body13 and muted11. Source sizes do not prove actual physical readability at DPI.'),
 ('ProductNameText','Product15ExtraBold wraps; Price16Black brand; category12Bold center and badge11Bold. Caller catalogue explicitly overrides many sizes, so these are not universal metrics.'),
 ('CartItemNameText','Actual16Bold dark primary contradict white-on-dark comment, correct for current white card palette.'),
 ('MetricValueText','Management26Black and page22Bold, subtitle/body13; caption11 and tableheader12 muted. Dense text requires scaling checks.'),
 ('StatusPillText','White11SemiBold forces light glyph on any semantic pill; neutral/warning contrast and high contrast mode need native check. Nav13SemiBold white.')
 ],'NOT_EXECUTED. XAML no callable declarations; screenreader reading order, fallback Arabic glyph shaping, OS text scale and contrast not observed. Source comment reference credentials were not copied into report.','Use theme/high contrast semantic text styles and test all style consumers; do not claim current native accessibility from nominal font sizes.')
record(W+'Styles/Colors.xaml','Legacy WPF blue palette with dark cart and header.',[
 ('BrandPrimaryColor','Primary#0F52BA and action#0866E8 actualblue distinguishes active green WinUI clients; no runtime theme dictionary.'),
 ('CartBackgroundColor','Dark cart#151C24/footer#11171E paired intended white/gray text. Surface/card remain light; consumers must choose correct text token.'),
 ('WarningColor','Semantic warning#D97706/success#059669/danger#DC2626 with dedicated hover colors. Numeric color definition alone not uservisible state without consumer.'),
 ('TextPrimaryBrush','Each brush references preceding Color, no dynamic SystemColors/highcontrast resources. TextPrimarydark on light and TextOnDarkwhite separate responsibilities.')
 ],'NOT_EXECUTED; resource definitions no callable symbols, DB/printing transactions NOT_APPLICABLE. Need consumer-specific contrast/highcontrast and legacy layout tests.','Keep legacy palette findings separate from active WinUI UI002; no global resource edits authorized.')
record(W+'Styles/Typography.xaml','Legacy WPF keyed Segoe UI Arabic typography.',[
 ('TextBase','Base fixesRTL and dark foreground. Total24Black/title20Black/section14ExtraBold reflect explicit nominal sizes, not independent rendering evidence.'),
 ('ProductNameText','15ExtraBold wraps; price16Blackbrand, category12Boldcenter, badge11Bold. Muted11 smaller user text may need scale review.'),
 ('CartItemNameText','13Boldwhite specifically dark cart. Different from active and CallCenter16dark styles; do not assume interchangeable shared resource behavior.')
 ],'NOT_EXECUTED; no callable declarations. Arabic glyph fallback/native DPI, contrast and reading order remain unobserved.','Validate only if legacy client deployed; preserve active frontend distinction.')
record(W+'Views/AppHeaderView.xaml','Legacy WPF64px header with restaurant, shift, time, lock and operator badge.',[
 ('IsShiftOpen','WPF DataTrigger dot emerald/slate plus explicit ShiftLabel text, so not sole-color state. Right logo group and left time/user groups float without shared columns, longnames/narrowwindow can overlap.'),
 ('TimeText','Clock/dateLTR insideRTL header; VM determines timezone. Fixed64height plus9/10px secondarytext may clip atOStextscale.'),
 ('LockCommand','RelativeSource ancestorWindow.DataContext correctly finds main shell command beyond HeaderVM. No bearer revocation behavior in markup; coordinator tracks lock/session separately.'),
 ('UserName[0]','Initial index binding may fail for emptyusername, WPF binding fallback rather than checked glyph. Full name/role remain visible in text; no photo/credentialread.')
 ],'NOT_EXECUTED; no native window interaction. No callable declarations. Need longArabicname/emptyuser/smallwidth/lockpending tests.','Use grid columns/trimming and safe initial converter if legacy maintained; do not infer app security from lock icon.')
record(W+'Views/LoginView.xaml','Legacy WPF dark PIN keypad and login action.',[
 ('PinDisplay','Bound display withMinHeight34 but no visibleemptyentryborder; errors wrap. Raw PIN not in markup.'),
 ('UniformGrid','3columns4rows inheritsRTL fromUserControl, unlike activeWinUI explicitLTRkeypad. Source predicts mirrored visualdigit order; nativeconfirmation unobserved.'),
 ('BoolToLoginLabelConverter','Busy label bound to submit command; digits/clear/backspace relyVMguards not IsEnabledbusy binding. Backspaceglyph no explicitaccessiblename.'),
 ('Width="440"','Fixed440/padding40 no ScrollViewer risks smallviewport/largetextclipping. Decorative red PINforeground and dark backdrop needpairwisecontrast tests.')
 ],'NOT_EXECUTED; no real authentication. No callabledeclarations. Legacykeypadorder/scaling/keyboard/accessiblenames remain native-test gaps.','Give numeric keypad explicitLTR and accessible actions only in approvedlegacyfix; activeWinUI alreadyexplicitLTR.')
