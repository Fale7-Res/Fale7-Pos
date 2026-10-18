import sys,ast
sys.path.insert(0,'Codex Work/Sub-Agents/Results')
from ui_source_receipts import *
# All seven Cashier bodies were independently read in full in this pass. Their exact
# executable statements and source positions match CC counterparts; reuse the already
# manually authored per-body analysis, not a name/index inference or script import.
tree=ast.parse(Path('Codex Work/Sub-Agents/Results/ui-exact-cc-controls.py').read_text(encoding='utf-8-sig'))
facts=ast.literal_eval(next(n.value for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='facts' for t in n.targets)))
for name,f in facts.items():
 if name=='SummaryCardView':
  x=Path(P+'Controls/SummaryCardView.xaml').read_text(encoding='utf-8-sig');assert 'Visibility="Collapsed"' in x
 persist_exact(P+'Controls/'+name+'.xaml.cs',f,'Cashier control full source independently read; seven implementations have equivalent executable property/event bodies to CallCenter counterparts, each exact span reconciled separately.')
s={
(22,5):('Initializes sparkline XAML then subscribes own SizeChanged and Loaded events to Render; no model subscriptions or asynchronous task owned. Geometry rebuilt only when size, load or dependency property changes.','InitializeComponent; SizeChanged/Loaded -> Render','Mutable same-reference Points collection updates do not trigger dependency-property callback unless owner reassigns or layout changes; owner contract needs testing.'),
(25,24):('On own size change invokes Render with current series and dimensions, avoiding stale geometry sized for prior bounds.','SizeChanged -> Render','Resize behavior and cost with long series require native/profile tests.'),
(26,19):('On Loaded renders using realized ActualWidth/ActualHeight; helps initial dependency changes that occurred before useful dimensions existed.','Loaded -> Render','First render remains native-unobserved.'),
(31,36):('Points dependency change casts sender SparklineView and rerenders; callback does not subscribe to collection change, so mutations inside unchanged list are not directly observed.','PointsProperty callback -> Render','Check replacement versus in-place list mutation contract.'),
(37,9):('Returns raw Points dependency value as object to permit IReadOnlyList<long> binding without wrappers; typed interpretation happens in Series expression.','GetValue(PointsProperty); Series expression','Unsupported object silently becomes empty series via Series fallback.'),
(38,9):('Sets Points object in dependency store; registered callback redraws if value changes, without validating numeric ranges.','SetValue(PointsProperty); Points callback','Boundary and unsupported input tests missing.'),
(43,36):('LineBrush change invokes Render so stroke, fill and endpoint share updated brush; this callback does not itself observe theme resource transitions.','LineBrushProperty callback -> Render','Dynamic theme brush binding requires native confirmation.'),
(47,9):('Reads nullable Brush from dependency store; Render supplies gray default if no brush was provided.','GetValue(LineBrushProperty)','Default and custom brush visual contrast not observed.'),
(48,9):('Stores brush dependency value and triggers redraw through registered property callback; no shared application resource mutation.','SetValue(LineBrushProperty)','Verify replacing brush and clearing it.'),
(53,5):('Returns before drawing for dimensions <=1; otherwise assigns common brush, scans min/max, and clears geometry/shows dash for absent, one-point or constant series. Nonflat series maps points with 3px padding, builds line/area, places marker at last point. Linear scans plus geometry allocations O(n); long max-min subtraction occurs before double cast and can overflow at extreme long inputs. Width between1 and6 yields negative usable span. Domain monetary bounds and card minimum dimensions may mitigate these theoretical extremes.','Series; BuildLine; BuildArea; LinePath/AreaPath/EndPoint/FlatLabel','No new finding from unreachable extremes; test zero/constant/negative/mixed/long-extreme series and tiny dimensions. Owner collected-series mismatch remains separate saved candidate.'),
(104,5):('Builds open unfilled figure starting points[0], appends line segments for remaining points then wraps in PathGeometry. Assumes nonempty collection; verified Render invokes only for at least2 nonflat points.','Render -> BuildLine; PathFigure; LineSegment','Unit geometry endpoints/segment count test missing; no independent public empty-list caller observed.'),
(115,5):('Builds closed filled polygon from first x at baseline through all line points then last x at baseline; Render supplies at least2 points and height-minus-padding baseline. Area is visual emphasis, not integrated numerical quantity.','Render -> BuildArea; PathGeometry','Check negative monetary trends and clipping visually; no claim area equals monetary totals.')}
persist_exact(P+'Controls/SparklineView.xaml.cs',s,'Exact sparkline dependency-property, event lambda and geometry construction reasoning; source-only no rendering/profiling.')
