namespace Falah.Infrastructure.Persistence;

using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public class FalahDbContext : DbContext
{
    public FalahDbContext(DbContextOptions<FalahDbContext> options) : base(options) { }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();
    public DbSet<OrderRefundEntity> OrderRefunds => Set<OrderRefundEntity>();
    public DbSet<ShiftEntity> Shifts => Set<ShiftEntity>();
    public DbSet<BusinessDayEntity> BusinessDays => Set<BusinessDayEntity>();
    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();
    public DbSet<TreasuryMovementEntity> TreasuryMovements => Set<TreasuryMovementEntity>();
    public DbSet<FinancialJournalEntryEntity> FinancialJournalEntries => Set<FinancialJournalEntryEntity>();
    public DbSet<OrderSequenceEntity> OrderSequence => Set<OrderSequenceEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<ManagerApprovalEntity> ManagerApprovals => Set<ManagerApprovalEntity>();

    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<ProductVariantEntity> ProductVariants => Set<ProductVariantEntity>();
    public DbSet<ProductImageEntity> ProductImages => Set<ProductImageEntity>();
    public DbSet<UserSessionEntity> UserSessions => Set<UserSessionEntity>();
    public DbSet<PriceRuleEntity> PriceRules => Set<PriceRuleEntity>();
    public DbSet<PriceRuleProductEntity> PriceRuleProducts => Set<PriceRuleProductEntity>();
    public DbSet<PriceRuleDayEntity> PriceRuleDays => Set<PriceRuleDayEntity>();
    public DbSet<TaxRuleEntity> TaxRules => Set<TaxRuleEntity>();
    public DbSet<RecipeEntity> Recipes => Set<RecipeEntity>();
    public DbSet<RecipeItemEntity> RecipeItems => Set<RecipeItemEntity>();
    public DbSet<InventoryItemEntity> InventoryItems => Set<InventoryItemEntity>();
    public DbSet<StockMovementEntity> StockMovements => Set<StockMovementEntity>();
    public DbSet<PrepRecipeEntity> PrepRecipes => Set<PrepRecipeEntity>();
    public DbSet<PrepRecipeLineEntity> PrepRecipeLines => Set<PrepRecipeLineEntity>();
    public DbSet<OrderPackagingRuleEntity> OrderPackagingRules => Set<OrderPackagingRuleEntity>();

    public DbSet<SupplierEntity> Suppliers => Set<SupplierEntity>();
    public DbSet<PurchaseEntity> Purchases => Set<PurchaseEntity>();
    public DbSet<PurchaseLineEntity> PurchaseLines => Set<PurchaseLineEntity>();
    public DbSet<PurchasePaymentEntity> PurchasePayments => Set<PurchasePaymentEntity>();
    public DbSet<SupplierLedgerEntryEntity> SupplierLedgerEntries => Set<SupplierLedgerEntryEntity>();

    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<CustomerAddressEntity> CustomerAddresses => Set<CustomerAddressEntity>();

    public DbSet<DriverEntity> Drivers => Set<DriverEntity>();
    public DbSet<DeliveryServiceEntity> DeliveryServices => Set<DeliveryServiceEntity>();
    public DbSet<DeliveryEntity> Deliveries => Set<DeliveryEntity>();
    public DbSet<DriverCustodyEntity> DriverCustodies => Set<DriverCustodyEntity>();
    public DbSet<DeliveryReassignmentEntity> DeliveryReassignments => Set<DeliveryReassignmentEntity>();
    public DbSet<DeliveryEventEntity> DeliveryEvents => Set<DeliveryEventEntity>();
    public DbSet<DriverSettlementEntity> DriverSettlements => Set<DriverSettlementEntity>();

    public DbSet<PrintJobEntity> PrintJobs => Set<PrintJobEntity>();
    public DbSet<PrintJobAttemptEntity> PrintJobAttempts => Set<PrintJobAttemptEntity>();

    public DbSet<ChecklistItemEntity> ChecklistItems => Set<ChecklistItemEntity>();
    public DbSet<ChecklistCompletionEntity> ChecklistCompletions => Set<ChecklistCompletionEntity>();
    public DbSet<BusinessDayCloseSnapshotEntity> BusinessDayCloseSnapshots => Set<BusinessDayCloseSnapshotEntity>();
    public DbSet<ControlExceptionStateEntity> ControlExceptionStates => Set<ControlExceptionStateEntity>();
    public DbSet<ControlAuditEventEntity> ControlAuditEvents => Set<ControlAuditEventEntity>();
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();
    public DbSet<CashDrawerOpenEntity> CashDrawerOpens => Set<CashDrawerOpenEntity>();
    public DbSet<CashDenominationCountEntity> CashDenominationCounts => Set<CashDenominationCountEntity>();
    public DbSet<SettingsEntity> Settings => Set<SettingsEntity>();
    public DbSet<CashHandoverEntity> CashHandovers => Set<CashHandoverEntity>();
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
    public DbSet<EmployeeLedgerEntryEntity> EmployeeLedgerEntries => Set<EmployeeLedgerEntryEntity>();
    public DbSet<AttendanceRecordEntity> AttendanceRecords => Set<AttendanceRecordEntity>();
    public DbSet<PayrollRunEntity> PayrollRuns => Set<PayrollRunEntity>();
    public DbSet<PayrollLineEntity> PayrollLines => Set<PayrollLineEntity>();
    public DbSet<BackupRecordEntity> BackupRecords => Set<BackupRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderEntity>(b =>
        {
            b.HasKey(o => o.Id);
            // The single most important constraint in the whole schema: the database itself, not
            // just an in-memory check, refuses a second Order for the same checkout request - a
            // race between a same-task double click and its own network retry cannot both win.
            b.HasIndex(o => o.CheckoutRequestId).IsUnique();
            b.HasIndex(o => o.SequenceNumber).IsUnique();
            // A plain application-managed concurrency token rather than a native SQL Server
            // rowversion column (IsRowVersion()) - callers that mutate an existing order (refund,
            // payment confirmation) are responsible for incrementing it before SaveChanges. This
            // works identically to SQLite here: IsConcurrencyToken() adds a WHERE Version = @original
            // clause to the UPDATE regardless of provider.
            b.Property(o => o.Version).IsConcurrencyToken();
            b.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(o => o.Refunds).WithOne().HasForeignKey(r => r.OrderId).OnDelete(DeleteBehavior.Cascade);
            // No CLR navigation property to Shift/User by design (Order does not need to load
            // them) - these are pure referential-integrity constraints: an order can never
            // reference a shift or cashier that does not exist in the database.
            b.HasOne<ShiftEntity>().WithMany().HasForeignKey(o => o.ShiftId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<UserEntity>().WithMany().HasForeignKey(o => o.CashierId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItemEntity>(b =>
        {
            b.HasKey(i => i.Id);
            // A sold line can never point at a product/variant that never existed - and, since
            // this FK exists, a Product/Variant that has ever been sold can never be hard-deleted
            // either, which is exactly the protection historical order data needs.
            b.HasOne<ProductEntity>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductVariantEntity>().WithMany().HasForeignKey(i => i.VariantId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OrderRefundEntity>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<ShiftEntity>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.OpenRequestId).IsUnique();
            // Filtered so multiple still-open shifts (CloseRequestId = NULL) don't collide on
            // uniqueness - only an actual close request id needs to be unique.
            b.HasIndex(s => s.CloseRequestId).IsUnique().HasFilter("[CloseRequestId] IS NOT NULL");
            // Single system-wide active shift (ported from openNewShift's own `if (activeShift)
            // throw` guard in the reference app) - the database itself, not just an application-
            // level check, refuses a second row with Status='Open' to exist at once. Filtered so
            // any number of Closed shifts coexist freely; only "Open" is constrained to at most one.
            b.HasIndex(s => s.Status).IsUnique().HasFilter("[Status] = 'Open'");
            b.Property(s => s.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<TreasuryMovementEntity>(b =>
        {
            b.HasKey(m => m.Id);
            b.HasIndex(m => m.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<BusinessDayEntity>(b =>
        {
            b.HasKey(d => d.Id);
            b.HasIndex(d => d.OpenRequestId).IsUnique();
        });

        modelBuilder.Entity<ExpenseEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.IdempotencyKey).IsUnique();
            // A cashier's own drawer reconciliation (ShiftReconciliation.ComputeExpectedCashAsync)
            // and the manager visibility view (GET /api/expenses) both filter by exactly these two
            // shapes - see expensesVisibleToActor in the reference app.
            b.HasIndex(e => new { e.ShiftId, e.PaidFromDrawer });
            b.HasIndex(e => e.BusinessDate);
        });

        modelBuilder.Entity<FinancialJournalEntryEntity>(b => b.HasKey(j => j.IdempotencyKey));

        modelBuilder.Entity<OrderSequenceEntity>(b =>
        {
            b.HasKey(s => s.Id);
            // The historical floor of 127 carried over from the current web app's seed data - a
            // fresh restaurant install still starts numbering from #0128, matching what staff are
            // already used to seeing on today's receipts.
            b.HasData(new OrderSequenceEntity { Id = 1, LastAllocated = 127 });
        });

        modelBuilder.Entity<UserEntity>(b =>
        {
            b.HasKey(u => u.Id);
            // No DB-level unique index on the PIN anymore - now that it is hashed with a per-user
            // random salt, two different rows holding the SAME pin never produce equal PinHash
            // values, so a unique index could never catch a collision anyway. Uniqueness is enforced
            // at the application layer instead (verify the candidate PIN against every OTHER active
            // user's hash) - see UserManagementQueryService/CreateUserHandler's own "PIN already in
            // use" check, the same shape setEmployeePin's own collision check took in the reference
            // app for exactly this reason (it already couldn't rely on a DB constraint either, since
            // Employee.pin and User.pin are two separate columns there).
        });

        modelBuilder.Entity<ManagerApprovalEntity>(b => b.HasKey(a => a.ApprovalId));

        modelBuilder.Entity<UserSessionEntity>(b =>
        {
            b.HasKey(x => x.Id);
            // Every authenticated request looks a session up by this hash, so it is the one index
            // that has to exist; unique because two sessions cannot share a token.
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.Property(x => x.TokenHash).HasMaxLength(64);
        });

        modelBuilder.Entity<CategoryEntity>(b => b.HasKey(c => c.Id));

        modelBuilder.Entity<ProductEntity>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasOne<CategoryEntity>().WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(p => p.Variants).WithOne().HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
            // A product's RecipeId is a plain nullable reference, not enforced as an FK here,
            // because Recipe.ProductId already points the other way (one product -> one active
            // recipe is the real relation); this column only feeds resolution at checkout time.
        });
        modelBuilder.Entity<ProductVariantEntity>(b => b.HasKey(v => v.Id));

        modelBuilder.Entity<ProductImageEntity>(b =>
        {
            b.HasKey(i => i.ProductId);
            b.Property(i => i.ContentType).HasMaxLength(80);
            b.Property(i => i.Version).HasMaxLength(40);
            // Cascade, unlike the rest of the menu graph: an image is not a record of anything that
            // happened, so a deleted product should not leave its photo behind.
            b.HasOne<ProductEntity>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PriceRuleEntity>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasMany(r => r.Products).WithOne().HasForeignKey(p => p.PriceRuleId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(r => r.Days).WithOne().HasForeignKey(d => d.PriceRuleId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PriceRuleProductEntity>(b =>
        {
            b.HasKey(p => new { p.PriceRuleId, p.ProductId });
            b.HasOne<ProductEntity>().WithMany().HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PriceRuleDayEntity>(b => b.HasKey(d => new { d.PriceRuleId, d.DayOfWeek }));

        modelBuilder.Entity<TaxRuleEntity>(b => b.HasKey(t => t.Id));

        modelBuilder.Entity<RecipeEntity>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasOne<ProductEntity>().WithMany().HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(r => r.Items).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecipeItemEntity>(b =>
        {
            b.HasKey(i => i.Id);
            b.HasOne<InventoryItemEntity>().WithMany().HasForeignKey(i => i.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryItemEntity>(b => b.HasKey(i => i.Id));

        modelBuilder.Entity<StockMovementEntity>(b =>
        {
            b.HasKey(m => m.Id);
            // Same guarantee as TreasuryMovements/OrderRefunds: a retried checkout that reaches
            // this far again can never deduct the same ingredient twice for the same order line.
            b.HasIndex(m => m.IdempotencyKey).IsUnique();
            b.HasOne<InventoryItemEntity>().WithMany().HasForeignKey(m => m.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<SupplierEntity>().WithMany().HasForeignKey(m => m.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrepRecipeEntity>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasOne<InventoryItemEntity>().WithMany().HasForeignKey(p => p.OutputInventoryItemId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(p => p.Lines).WithOne().HasForeignKey(l => l.PrepRecipeId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PrepRecipeLineEntity>(b =>
        {
            b.HasKey(l => l.Id);
            b.HasOne<InventoryItemEntity>().WithMany().HasForeignKey(l => l.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderPackagingRuleEntity>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasOne<InventoryItemEntity>().WithMany().HasForeignKey(r => r.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierEntity>(b => b.HasKey(s => s.Id));

        modelBuilder.Entity<PurchaseEntity>(b =>
        {
            b.HasKey(p => p.Id);
            // Same idempotency contract as every other financial command in this codebase - a
            // retried receivePurchase can never post the same invoice/stock/payment twice.
            b.HasIndex(p => p.IdempotencyKey).IsUnique();
            b.HasOne<SupplierEntity>().WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(p => p.Lines).WithOne().HasForeignKey(l => l.PurchaseId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(p => p.Payments).WithOne().HasForeignKey(pp => pp.PurchaseId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PurchaseLineEntity>(b =>
        {
            b.HasKey(l => l.Id);
            b.HasOne<InventoryItemEntity>().WithMany().HasForeignKey(l => l.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PurchasePaymentEntity>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.IdempotencyKey).IsUnique();
        });
        modelBuilder.Entity<SupplierLedgerEntryEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.IdempotencyKey).IsUnique();
            // Every read of "how much do we owe this supplier" (or "for this one invoice") filters
            // by exactly this - see PurchasingRules.SupplierOutstandingFromLedger.
            b.HasIndex(e => new { e.SupplierId, e.PurchaseId });
            b.HasOne<SupplierEntity>().WithMany().HasForeignKey(e => e.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerEntity>(b =>
        {
            b.HasKey(c => c.Id);
            // Phone is the identity - never looked up or merged by name (CustomerRules' own doc
            // comment). The database itself, not just an application-level check, refuses a second
            // customer row for the same normalized phone number.
            b.HasIndex(c => c.Phone).IsUnique();
            b.HasMany(c => c.Addresses).WithOne().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CustomerAddressEntity>(b => b.HasKey(a => a.Id));

        modelBuilder.Entity<DriverEntity>(b => b.HasKey(d => d.Id));
        modelBuilder.Entity<DeliveryServiceEntity>(b => b.HasKey(d => d.Id));

        modelBuilder.Entity<DeliveryEntity>(b =>
        {
            b.HasKey(d => d.Id);
            // One delivery row per order, ever - this is what makes checkout's auto-creation
            // idempotent by construction on retry, the same way CheckoutRequestId protects Orders.
            b.HasIndex(d => d.OrderId).IsUnique();
            b.HasOne<OrderEntity>().WithMany().HasForeignKey(d => d.OrderId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ShiftEntity>().WithMany().HasForeignKey(d => d.ShiftId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<DriverEntity>().WithMany().HasForeignKey(d => d.DriverId).OnDelete(DeleteBehavior.Restrict);
            b.Property(d => d.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<DriverCustodyEntity>(b =>
        {
            b.HasKey(c => c.Id);
            // The modern per-order dispatch path creates at most one custody row per delivery ever
            // (reassignment relabels DriverId on this same row - see DeliveryRules.ReassignCustody) -
            // this constraint is what makes that "relabel, never duplicate" invariant real at the
            // database level, not just in application code.
            b.HasIndex(c => c.DeliveryId).IsUnique();
            b.HasOne<DeliveryEntity>().WithMany().HasForeignKey(c => c.DeliveryId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<DriverEntity>().WithMany().HasForeignKey(c => c.DriverId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DeliveryReassignmentEntity>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.DeliveryId);
        });

        modelBuilder.Entity<DeliveryEventEntity>(b =>
        {
            b.HasKey(e => e.Id);
            // Every timeline read in this slice is "give me one order's full history" - see
            // DeliveryQueryService.
            b.HasIndex(e => e.DeliveryId);
        });

        modelBuilder.Entity<DriverSettlementEntity>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<PrintJobEntity>(b =>
        {
            b.HasKey(j => j.Id);
            // A station's own print agent polls "pending jobs for my route" - this is the exact
            // shape of that query.
            b.HasIndex(j => new { j.Route, j.Status });
            b.HasIndex(j => j.OrderId);
            b.HasIndex(j => j.CreateRequestId);
            b.HasMany(j => j.Attempts).WithOne().HasForeignKey(a => a.PrintJobId).OnDelete(DeleteBehavior.Cascade);
            // No FK to Orders by design: a print job records a moment-in-time hardware dispatch
            // fact, not a financial relationship - it must never be able to block or be blocked by
            // anything happening to the order it describes.
        });
        modelBuilder.Entity<PrintJobAttemptEntity>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasIndex(a => a.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<ChecklistItemEntity>(b =>
        {
            b.HasKey(i => i.Id);
            // Fixed catalog, ported verbatim from the reference app's own hardcoded checklistItems
            // array - see enterprise-store.tsx's initial state.
            b.HasData(
                new ChecklistItemEntity { Id = "open-drawer", Label = "تجهيز درج النقدية", Phase = "Opening", Required = true, Active = true, SortOrder = 1 },
                new ChecklistItemEntity { Id = "open-printer", Label = "فحص الطابعة", Phase = "Opening", Required = false, Active = true, SortOrder = 2 },
                new ChecklistItemEntity { Id = "open-stations", Label = "تأكيد جاهزية محطات التشغيل والتنظيف", Phase = "Opening", Required = false, Active = true, SortOrder = 3 },
                new ChecklistItemEntity { Id = "close-shifts", Label = "مراجعة إغلاق كل الورديات", Phase = "Closing", Required = true, Active = true, SortOrder = 1 },
                new ChecklistItemEntity { Id = "close-drawer", Label = "عد درج النقدية", Phase = "Closing", Required = true, Active = true, SortOrder = 2 },
                new ChecklistItemEntity { Id = "close-delivery", Label = "مراجعة عهد السائقين", Phase = "Closing", Required = true, Active = true, SortOrder = 3 },
                new ChecklistItemEntity { Id = "close-review", Label = "مراجعة المرتجعات والمصروفات", Phase = "Closing", Required = false, Active = true, SortOrder = 4 });
        });
        modelBuilder.Entity<ChecklistCompletionEntity>(b => b.HasKey(c => new { c.BusinessDayId, c.ItemId }));

        modelBuilder.Entity<BusinessDayCloseSnapshotEntity>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.BusinessDayId);
            b.HasOne<BusinessDayEntity>().WithMany().HasForeignKey(s => s.BusinessDayId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ControlExceptionStateEntity>(b => b.HasKey(e => e.Id));

        modelBuilder.Entity<ControlAuditEventEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.CreatedAtUtc);
        });

        modelBuilder.Entity<AuditEventEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.TimestampUtc);
            b.HasIndex(e => e.Category);
            b.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<CashDrawerOpenEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.ShiftId);
        });

        modelBuilder.Entity<CashDenominationCountEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.BusinessDayId);
        });

        modelBuilder.Entity<CashHandoverEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.TreasuryMovementId).IsUnique().HasFilter("[TreasuryMovementId] IS NOT NULL");
        });

        modelBuilder.Entity<EmployeeEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.EmployeeNumber).IsUnique();
        });

        modelBuilder.Entity<EmployeeLedgerEntryEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.EmployeeId);
            b.HasIndex(e => e.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
        });

        modelBuilder.Entity<AttendanceRecordEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.EmployeeId, e.Date }).IsUnique();
        });

        modelBuilder.Entity<PayrollRunEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
            // One live run per period, enforced by the database rather than by a read the handler
            // does just before it inserts. Eight requests arriving together all read "no run for
            // this period yet" and all inserted one; the restaurant ended up with several payroll
            // runs for the same month, each with its own set of employee lines to be paid.
            // Cancelled runs are excluded - a period whose run was cancelled has to be re-run.
            b.HasIndex(e => e.Period).IsUnique().HasDatabaseName("IX_PayrollRuns_Period_Live")
                .HasFilter("[Status] <> 'Cancelled'");
        });

        modelBuilder.Entity<PayrollLineEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.PayrollRunId);
        });

        modelBuilder.Entity<BackupRecordEntity>(b => b.HasKey(e => e.Id));
    }
}
