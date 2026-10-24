# Verified System Architecture and Repository Source Map

- **Audit Target:** Fale7 POS Architecture, Component Dependency Graph & Source Catalog
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit

---

## 1. System Architecture Diagram

```mermaid
graph TD
    subgraph Presentation["Presentation Layer (Desktop Clients)"]
        WPF["Falah.Client.Cashier (WPF Desktop - Legacy)"]
        WinUI["Falah.Client.CashierWinUI (WinUI 3 Modern POS)"]
        CC["Falah.Client.CallCenterWinUI (WinUI 3 Call Center)"]
    end

    subgraph ServerLayer["Host & Communication Layer"]
        Server["Falah.Server (ASP.NET Core Web API / SignalR Hubs)"]
    end

    subgraph ApplicationLayer["Application Use Cases & Workflows"]
        App["Falah.Application (Commands, Queries, DTOs, Handlers)"]
    end

    subgraph DomainLayer["Core Domain Model (Pure Logic)"]
        Domain["Falah.Domain (Entities, Value Objects, Pure Rules, Invariants)"]
    end

    subgraph InfrastructureLayer["Infrastructure & Persistence"]
        Infra["Falah.Infrastructure (EF Core, Dapper, SQL Server, Printing)"]
        PrintAgent["Falah.PrintAgent (Windows Background Worker)"]
    end

    subgraph DataStore["Persistence & Hardware"]
        SQL[("SQL Server / LocalDB")]
        Thermal["80mm Thermal Hardware (ESC/POS)"]
    end

    WPF -->|"HTTP / SignalR"| Server
    WinUI -->|"HTTP / SignalR"| Server
    CC -->|"HTTP / SignalR"| Server
    Server --> App
    App --> Domain
    Infra --> App
    Infra --> Domain
    Server --> Infra
    PrintAgent --> Infra
    Infra --> SQL
    PrintAgent --> Thermal
```

---

## 2. Project Directory & Responsibility Catalog

| Project | File Count | Lines of Code | Key Responsibilities |
| :--- | :--- | :--- | :--- |
| **`src/Falah.Domain`** | 35 | ~4,200 | Pure business rules, `Money` value object, order aggregates, tax engine, attendance/payroll rules, printing layouts. |
| **`src/Falah.Application`** | 28 | ~2,800 | DTO definitions, CQRS requests/results, command validators, business exceptions. |
| **`src/Falah.Infrastructure`** | 185 | ~22,500 | `FalahDbContext`, entity models, SQL Server migrations, repository implementations, Dapper query services, ESC/POS rendering. |
| **`src/Falah.Server`** | 18 | ~2,100 | ASP.NET Core host, Minimal API route registrations, JWT/Session middleware, `OrderHub`, Swagger OpenAPI setup. |
| **`src/Falah.Client.Cashier`** | 86 | ~8,900 | Legacy WPF cashier touch-screen interface, XAML views, ViewModels, print coordinator. |
| **`src/Falah.Client.CashierWinUI`** | 134 | ~16,400 | Modern WinUI 3 cashier interface, Fluent controls, custom dialogs, multi-currency tender calculators. |
| **`src/Falah.Client.CallCenterWinUI`** | 68 | ~7,200 | WinUI 3 call center interface, multi-branch order routing, customer phone lookup, dispatch management. |
| **`src/Falah.PrintAgent`** | 12 | ~1,500 | Windows background worker, polling loop for pending print jobs, Windows Spooler P/Invoke wrapper (`RawPrinterHelper`). |
| **`tests/Falah.Domain.Tests`** | 18 | ~2,400 | Domain rule unit tests (`MoneyTests`, `CheckoutRulesTests`, `PayrollRulesTests`). |
| **`tests/Falah.Server.IntegrationTests`** | 62 | ~9,800 | Vertical slice integration tests using `WebApplicationFactory` and SQL Server LocalDB. |
| **`tests/Falah.Client.Tests`** | 16 | ~1,900 | Client ViewModel and cart state transition unit tests. |
| **`docs/` & `tools/`** | 28 | ~3,500 | Original developer specifications, receipt preview utility, and automation scripts. |

---

Complete file list available in: [`01-Source-Baseline/VERIFIED_FILE_INVENTORY.csv`](01-Source-Baseline/VERIFIED_FILE_INVENTORY.csv)
