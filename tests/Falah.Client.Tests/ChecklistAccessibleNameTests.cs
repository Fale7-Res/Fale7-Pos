using Falah.Application.BusinessDay;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// UI pass 2026-11-08: a Control Center checklist item showed done / not done only as an icon. Its UI
/// Automation name was the label alone, so a screen reader could not tell which items were done.
/// </summary>
public class ChecklistAccessibleNameTests
{
    [Theory]
    [InlineData(true, true, "فحص الطابعة، تم، إلزامي")]
    [InlineData(false, true, "فحص الطابعة، لم يتم، إلزامي")]
    [InlineData(false, false, "فحص الطابعة، لم يتم")]
    public void The_name_says_whether_the_item_is_done(bool completed, bool required, string expected) =>
        Assert.Equal(expected, ControlCenterViewModel.ChecklistAccessibleName(
            new ChecklistItemStatusDto("i", "فحص الطابعة", "close", required, completed, null, null)));
}
