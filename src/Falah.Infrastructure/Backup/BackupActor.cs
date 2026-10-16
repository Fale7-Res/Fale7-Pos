namespace Falah.Infrastructure.Backup;

/// <summary>
/// Who a backup is attributed to in the audit trail. An operator-triggered backup carries the real
/// user who pressed the button; the nightly schedule has no operator behind it, so it carries
/// <see cref="System"/> rather than borrowing some manager's name for work they did not do - the
/// audit log is the one place in this system that must never say a person did something they didn't.
/// </summary>
public readonly record struct BackupActor(string Id, string Name, string Role)
{
    public static readonly BackupActor System = new("system-scheduler", "النسخ الاحتياطي التلقائي", "system");
}
