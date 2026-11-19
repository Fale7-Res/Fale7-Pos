namespace Falah.Client.Tests;

using CallCenterServices = Falah.Client.CallCenterWinUI.Services;
using CashierModels = Falah.Client.CashierWinUI.Models;
using CallCenterModels = Falah.Client.CallCenterWinUI.Models;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

/// <summary>
/// UI pass 2026-11-08: the kitchen-note chips ("بدون بصل", "حار") appended their text on every press, so
/// a double tap on the touch till printed "حار، حار" on the kitchen ticket. A chip adds its note once;
/// typed notes are left exactly as written.
/// </summary>
[Collection(RetainedRequestStorageCollection.Name)]
public class NoteChipTests
{
    private static AppSession CashierSession() =>
        new(new ApiClient(new HttpClient { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("actor", "اختبار", "manager", new[] { "all" }), ShiftId = "shift" };

    private static CallCenterServices.AppSession CallCenterSession() =>
        new(new CallCenterServices.ApiClient(new HttpClient { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("operator", "موظف", "cashier", new[] { "pos" }) };

    [Theory]
    [InlineData(null, "حار", "حار")]
    [InlineData("حار", "حار", "حار")]                              // the double tap
    [InlineData("بدون بصل، حار", "حار", "بدون بصل، حار")]
    [InlineData("بدون بصل", "حار", "بدون بصل، حار")]
    [InlineData("حار جدا", "حار", "حار جدا، حار")]                  // a typed note that only contains the word is not the chip
    [InlineData("بدون مخلل\r\n", "حار", "بدون مخلل، حار")]          // a typed note ending in Enter does not leave the chip on a line of its own
    public void A_cashier_chip_adds_its_note_once(string? before, string chip, string expected)
    {
        var vm = new CashierViewModel(CashierSession(), new HeaderViewModel());
        var line = new CashierModels.CartLine { ProductId = "p", ProductName = "كبدة", UnitPricePiastres = 4500, Notes = before };
        vm.AppendNoteText(line, chip);
        Assert.Equal(expected, line.Notes);
    }

    [Theory]
    [InlineData("حار", "حار", "حار")]
    [InlineData("بدون بصل", "حار", "بدون بصل، حار")]
    public void A_call_center_chip_adds_its_note_once(string? before, string chip, string expected)
    {
        var vm = new Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel(CallCenterSession(), new());
        var line = new CallCenterModels.CartLine { ProductId = "p", ProductName = "كبدة", UnitPricePiastres = 4500, Notes = before };
        vm.AppendNoteText(line, chip);
        Assert.Equal(expected, line.Notes);
    }
}
