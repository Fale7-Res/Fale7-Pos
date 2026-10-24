# Software Supply Chain & Dependency Security Audit (SBOM)

- **Audit Target:** Direct and Transitive NuGet Dependencies & Supply Chain Integrity
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit
- **Status:** Complete Package Inventory & Vulnerability Assessment.

---

## 1. Complete Software Bill of Materials (SBOM)

| Package Name | Resolved Version | Target Projects | Security & Maintenance Assessment |
| :--- | :--- | :--- | :--- |
| **Microsoft.EntityFrameworkCore.SqlServer** | `8.0.11` | `Falah.Infrastructure` | Supported LTS release; parameterized SQL query generation |
| **Microsoft.EntityFrameworkCore.Design** | `8.0.11` | `Falah.Infrastructure`, `Falah.Server` | Development migration tooling |
| **Microsoft.WindowsAppSDK** | `1.5.250108004` / `1.7.260224002` | `CashierWinUI`, `CallCenterWinUI` | **VERSION MISMATCH RISK:** WinUI projects reference conflicting SDK versions |
| **Microsoft.Windows.SDK.BuildTools** | `10.0.22621.3233` | Client WinUI projects | Official Microsoft Windows SDK build tools |
| **CommunityToolkit.Mvvm** | `8.3.2` | WPF and WinUI clients | High-performance Roslyn source generators for INotifyPropertyChanged |
| **QRCoder** | `1.8.0` | `Falah.Domain` / `Printing` | Pure managed QR code generator (used for ZATCA / Egyptian e-receipt compliance) |
| **ZXing.Net** | `0.16.11` | Client projects | Barcode and QR decoding |
| **Swashbuckle.AspNetCore** | `6.6.2` | `Falah.Server` | OpenAPI / Swagger documentation generator |
| **System.Drawing.Common** | `8.0.0` | `Falah.PrintAgent` | Windows GDI+ wrapper for thermal bitmap rasterization (Windows-only) |
| **System.IO.Ports** | `8.0.0` | `Falah.PrintAgent` | Serial / COM port communication for legacy thermal printers |
| **System.Text.Encoding.CodePages** | `8.0.0` | `Falah.Domain` / `Printing` | Code page provider for legacy Arabic encodings (CP864 / Windows-1256) |
| **xunit** | `2.5.3` | Test projects | Standard .NET unit testing framework |
| **coverlet.collector** | `6.0.0` | Test projects | Code coverage collection |

---

## 2. Supply-Chain Risk Findings & Vulnerability Analysis

1. **WindowsAppSDK Version Inconsistency:**
   `Falah.Client.CashierWinUI` references `1.5.250108004` while `Falah.Client.CallCenterWinUI` references `1.7.260224002`. This version skew risks binary incompatibility or divergent Fluent style rendering on POS terminals. Both should standardize on Windows App SDK 1.5 LTS.
2. **Platform Dependency (System.Drawing.Common):**
   `System.Drawing.Common` is strictly Windows-only in .NET 8. The POS server and print agent cannot be containerized or hosted on Linux without refactoring bitmap rendering to SkiaSharp.
3. **No Known Critical CVEs:**
   No known critical remote code execution (RCE) or deserialization CVEs affect the resolved NuGet packages.
