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
    public DbSet<TreasuryMovementEntity> TreasuryMovements => Set<TreasuryMovementEntity>();
    public DbSet<FinancialJournalEntryEntity> FinancialJournalEntries => Set<FinancialJournalEntryEntity>();
    public DbSet<OrderSequenceEntity> OrderSequence => Set<OrderSequenceEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<ManagerApprovalEntity> ManagerApprovals => Set<ManagerApprovalEntity>();

    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<ProductVariantEntity> ProductVariants => Set<ProductVariantEntity>();
    public DbSet<PriceRuleEntity> PriceRules => Set<PriceRuleEntity>();
    public DbSet<PriceRuleProductEntity> PriceRuleProducts => Set<PriceRuleProductEntity>();
    public DbSet<PriceRuleDayEntity> PriceRuleDays => Set<PriceRuleDayEntity>();
    public DbSet<TaxRuleEntity> TaxRules => Set<TaxRuleEntity>();
    public DbSet<RecipeEntity> Recipes => Set<RecipeEntity>();
    public DbSet<RecipeItemEntity> RecipeItems => Set<RecipeItemEntity>();
    public DbSet<InventoryItemEntity> InventoryItems => Set<InventoryItemEntity>();
    public DbSet<StockMovementEntity> StockMovements => Set<StockMovementEntity>();

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
            b.Property(s => s.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<TreasuryMovementEntity>(b =>
        {
            b.HasKey(m => m.Id);
            b.HasIndex(m => m.IdempotencyKey).IsUnique();
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
            // The lock/login screen looks a user up by PIN alone - this constraint is what makes
            // that lookup unambiguous, matching the reference app's behavior exactly.
            b.HasIndex(u => u.Pin).IsUnique();
        });

        modelBuilder.Entity<ManagerApprovalEntity>(b => b.HasKey(a => a.ApprovalId));

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
        });
    }
}
