from ui_persistent import *
record(P+'Views/InventoryModalsView.xaml','Stock-count correction and loss-entry overlays.',[
 ('IsCountOpen','440wide max640 full-cardScrollViewer includes close/submit; can scroll tall content but footer not pinned. Shows captured balance+unit and actual physical count/difference. Runtime stale baseline conflict handling belongsAPI/VM.'),
 ('CountManagerPin','Inputs disabledCanEditAction and managerPasswordBox only CountNeedsApproval. Amountactual header explicit, numeric InputScope not validation. No PIN secret value read or copied.'),
 ('SubmitCountCommand','Submit relies command guard while textbox disabled; errorwrap present. Fixedcaption gives no retained retrylabel, recovery visibility may dependVMopenflag.'),
 ('SetWasteTypeCommand','Three typed loss categories separate, selected brush whitebrand/unselected transparentmuted. Categorybuttons no explicitCanEditAction but VM command must freeze retained choice.'),
 ('WasteQuantityText','Loss quantity/reason/approvalPINdisabledCanEditAction, description promises frozen latest purchase cost. UI cannot establish backendcostfreeze; domainreviewindependent.'),
 ('IsWasteOpen','440max660scroll includesheader/body/footer; stackedGridmodal flags requireexclusiveVMstate. No explicitfocustrap or escapehandler.')
 ],'NOT_EXECUTED. Same-process managementtests coverindividualcount/waste immutable retry, not nativefocus orprocess-recovered modal visibility. No callableXAMLsymbols.','Expose retry/context forrecovered requests and preserve type/actor/baseline; focus andscalingnativeverificationpending.')
record(P+'Views/WasteView.xaml','Read-only dated waste/loss summary and movement list.',[
 ('BusinessDateLabel','Prev/nextbuttons have Arabicaccessible names andalways-enabled markup; VMmustqueue/coalesce dates. Summarytitle explicitlyonebusinessdate.'),
 ('Summary.TotalLossValuePiastres','Summary metric alwaysvisible regardlessIsLoading/error; stale/nullsummariesmayread olddate with newheading. ByTypehorizontalmin140percategory plus170total needs substantialwidth; no horizontalwrapping.'),
 ('Movements','Rows showtype/quantity/value/reason but noitemname, timestamp orbusinessdate. Therefore VMglobalrecentmovementdate mismatch cannot be detectedfromtable itself; priorWasteVMsource candidate requires endpoint/datechallenge.'),
 ('NullablePiastresToDisplayConverter','Individualnullcost explicitlyunvaluednot0. SummarytotalusesnonnullablePiastresconverter andcannotaloneconvey unknownlosscoverage; backendDTOsemanticsmustbechecked.'),
 ('HasNoLoss','Empty statesays no lossinthisday, whiletableunconditionallyboundMovements. Differentdate-filtersemanticscanyieldcontradictoryemptybanner/list; nativecasesnotobserved.'),
 ('LoadCommand','Spinner/error/retryoverlay tableonly, nottopsummaries; fixed140/120/130columns plusreasonstar potentialnarrowviewportpressure. EagerItemsControlnotvirtualized.')
 ],'NOT_EXECUTED. Needrapiddatechanges, serverrecentlistversusdatedsummary, unknowncostcoverage andlongreason/narrowlayout. No callabledeclarations.','Exposemovementitem/timeanddate-consistentloadingdata; neverclaimdatedsummaryandglobalrecentrowsreconcilewithoutqueryevidence.')
