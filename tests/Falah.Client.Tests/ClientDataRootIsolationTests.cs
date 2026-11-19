using Xunit;
using Cashier = Falah.Client.CashierWinUI.Services;
using CallCenter = Falah.Client.CallCenterWinUI.Services;

namespace Falah.Client.Tests;

/// <summary>A verification session must be able to run the real clients without touching the
/// station's own pending operations (owner's verification brief, 2026-11-08).</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public class ClientDataRootIsolationTests
{
    [Fact]
    public void Both_clients_keep_pending_operations_under_an_explicit_data_root_when_one_is_given()
    {
        var root = Path.Combine(Path.GetTempPath(), "falah-root-" + Guid.NewGuid().ToString("N"));
        var previous = Environment.GetEnvironmentVariable("FALAH_CLIENT_DATA_ROOT");
        try
        {
            Environment.SetEnvironmentVariable("FALAH_CLIENT_DATA_ROOT", root);
            Assert.Equal(Path.Combine(root, "pending-operations", "cashier"), Cashier.FileRetainedRequestStore.DirectoryFor("cashier"));
            Assert.Equal(Path.Combine(root, "pending-operations", "callcenter"), CallCenter.FileRetainedRequestStore.DirectoryFor("callcenter"));
        }
        finally { Environment.SetEnvironmentVariable("FALAH_CLIENT_DATA_ROOT", previous); }
    }

    [Fact]
    public void Without_an_override_the_station_folder_is_unchanged()
    {
        var previous = Environment.GetEnvironmentVariable("FALAH_CLIENT_DATA_ROOT");
        try
        {
            Environment.SetEnvironmentVariable("FALAH_CLIENT_DATA_ROOT", null);
            var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Falah", "pending-operations", "cashier");
            Assert.Equal(expected, Cashier.FileRetainedRequestStore.DirectoryFor("cashier"));
        }
        finally { Environment.SetEnvironmentVariable("FALAH_CLIENT_DATA_ROOT", previous); }
    }
}
