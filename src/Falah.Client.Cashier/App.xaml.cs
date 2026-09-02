using System.Configuration;
using System.Data;
using System.Windows;

namespace Falah.Client.Cashier;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
// The "Falah.Application" project this client depends on and "System.Windows.Application" (WPF's
// app base class) collide by simple name here since both are visible in an enclosing namespace of
// "Falah.Client.Cashier" - fully qualifying avoids relying on using-directive precedence rules.
public partial class App : System.Windows.Application
{
}

