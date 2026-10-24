import sys
from substantive_review_engine import execute_review_batch

batch_files = [
    "src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs",
    "src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs",
    "src/Falah.PrintAgent/PrintJobProcessor.cs",
    "src/Falah.PrintAgent/PrintServerClient.cs",
    "src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs"
]

analyses = {
    "src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs": {
        "file_overview": "Print job creation handler dispatching ticket generation across the 6 system routes (Receipt, Kitchen, Grill, Shawarma, Appetizers, CallCenter). Hosts critical defect PRT-001 (ticket duplication).",
        "Falah.Infrastructure.Printing.CreatePrintJobsHandler.HandleAsync(Falah.Application.Printing.CreatePrintJobsRequest, CancellationToken)": {
            "behavior": "Checks existing print jobs by CreateRequestId. If not found, groups order items by preparation stations, generates PrintJobEntity instances, and persists them to PrintJobs table.",
            "trust": "Print request from cashier checkout auto-print or manual reprint dialog.",
            "mutations": "Inserts PrintJobEntity records to database.",
            "concurrency": "Only dedupes on RequestId. Missing unique composite constraint on (OrderId, Route) in PrintJobs table allows concurrent checkout events or auto-print/reprint collisions to generate duplicate kitchen tickets.",
            "risks": "CRITICAL FLAW PRT-001: Duplicate kitchen prep tickets causing food waste and double meal preparation.",
            "finding": "PRT-001"
        }
    },
    "src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs": {
        "file_overview": "Hardware transport submitting raw ESC/POS byte streams to local Windows printer queues via unmanaged Win32 spooler P/Invoke APIs (winspool.drv).",
        "Falah.PrintAgent.WindowsSpoolerTicketTransport.Send(string, byte[])": {
            "behavior": "Calls OpenPrinter, StartDocPrinter, StartPagePrinter, WritePrinter, EndPagePrinter, EndDocPrinter, ClosePrinter. Returns true once bytes are accepted by Windows spooler queue.",
            "trust": "Local system spooler interaction.",
            "mutations": "Writes raw byte payload to Windows Print Spooler memory buffer.",
            "concurrency": "Win32 spooler serialization per printer handle.",
            "risks": "CRITICAL FLAW PRT-002: Conflates spooler queue acceptance with physical hardware printing. Paper-out, paper jams, and offline printers silently report success to POS till while food tickets are never physically printed.",
            "finding": "PRT-002"
        },
        "Falah.PrintAgent.WindowsSpoolerTicketTransport.SendAsync(byte[], CancellationToken)": {
            "behavior": "Executes synchronous Send(_printerName, payload) on calling thread without honoring CancellationToken.",
            "trust": "PrintAgent background worker.",
            "mutations": "Synchronous Win32 I/O.",
            "concurrency": "Blocks thread pool thread during network printer spooler delays.",
            "risks": "FLAW PRT-003: Thread pool starvation during printer offline/network timeouts because CancellationToken is completely ignored.",
            "finding": "PRT-003"
        }
    },
    "src/Falah.PrintAgent/PrintJobProcessor.cs": {
        "file_overview": "Core processing loop of PrintAgent polling pending jobs from server, executing rasterization/formatting, dispatching to transport, and reporting execution results.",
        "Falah.PrintAgent.PrintJobProcessor.ProcessAsync(Falah.Application.Printing.PrintJobDto, CancellationToken)": {
            "behavior": "Fetches ticket payload from server, passes to transport, records attempt, and calls ReportResultAsync.",
            "trust": "Server HTTP API responses.",
            "mutations": "Local retry cache mutation, network HTTP reporting.",
            "concurrency": "Serial execution per agent instance.",
            "risks": "Spooler false positives report Success to server even when printer hardware is jammed."
        }
    },
    "src/Falah.PrintAgent/PrintServerClient.cs": {
        "file_overview": "HTTP client communicating with Falah.Server to poll print queues and report hardware ticket status.",
        "Falah.PrintAgent.PrintServerClient.AuthenticateAsync(string, CancellationToken)": {
            "behavior": "Sends POST /api/auth/login-by-pin with station PIN at startup and caches bearer token. Never re-authenticates if session expires or server restarts.",
            "trust": "Server authentication endpoint.",
            "mutations": "Sets _bearerToken field in HttpClient default headers.",
            "concurrency": "Single worker thread.",
            "risks": "FLAW PRT-004: A single 401 Unauthorized permanently breaks printing; the agent enters an infinite loop failing every request until service restart.",
            "finding": "PRT-004"
        }
    },
    "src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs": {
        "file_overview": "Service triggered upon order checkout to evaluate automatic ticket printing rules based on order type, payment method, and station settings.",
        "Falah.Infrastructure.Printing.OrderAutoPrintService.TryPrintAsync(Falah.Application.Orders.CheckoutOrderRequest, Falah.Application.Orders.CheckoutOrderResult, CancellationToken)": {
            "behavior": "Evaluates settings and suppresses kitchen printing for electronic pending orders. Completely lacks a mechanism to dispatch prep tickets for unpaid pickup orders.",
            "trust": "CheckoutOrderResult from server.",
            "mutations": "Calls CreatePrintJobsHandler.HandleAsync.",
            "concurrency": "Executes asynchronously after checkout transaction commits.",
            "risks": "FLAW FLOW-001: Branch pickup workflow lacks a 2-stage state machine; kitchen preparation cannot be dispatched without forcing immediate till payment.",
            "finding": "FLOW-001"
        }
    }
}

if __name__ == '__main__':
    execute_review_batch(batch_files, "Printing & Hardware Reliability (Batch 3)", "printing-workflow-auditor", analyses)
