namespace Falah.Server.IntegrationTests;

using System.Text.RegularExpressions;
using Falah.Domain.Orders;
using Falah.Infrastructure.Orders;

/// <summary>Independent audit 2026-11-23, F-14. Orders store their status lower-case ("cancelled"). Queries
/// compared it with nameof(OrderStatus.Cancelled) - "Cancelled" - and were right only because the database
/// collation ignores case; on a case-sensitive installation every cancelled order would count as a sale.
/// No database is needed: this reads the server's own source.</summary>
public class StoredStatusConventionTests
{
    [Fact]
    public void The_cancelled_constant_is_the_stored_form()
    {
        Assert.Equal(OrderStatus.Cancelled.ToWireString(), OrderEnumMapping.CancelledWire);
    }

    [Fact]
    public void No_query_compares_an_order_column_with_an_enum_name()
    {
        var root = RepositoryRoot();
        var pattern = new Regex(@"o\.(Status|PaymentMethod|OrderType|ElectronicPaymentStatus)\s*[!=]=\s*nameof\((OrderStatus|PaymentMethod|OrderType|ElectronicPaymentStatus)\.");
        var offenders = new[] { "src/Falah.Infrastructure", "src/Falah.Server" }
            .SelectMany(dir => Directory.EnumerateFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains("Migrations"))
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, i, line)))
            .Where(x => pattern.IsMatch(x.line))
            .Select(x => $"{Path.GetRelativePath(root, x.f)}:{x.i + 1}")
            .ToList();
        Assert.True(offenders.Count == 0, "Compare with the stored (wire) value instead: " + string.Join(", ", offenders));
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Falah.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Falah.sln not found above the test output.");
    }
}
