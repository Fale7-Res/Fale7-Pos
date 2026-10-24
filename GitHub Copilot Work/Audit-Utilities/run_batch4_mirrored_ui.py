import sys
from substantive_review_engine import execute_review_batch

batch_files = [
    "src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs",
    "src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs",
    "src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs",
    "src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs",
    "src/Falah.Client.CashierWinUI/Views/ReportsView.xaml.cs"
]

analyses = {
    "src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs": {
        "file_overview": "Legacy WPF cashier view model handling order lookup, status translation, and refund submission. Diverges from WinUI client in idempotency key reuse during network transport ambiguities.",
        "Falah.Client.Cashier.ViewModels.CashierViewModel.SubmitRefundAsync()": {
            "behavior": "Submits refund request to API. Generates fresh RefundRequestId per invocation without persisting or reusing original ID across ambiguous network retries.",
            "trust": "Cashier UI interaction.",
            "mutations": "Mutates view model status strings and calls ApiClient.RefundOrderAsync.",
            "concurrency": "UI Dispatcher thread.",
            "risks": "FLAW FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001: Ambiguous network timeout retry sends new RefundRequestId, risking double-debiting drawer cash on server.",
            "finding": "FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001"
        }
    },
    "src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs": {
        "file_overview": "Modern WinUI 3 cashier view model handling refund workflows, reason codes, manager approvals, and split tender breakdown display.",
        "Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitRefundAsync()": {
            "behavior": "Validates reason code, prompts manager approval if required, and dispatches to CompleteRefundAsync using RetainedRequestStore durability.",
            "trust": "Cashier touch till input.",
            "mutations": "Sets IsRefunding state, updates ObservableCollection balances.",
            "concurrency": "WinUI CoreDispatcher / UI thread.",
            "risks": "SEM-MIRROR-065: Validation rules for partial refunds diverge between client view model and server RefundRules."
        }
    },
    "src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs": {
        "file_overview": "Menu and product variant configuration view model. Binds category selections, pricing inputs, day choices, and product image uploads.",
        "Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.CreateRequest()": {
            "behavior": "Collects input fields into CreateProductRequest / UpdateProductRequest DTOs. Allows negative price delta variants without client-side floor validation.",
            "trust": "Manager menu configuration input.",
            "mutations": "Generates API request payload.",
            "concurrency": "UI thread.",
            "risks": "FLAW FIN-CAND-NEGATIVE-VARIANT-PRICE-001: Submits negative variant prices that server accepts, allowing carts with negative totals to consume inventory.",
            "finding": "FIN-CAND-NEGATIVE-VARIANT-PRICE-001"
        }
    },
    "src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs": {
        "file_overview": "Call center shell view model managing operator authentication state, station locking, health checks, and view transitions.",
        "Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.OnLoggedIn()": {
            "behavior": "Async void event handler triggered upon login completion. Executes background initialization, health check polling, and station state recovery.",
            "trust": "Internal application event.",
            "mutations": "Mutates CurrentUser, CurrentView, Header state.",
            "concurrency": "Executes on UI thread context as async void.",
            "risks": "FLAW ASYNC-001: Unhandled exceptions inside async void terminate the entire application process abruptly without crash recovery.",
            "finding": "ASYNC-001"
        }
    },
    "src/Falah.Client.CashierWinUI/Views/ReportsView.xaml.cs": {
        "file_overview": "Reports and shift summary presentation view binding financial statements, daily sales charts, and drawer handover receipts.",
        "Falah.Client.CashierWinUI.Views.ReportsView.ReportsView()": {
            "behavior": "Initializes XAML components via InitializeComponent(). Binds reports view model.",
            "trust": "Internal WinUI presentation tree.",
            "mutations": "Builds Visual Tree.",
            "concurrency": "UI thread only.",
            "risks": "FLAW UI-002: Hardcoded styling causes 1.22:1 contrast ratio failure on dark theme POS displays. Missing ItemsControl virtualization causes UI thread freeze on large reports."
        }
    }
}

if __name__ == '__main__':
    execute_review_batch(batch_files, "Mirrored Code, UI & Maintainability (Batch 4)", "mirrored-code-auditor", analyses)
