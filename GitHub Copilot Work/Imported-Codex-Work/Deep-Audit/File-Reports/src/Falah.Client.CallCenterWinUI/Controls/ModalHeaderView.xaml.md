# src/Falah.Client.CallCenterWinUI/Controls/ModalHeaderView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `906b6cb98984282eff0b88c483fd0307a62091467245803e6840cff84d6d67bc`; worktree SHA256 `906b6cb98984282eff0b88c483fd0307a62091467245803e6840cff84d6d67bc`; LF-equivalent `True`.

Actual role: CallCenter RTL title/close modal header.

## Function/resource review

- `TitleBlock` at `src/Falah.Client.CallCenterWinUI/Controls/ModalHeaderView.xaml:20`: Star/Auto columns separate wrapping max2line title from close target. Right alignment with RTL; close glyph explicit Arabic AutomationProperties.Name. Uses shared44DIP close style.

- `CloseButton` at `src/Falah.Client.CallCenterWinUI/Controls/ModalHeaderView.xaml:23`: Button command assigned code-behind dependency property; no dismiss on background click or Escape/focus trap in header itself. Parent controls full modal semantics.

## Tests and limitations

Native long Arabic title/close focus tests required; NOT_APPLICABLE financial authorization.

## Recommendations (not implemented)

Keep explicit close name, verify keyboard focus and owner cancellation safety.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
