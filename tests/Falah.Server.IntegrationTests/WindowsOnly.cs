// The suite runs only on Windows: SQL Server Express on this machine, the print agent's spooler transport and its
// GDI+ ticket renderer. Declared once so the platform analyzer (CA1416) checks against that instead of warning on
// every renderer and spooler call.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]
