import sys
from substantive_review_engine import execute_review_batch

batch_files = [
    "src/Falah.Server/AuthenticationMiddleware.cs",
    "src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs",
    "src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs",
    "src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs",
    "src/Falah.Infrastructure/Employees/EmployeeQueryService.cs"
]

analyses = {
    "src/Falah.Server/AuthenticationMiddleware.cs": {
        "file_overview": "ASP.NET Core request pipeline middleware establishing identity via bearer token resolution and validating claimed request actor consistency. Hosts critical vulnerabilities SEC-001 (case sensitivity mismatch bypass) and SEC-004 (omission of closedByUserId).",
        "Falah.Server.AuthenticationMiddleware.AuthenticationMiddleware(RequestDelegate)": {
            "behavior": "Injects downstream pipeline RequestDelegate into private field _next.",
            "trust": "Internal DI pipeline container.",
            "mutations": "Assigns _next. Zero state mutations.",
            "concurrency": "Thread-safe singleton registration in ASP.NET Core application builder.",
            "risks": "None."
        },
        "Falah.Server.AuthenticationMiddleware.InvokeAsync(HttpContext, Falah.Infrastructure.Authorization.SessionService)": {
            "behavior": "Inspects request path. Bypasses /health and login anonymous endpoints. Extracts bearer token from Authorization header and calls SessionService.ResolveAsync. Writes HTTP 401 if missing/invalid. Attaches resolved UserEntity to HttpContext.Items['falah.actor']. Calls FindActorMismatchAsync to prevent identity spoofing, writing HTTP 403 on mismatch.",
            "trust": "Network boundary. Treats all request headers and query/body claims as untrusted until verified against session store.",
            "mutations": "Mutates HttpContext.Items with authenticated UserEntity; mutates HttpResponse on 401/403.",
            "concurrency": "Runs concurrently across all HTTP worker threads. SessionService uses scoped DbContext per request.",
            "risks": "Does not enforce RBAC endpoint permissions; relies on downstream handlers for authorization checks."
        },
        "Falah.Server.AuthenticationMiddleware.FindActorMismatchAsync(HttpContext, string)": {
            "behavior": "Compares request query and JSON body against ActorFields: {'actorUserId', 'cashierId', 'createdByUserId', 'openedByUserId', 'reportedByUserId'}. Enables request body buffering, parses JSON via JsonDocument, and compares string value with authenticated user ID.",
            "trust": "Raw client HTTP query parameters and JSON request body.",
            "mutations": "Buffers request body stream and resets Position to 0.",
            "concurrency": "Thread-safe per HttpContext stream.",
            "risks": "VULNERABILITY SEC-001: TryGetProperty is ordinal case-sensitive. PascalCase 'ActorUserId' bypasses check completely. VULNERABILITY SEC-004: ActorFields omits 'closedByUserId'.",
            "finding": "SEC-001"
        },
        "Falah.Server.HttpContextActorExtensions.Actor(HttpContext)": {
            "behavior": "Extracts resolved UserEntity from HttpContext.Items['falah.actor']. Throws InvalidOperationException if unauthenticated.",
            "trust": "Internal server boundary; expects middleware to have populated item.",
            "mutations": "None.",
            "concurrency": "Thread-safe read of HttpContext.Items dictionary.",
            "risks": "Throws exception if called on anonymous routes."
        },
        "Falah.Server.HttpContextActorExtensions.ActorId(HttpContext)": {
            "behavior": "Shorthand accessor returning context.Actor().Id.",
            "trust": "Internal server boundary.",
            "mutations": "None.",
            "concurrency": "Thread-safe string property read.",
            "risks": "None."
        }
    },
    "src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs": {
        "file_overview": "Administrative user permission management handler. Mutates target user permissions under serializable database transaction with owner-immutability and privilege-escalation checks.",
        "Falah.Infrastructure.Users.UpdateUserPermissionsHandler.UpdateUserPermissionsHandler(FalahDbContext, IApprovalRegistry)": {
            "behavior": "Injects EF Core DbContext and approval registry.",
            "trust": "Internal DI container.",
            "mutations": "Assigns private fields. Zero state mutations.",
            "concurrency": "Thread-safe constructor.",
            "risks": "None."
        },
        "Falah.Infrastructure.Users.UpdateUserPermissionsHandler.HandleAsync(Falah.Application.Users.UpdateUserPermissionsRequest, CancellationToken)": {
            "behavior": "Begins Serializable transaction. Queries Users table for request.ActorUserId and checks users_manage permission via AuthorizationPolicy.AuthorizeCommand. Queries target user and asserts target is not owner. Updates permissions hash set and validates that at least one admin survives. Appends audit log entry and commits transaction.",
            "trust": "VULNERABILITY SEC-001: Queries actorUser from request.ActorUserId rather than context.ActorId(). Attacker exploiting case-sensitivity bypass can impersonate administrator to escalate permissions.",
            "mutations": "Mutates target UserEntity.Permissions, inserts ControlAuditEventEntity, commits EF Core transaction.",
            "concurrency": "Serializable isolation level prevents concurrent privilege escalation or simultaneous admin revocation races.",
            "risks": "Critical authorization flaw allowing non-admin caller to grant arbitrary permissions if ActorUserId is spoofed.",
            "finding": "SEC-001"
        }
    },
    "src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs": {
        "file_overview": "Client offline durability store writing in-flight requests to %LOCALAPPDATA% before HTTP dispatch to guarantee crash and power-loss recovery on POS terminals.",
        "Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.Save(string, object)": {
            "behavior": "Packs request into anonymous envelope with Type, SavedAtUtc, and Payload. Serializes to temp JSON file and moves to final path.",
            "trust": "Receives command DTOs containing user credentials and tender payments from client ViewModels.",
            "mutations": "Writes plaintext .json file to station LocalApplicationData folder.",
            "concurrency": "File system writes are atomic via File.Move replace.",
            "risks": "VULNERABILITY SEC-002: Serializes user PINs (InitialPin, CurrentPin, NewPin) in unencrypted plaintext on disk.",
            "finding": "SEC-002"
        }
    },
    "src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs": {
        "file_overview": "Shift close and drawer reconciliation handler executing under SQL Server UPDLOCK/ROWLOCK/HOLDLOCK transaction.",
        "Falah.Infrastructure.Shifts.CloseShiftHandler.HandleAsync(Falah.Application.Shifts.CloseShiftRequest, CancellationToken)": {
            "behavior": "Checks idempotency replay via FindExistingAsync. Loads user matching request.ClosedByUserId. Locks Shift row in SQL Server via FromSqlRaw UPDLOCK hint. Reconciles drawer cash balances and sets ShiftStatus to Closed.",
            "trust": "VULNERABILITY SEC-004: Reads request.ClosedByUserId from request body without middleware session enforcement.",
            "mutations": "Mutates ShiftEntity status, ActualCashPiastres, DiscrepancyPiastres, ClosedAtUtc. Appends AuditLog entry.",
            "concurrency": "UPDLOCK/ROWLOCK/HOLDLOCK prevents concurrent double-closing and race conditions on expected cash calculation.",
            "risks": "Shift close identity spoofing allowing any user to attribute a shift closure to another cashier.",
            "finding": "SEC-004"
        }
    },
    "src/Falah.Infrastructure/Employees/EmployeeQueryService.cs": {
        "file_overview": "Employee reporting and profile query service mapping database entities to DTOs for client consumption.",
        "Falah.Infrastructure.Employees.EmployeeQueryService.ListAsync(CancellationToken)": {
            "behavior": "Queries all active employees with AsNoTracking() and projects via ToDto.",
            "trust": "Invoked from Minimal API endpoint GET /api/employees.",
            "mutations": "None; read-only query.",
            "concurrency": "Safe under concurrent reads; uses AsNoTracking().",
            "risks": "VULNERABILITY SEC-003: Exposes full employee NationalId, MonthlySalaryPiastres, DailyWagePiastres, and FingerprintId without permission check.",
            "finding": "SEC-003"
        },
        "Falah.Infrastructure.Employees.EmployeeQueryService.ToDto(Falah.Infrastructure.Persistence.Entities.EmployeeEntity)": {
            "behavior": "Static mapper constructing EmployeeDto containing NationalId, MonthlySalaryPiastres, DailyWagePiastres, FingerprintId, and Address.",
            "trust": "Internal entity to DTO boundary.",
            "mutations": "None.",
            "concurrency": "Thread-safe pure function.",
            "risks": "Over-exposes highly sensitive employee PII and compensation data.",
            "finding": "SEC-003"
        }
    }
}

if __name__ == '__main__':
    execute_review_batch(batch_files, "Security & Authorization Core (Batch 1)", "security-auditor", analyses)
