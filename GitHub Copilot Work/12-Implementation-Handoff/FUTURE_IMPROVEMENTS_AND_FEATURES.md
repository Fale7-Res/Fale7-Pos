# Future Architectural Improvements and Strategic Features (P2 / P3)

- **Audit Target:** Long-Term Architecture Modernization & POS Enhancements
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit

---

## Priority 2: Architectural Consolidation & Performance (P2)

1. **Complete Deprecation of Legacy WPF Client:**
   Retire `src/Falah.Client.Cashier` (WPF) and standardize 100% of physical restaurant workstations on `src/Falah.Client.CashierWinUI` (WinUI 3). This reduces repository surface by 129 files and eliminates 35% of duplicated ViewModel logic.
2. **Shared Desktop Client Core Library (`Falah.Client.Core`):**
   Extract shared API client services, state managers, and ESC/POS preview controls into a dedicated class library referenced by CashierWinUI and CallCenterWinUI.
3. **Cross-Platform Linux Server Containerization:**
   Refactor `System.Drawing.Common` in `Falah.PrintAgent` to `SkiaSharp`, allowing `Falah.Server` and background workers to run on lightweight Alpine Linux Docker containers on inexpensive mini-PCs or cloud VPS.

---

## Priority 3: Modern POS Features (P3)

1. **Direct Thermal Bluetooth & Network Discovery:**
   Implement mDNS / SSDP zero-configuration discovery for thermal kitchen printers on the restaurant LAN.
2. **Customer-Facing Dual-Screen Display:**
   Add a secondary customer monitor interface in WinUI 3 displaying live cart totals, promotional slides, and QR codes for instant digital payments.
3. **Automated WhatsApp Order Dispatch Notifications:**
   Integrate automated WhatsApp Cloud API webhooks to notify delivery customers when their meal is dispatched with driver contact details.
