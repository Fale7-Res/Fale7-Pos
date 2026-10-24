import sys
sys.path.insert(0,'Codex Work/Sub-Agents/Results')
from ui_facet_receipts import *
for prefix,name,line in [(C,'CallCenterOrderPanelView',11),(C,'CategoryStripView',9),(C,'ProductCatalogueView',10),(P,'CartPanelView',12),(P,'CategoryStripView',10),(P,'ProductCatalogueView',10)]:
 persist_facets(prefix+'Views/'+name+'.xaml.cs',{(line,5):('Casts current DataContext to the expected '+('CallCenterViewModel' if prefix==C else 'CashierViewModel')+' and returns null for incompatible context; evaluates on every access rather than caching. Event handlers type-check before dispatch, and this getter neither initializes VM nor authorizes operations.','DataContext; code-behind event handlers','DataContext replacement/lifetime differs between views; explicit subscriptions reviewed separately, no native binding run.')},{})
persist_facets(P+'Views/CashierView.xaml.cs',{
(21,5):('Returns current DataContext as CashierViewModel or null; helper used by shortcut routing, no state mutation or cache.','CashierView keyboard handlers','Typed null guard protects absent VM, not modal focus or command policy.'),
(27,5):('Workspace interactive requires editable operation and no checkout result, variant picker or checkout modal. It does not explicitly exclude refund, expense, shift, delivery/pickup overlays. Shortcut handlers rely this helper, UI010 source evidence; native key routing unobserved.','Handle shortcut methods; CashierViewModel.CanEditOperation','Existing parent challenge owns UI010 qualification, no screenshot or event-routing claim.')},{})
