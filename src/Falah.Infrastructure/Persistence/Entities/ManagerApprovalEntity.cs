namespace Falah.Infrastructure.Persistence.Entities;

/// <summary>Persisted form of a single-use manager approval. ApprovalId is the primary key - the
/// same "the id itself is the uniqueness guarantee" pattern as FinancialJournalEntries.</summary>
public class ManagerApprovalEntity
{
    public string ApprovalId { get; set; } = default!;
    public string ApproverUserId { get; set; } = default!;
    public string ApproverName { get; set; } = default!;
    public string ApproverRole { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string? Reason { get; set; }
    public DateTimeOffset ApprovedAtUtc { get; set; }
    public bool Consumed { get; set; }
}
