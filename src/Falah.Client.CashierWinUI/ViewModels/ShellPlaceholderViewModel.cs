using CommunityToolkit.Mvvm.ComponentModel;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>Honest "not built yet" placeholder for a nav destination whose real screen has not been
/// implemented in this pass. Deliberately shows no data of any kind (not even sample data) - per the
/// "real data only" constraint, a screen that cannot yet call the real API must say so plainly
/// rather than render something that looks finished.</summary>
public sealed partial class ShellPlaceholderViewModel : ObservableObject
{
    [ObservableProperty] private string _moduleTitle;

    public ShellPlaceholderViewModel(string moduleTitle)
    {
        _moduleTitle = moduleTitle;
    }
}
