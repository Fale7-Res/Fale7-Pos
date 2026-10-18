from ui_source_receipts import *
persist_exact(C+'App.xaml.cs',{
(12,5):('Initializes XAML thenconfigurescallcenterstationjournal beforeanyorderdeskconstruction. FileStoreconstructorcreatesLocalAppData directory andcanthrowstartup ifpermissionfailure; noerrorcallbackpassedso laterIOerrorssilent. ReviewerdoesnotlaunchApp.','FileRetainedRequestStore.DirectoryFor(callcenter)','No newdirectoryorcredentialsread byreview.'),
(21,5):('NormalbuildcreatesMainWindow/Activate; conditionalSCREEN_LAYOUT_PROBE insteadstartsverificationwindow. Compiletimebranchonly, noautoruntimeprobingfromreview. Retainswindowfieldforlifetime.','MainWindow; Window.Activate; conditionalNativeScreenLayoutProbe','Publishedbuildconstants mustexcludeprobe foractualPOS, parentbuildmetadata check.')
},'Read complete CallCenterApp source;2 exact startup methods, normal/probecompilebranches differentiated without execution.')
persist_exact(P+'App.xaml.cs',{
(17,43):('Returnsstaticmainwindowforunpackagedfilepicker HWNDparenting. NullbeforeOnLaunched/verificationbranches; caller musthandle.','MenuEditorView picker host priorfile report','Probe/no-mainwindowpath notactualappclaim.'),
(17,48):('Privatestoresnormalmainwindowreferenceonlaunch; neverclearedClosed, processnormallyendsbutstaticreferenceholdswindowuntilshutdown. No externalsetter.','OnLaunched assignment','Windowclose/reopenlifetime code notavailablehere.'),
(19,5):('Initializesresources thenconfigurescashier-localjournal beforeVMfieldinitializers recover. Defaultprofilefileconstructorcanabortstartup; noonErrorcallback so subsequentIOerrorssilentSEC002/persistencecontext.','FileRetainedRequestStore.DirectoryFor(cashier)','No actualLocalAppDatafile read orwrite.'),
(29,5):('NormalbranchcreatesMainWindow, setsMainAppWindow andactivates. FOOTER/SCREEN_LAYOUT_PROBEcompiledbranchescreateisolatedverificationwindowinstead; mustnotconfusebuildprobedatawithactualPOSruntime.','Window.Activate; conditionalprobeStart','Parentownsactualbuildsymbols/nativeevidence; reviewerexecutesnone.')
},'Read complete CashierApp startup source and4 exact accessor/startup spans. Stationjournal initialization and conditionalverificationbuilds onlystatic.')
persist_exact(C+'MainWindow.xaml.cs',{
(13,5):('InitializesXAML/rootVM, bindsclose/header, attachesclosing/closed/lock/propertylisteners, rendersinitialpage andmaximizes. No themeServiceintegrationhereunlikeCashier, so CallCenter themebehaviorseparate.','MainViewModel; Apply methods; MaximizeOnTargetDisplay','Nativewindow/theme/layoutnotobserved.'),
(18,30):('Nativeclosingcancel uses rootTryClose pendingguard. DoesnotprotectOSforcekill, no discard ofjournal.','AppWindow.Closing -> TryClose','Actualcloseeventnativeunverified.'),
(19,19):('ClosedunsubscribesrootPropertyChanged thenStophealth/headerclocks. HeaderLockeventnotexplicitlyremoved, samewindowlifetime likelyfine.','MainVM.Stop','No unconditionalmemoryleakclaim.'),
(22,37):('HeaderlockeventexecutesrootLockCommand, whichchecksaggregatepending. Doesnotrevokeauthheaderitself.','LockCommand.Execute','Securityrootlock/sharedbearerchallenge independent.'),
(34,5):('MaximizesOverlappedPresenteronly, noexplicitmonitorselectiondespitemethodname. Avoidsfixedphysicalpixelresize.','OverlappedPresenter.Maximize','Multimonitor/DPI nativeunverified.'),
(40,5):('RoutesknownthreePropertyChanged names toUIupdates, ignoresnull/allpropertiesnames. Standardgeneratedsetters concrete namesnormal.','ApplyIsLoggedIn/Online/CurrentView','Thread-affinitycaller expectedDispatcher; nativeexceptionsnotasserted.'),
(56,5):('Headerhiddenonlogin/shownwhenloggedin, visibilitynotauthentication state clearing. OldusertextremainsinsidehiddenheaderVM.','HeaderHost.Visibility','No tokenrevocationclaim.'),
(59,5):('Offlinebannerinversehealthboolean; optimisticinitialtrue rootmeansstartupnetworkuncheckeduntilping. HealthnotDBreadiness.','OfflineBanner.Visibility','Connectionfailureandlatepingtest.'),
(62,5):('CreatesnewLogin/CallCenterUserControlforcurrentVM eachrouting, noimplicitWinUItemplates. CachedCallCenterVMretainsoldCart eventsubscriberunlessviewdetaches UI015.','new LoginView/CallCenterView; RootContent.Content','ActualCallCenterViewCartsubscriptionchainalreadyfilereviewed; nativeunobserved.')
},'Read complete CallCenterwindow source;9 exact constructor/eventlambda/routing spans reconciled. Windowlifecycleauditedstatically only.')
