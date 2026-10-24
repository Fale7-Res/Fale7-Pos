# docs/PRINT_AGENT.md

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Worktree SHA256 `5f0309684031f1f0d05f5ae4598558e64bab372662340ba82fa3f50fa79df0d6`; Git blob SHA256 `c27b94ad0ef00e2d73879b4cb39d4efe95a14cb44a8f55ffb83a68674304bb46`. Read lines1–155.

Status: full static document/configuration review; functions NOT_APPLICABLE.

## role

PrintAgent queue/transport/configuration and operator recovery documentation.

## reasoning

All155lines read in2bounded chunks. Correctly separates station HTTP from Infrastructure DB access, route ownership, pre-send uncertain-attempt journal and file-only simulation. Machine mutex cannot fence a second machine(PRT001). Boolean success still reflects local transport acceptance, not physical paper(PRT002), despite paper-oriented introductory wording. Claim every customer reprint excludes kitchen is false for CallCenter defaultflag(UI012). Physical image-mode claims coexist with unverified-paper section; retain dated hardware provenance rather than silently declaring current Arabic clipping solved. Printed-byte validation fixture and paused-queue acceptance do not prove real paper or actual UI preview. Recommending verify-windows-printer-transport as safe is contradicted by PRT005 broad job cleanup/unchecked pause; --test-print physically sends when real transport configured.

## relationships

- PrintJobProcessor/UnreportedAttemptStore
- WindowsSpoolerTicketTransport
- BitmapTicketRenderer/EscPosTicketRenderer
- CreatePrintJobsHandler defaults and CallCenter reprint
- PRT001/002/003/004/005; UI012

## tests

No printer queue, test-print, journal deletion, config PIN inspection, output drain or physical transport invoked. Historical10agent/7renderer and file-flow tests not rerun. File transport must not drain real backlog for dashboard success.

## recommendation

Document distinct submitted/unknown/physically-confirmed levels, cross-host station fencing and explicit reprint route flags. Provide approved isolated queue/transport verification and true rendered-preview workflow with no financial or print success mutation.

## risks

Deploying file transport or replaying after uncertain paper can mark unprinted work complete or duplicate food; config credentials and live journals stay private.

## Scope and effects

Inputs are human/operator/build configuration; outputs are instructions/build membership/ignore behavior as described above. No application business logic, XAML, packages, databases, services or printers modified. Runtime/hardware claims remain explicitly unverified.
