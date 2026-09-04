using Falah.Domain.Audit;
using Falah.Domain.Common;
using Xunit;

namespace Falah.Domain.Tests;

public class AuditEventTests
{
    [Theory]
    [InlineData("info")]
    [InlineData("normal")]
    [InlineData("warning")]
    [InlineData("critical")]
    public void Every_reference_app_severity_value_is_accepted(string severity)
    {
        AuditRules.AssertValidSeverity(severity);
    }

    [Fact]
    public void An_unknown_severity_is_rejected()
    {
        Assert.Throws<DomainException>(() => AuditRules.AssertValidSeverity("catastrophic"));
    }

    [Fact]
    public void Appending_to_the_in_memory_log_makes_it_readable_back_for_tests()
    {
        var log = new InMemoryAuditLog();
        var eventRecord = new AuditEvent("audit-1", "u-1", "أحمد", "cashier", "استرداد طلب", "order", "تفاصيل", "critical", DateTimeOffset.UtcNow);
        log.Append(eventRecord);
        Assert.Single(log.Events);
        Assert.Equal("audit-1", log.Events[0].Id);
    }
}
