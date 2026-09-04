namespace Falah.Application.Audit;

public sealed record AuditEventDto(
    string Id, string UserId, string UserName, string UserRole, string Action, string Category,
    string Details, string Severity, DateTimeOffset TimestampUtc, string? ShiftId, string? Device,
    string? Reason, string? AuthorizedBy, string? PreviousValue, string? NewValue, string? OperationId, string? ReferenceId);
