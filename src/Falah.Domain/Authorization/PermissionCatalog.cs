namespace Falah.Domain.Authorization;

public sealed record PermissionDefinition(string Key, string Category);

/// <summary>Every permission string this system's own AuthorizationPolicy/handlers actually check -
/// built from evidence (every AuthorizeCommand/HasPermission/AuthorizeExpense/AuthorizeAddSupplier
/// call site in this codebase), NOT copied from the reference app's own 38-string catalog, most of
/// which gates features that do not exist in this system yet (Employees, Attendance, Devices,
/// Settings, grill-station-only login, etc.). This is exactly the fix IMPLEMENTATION_MATRIX.md row 4b
/// calls for: the reference app's own Users screen famously exposed only 10 of its 38 permission
/// strings - this catalog is complete by construction, since a new permission literal introduced
/// anywhere in this codebase belongs here too.</summary>
public static class PermissionCatalog
{
    public static readonly IReadOnlyList<PermissionDefinition> All = new List<PermissionDefinition>
    {
        new("all", "system"),
        new("pos", "orders"),
        new("cash_drawer_sale", "orders"),
        new("orders_comp", "orders"),
        new("orders_refund", "orders"),
        new("shifts_blind_close", "shifts"),
        new("shifts_manage", "shifts"),
        new("shifts_open_own", "shifts"),
        new("orders_discount", "orders"),
        new("orders_view_all", "orders"),
        new("expenses_add_drawer", "expenses"),
        new("expenses_main_treasury", "expenses"),
        new("treasury_manage", "treasury"),
        new("safe_drop", "treasury"),
        new("cash_drawer", "treasury"),
        new("business_day_open", "business_day"),
        new("business_day_close", "business_day"),
        new("business_day_reopen", "business_day"),
        new("control_center", "business_day"),
        new("cash_count", "business_day"),
        new("purchasing_manage", "purchasing"),
        new("suppliers_manage", "purchasing"),
        new("inventory_manage", "inventory"),
        new("menu_manage", "menu"),
        new("offers_manage", "menu"),
        new("settings_manage", "settings"),
        new("delivery_manage", "delivery"),
        new("delivery_override", "delivery"),
        new("delivery_settlement_approve", "delivery"),
        new("reports_view", "reports"),
        new("users_manage", "users"),
        new("audit_view", "audit"),
        new("employees_manage", "employees"),
        new("employees_ledger", "employees"),
        new("attendance_manage", "employees"),
        new("payroll_approve", "employees"),
        new("payroll_pay", "employees"),
        new("backup_manage", "backup"),
    };

    public static readonly IReadOnlySet<string> Keys = All.Select(p => p.Key).ToHashSet();
}
