from ui_incremental import *
import xml.etree.ElementTree as ET
X='{http://schemas.microsoft.com/winfx/2006/xaml}'
colors=ET.parse(ROOT/(P+'Styles/Colors.xaml')).getroot()
themes={}
for n in colors:
 if n.tag.endswith('ResourceDictionary.ThemeDictionaries'):
  for t in n:themes[t.attrib[X+'Key']]={e.attrib[X+'Key']:(e.text or '').strip() for e in t if X+'Key' in e.attrib}
assert themes['Light'].keys()==themes['Dark'].keys(),'Theme resource keys differ'
def lum(h):
 h=h.lstrip('#');v=[int(h[i:i+2],16)/255 for i in [0,2,4]];v=[x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4 for x in v]
 return sum(x*w for x,w in zip(v,[.2126,.7152,.0722]))
def ratio(a,b):
 x,y=sorted([lum(a),lum(b)]);return round((y+.05)/(x+.05),4)
pairs=[]
for theme,keys in themes.items():
 for key in ['TextPrimaryColor','BrandPrimaryColor','ActionBlueColor','SuccessColor','WarningColor','DangerColor']:
  value=keys[key];pairs.append({'theme':theme,'foreground':'#FFFFFF','background':value,'backgroundKey':key,'contrast':ratio('#FFFFFF',value),'status':'SOURCE_TOKEN_MATH_NOT_RENDERED'})
 pairs.append({'theme':theme,'foreground':keys['TextPrimaryColor'],'background':'#FFFFFF','usage':'unselected expense/payment/category background overrides','contrast':ratio(keys['TextPrimaryColor'],'#FFFFFF'),'status':'SOURCE_TOKEN_MATH_NOT_RENDERED'})
controls=ET.parse(ROOT/(P+'Styles/Controls.xaml')).getroot();styles=[n.attrib.get(X+'Key',n.attrib.get('TargetType')) for n in controls if n.tag.endswith('Style')]
output={'baseline':SHA,'parser':'Python XML ElementTree parses actual XAML XML; not WinUI semantic/layout execution','themeResourceKeyCount':{k:len(v) for k,v in themes.items()},'sameThemeKeys':True,'styleKeysOrImplicitTargets':styles,'contrastPairs':pairs,'limitations':'Literal sRGB token math only; does not model antialiasing, opacity compositing, inherited rendered brush resolution or Windows high contrast. Visual proof NOT_OBSERVED.'}
dest=ROOT/'Codex Work/Deep-Audit/UI-UX/source-theme-analysis.json';dest.write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf-8')
record(P+'Styles/Colors.xaml','Complete Light/Dark palette and literal utility/gradient brushes.',[
('ResourceDictionary.ThemeDictionaries',f'Full XML and source reviewed: Light/Dark each{len(themes["Light"])}keyed entries, exact matching key sets verified by real XML parser. This verifies structure, not native theme lookup resolution. Every themed Color and Brush pairing examined as semantic map; flatWhite/Transparent/gradients distinct.'),
('TextPrimaryColor','Light#0F172AandDark#E6E9EE. Dark white-background mismatch in expense/payment/category gives source-token ratio'+str(ratio('#E6E9EE','#FFFFFF'))+':1. Correct DarkCard#1E232Bpair far different; fix call sites, not literalWhite globally.'),
('ActionBlueColor','Primary ActionBlue/BrandPrimary brighten inDark but control foreground often fixedWhite. Source contrast table includes all Light/Dark accent/semantic white pairs; some bright dark fills below ordinary readable contrast. No actual native screenshot or compliance certification.'),
('SuccessPillBrush','Pills map semantic Success/Danger/Warning/Neutral to bright Darkcolors; TypographyStatusPillText fixedWhite. Low source contrast repeat beyond expense; accessibility text/background pair should adapt coherently.'),
('WhiteBrush','Literal paper/on-accent utility intentionally outside theme dictionaries; correct many uses. Product gradients static two palettes but comments claiming no image pipeline stale: actual image URI/converter/picker/server paths now exist. Documentation drift, no fake photos inserted.')],
'Actual source XML parsed and contrast math saved UI-UX/source-theme-analysis.json; Native binding/ActualTheme/highcontrast/rendered pixels NOT_TESTED. Function count NOT_APPLICABLE resource file.',
'Use per-state semantic color pairs, verify theme overrides and live converted brushes, update misleading historical comments only after authorized change.')
record(P+'Styles/Controls.xaml','Complete reusable button/control/layout design system with visual states.',[
('BaseButtonStyle','Full source and XML style entries reviewed. Base Template BorderContentPresenter maps Background/Border/radius/padding; CommonStates Normal/hover.92/press.8/disabled.45opacity, no explicit FocusStates. Native UseSystemFocusVisuals behavior must be observed, not assumed absent.'),
('PrimaryButtonStyle','Accent/White styles Primary/Cta/Success/Danger/selected variants; Darkaccent brightness can reduce white contrast. Secondary/Payment base semantic surface+TextPrimary correct until overridden by viewWhitebinding UI-002.'),
('SuccessOutlineButtonStyle','Outline success/info/warn/danger each light/tinted semantic backgrounds and matching colored border/text; source theme table supports selected text concerns. Segment fully fills cells, selected Danger/White alternate.'),
('ProductCardStyle','Fixed150x190old style; actual catalogue GridView uses custom card content and width calculation, so do not infer active product dimensions solely from unused style comment. Quantity44DIP, note min44, keypad60; modal close44; Icon40target smaller.'),
('SearchBoxStyle','RTL pillTextBox42height, ordinary TextBox/Password/Combo/Calendar48height; NumberBox compact spin48. Keycap/summary/table/alert/card/dialog styles are presentation only no backend/printing actions.'),
('ShellNavViewStyle','Legacy NavigationView dictionary style still present though current shell uses C#Buttons; historical XamlCompiler comments about1.5may not apply current1.7. No actual compiler run/repro asserted.'),
('DialogDestructiveButtonStyle','Explicit danger fill helps distinguish destructive operation; does not enforce confirmation/authorization. ContentDialog preferred semantics only used if call sites adopt, custom Gridoverlays elsewhere no focus trap implemented by this file.')],
f'XML parser found{len(styles)}style entries, machine-readable list saved. No tests/native screenshots, highcontrast, text scaling or keyboard-focus proof. Function count NOT_APPLICABLE.',
'Test actual used style/callsite full states and focus; semantic selected/unselected foreground/background; do not globally alterWhiteutility.')
print(json.dumps(output['themeResourceKeyCount']),len(styles),ratio('#E6E9EE','#FFFFFF'))
