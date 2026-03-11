using DairySystem.Api.suppliers;
using DairySystem.Api.Units;
using DairySystem.Api.Users;
using DairySystem.Api.Users.Auth;
using DairySystem.Api.Users.Roles;
using Microsoft.EntityFrameworkCore;

namespace DairySystem.Api
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductBatch> ProductBatches { get; set; }
        public DbSet<MilkCollection> MilkCollections { get; set; }
        public DbSet<SupplierPayment> SupplierPayments { get; set; }
        public DbSet<SupplierLedger> SupplierLedgers { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerPayment> CustomerPayments { get; set; }
        public DbSet<CustomerLedger> CustomerLedgers { get; set; }
        public DbSet<StockAdjustment> StockAdjustments { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<JournalLine> JournalLines { get; set; }
        public DbSet<AccountSetting> AccountSettings { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);


            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            UnitSeeder.Seed(modelBuilder);


            modelBuilder.Entity<Account>().HasData(
                // ── أصول (Assets) ──────────────────────────
                new Account { Id = 1, Code = "1000", Name = "الأصول", Type = AccountType.Asset, ParentId = null, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Account { Id = 2, Code = "1100", Name = "الأصول المتداولة", Type = AccountType.Asset, ParentId = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Account { Id = 3, Code = "1101", Name = "الصندوق", Type = AccountType.Asset, ParentId = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Account { Id = 4, Code = "1102", Name = "البنك", Type = AccountType.Asset, ParentId = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Account { Id = 5, Code = "1103", Name = "ذمم العملاء", Type = AccountType.Asset, ParentId = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Account { Id = 6, Code = "1104", Name = "المخزون", Type = AccountType.Asset, ParentId = 2, IsActive = true, CreatedAt = DateTime.UtcNow },

                // ── خصوم (Liabilities) ─────────────────────
                new Account { Id = 7, Code = "2000", Name = "الخصوم", Type = AccountType.Liability, ParentId = null, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Account { Id = 8, Code = "2100", Name = "الخصوم المتداولة", Type = AccountType.Liability, ParentId = 7, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Account { Id = 9, Code = "2101", Name = "ذمم الموردين", Type = AccountType.Liability, ParentId = 8, IsActive = true, CreatedAt = DateTime.UtcNow },

                // ── حقوق الملكية (Equity) ──────────────────
                new Account { Id = 10, Code = "3000", Name = "حقوق الملكية", Type = AccountType.Equity, ParentId = null, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Account { Id = 11, Code = "3001", Name = "رأس المال", Type = AccountType.Equity, ParentId = 10, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Account { Id = 12, Code = "3002", Name = "الأرباح المحتجزة", Type = AccountType.Equity, ParentId = 10, IsActive = true, CreatedAt = DateTime.UtcNow },

                // ── إيرادات (Revenue) ──────────────────────
                new Account { Id = 13, Code = "4000", Name = "الإيرادات", Type = AccountType.Revenue, ParentId = null, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Account { Id = 14, Code = "4001", Name = "إيرادات المبيعات", Type = AccountType.Revenue, ParentId = 13, IsActive = true, CreatedAt = DateTime.UtcNow },

                // ── مصروفات (Expenses) ─────────────────────
                new Account { Id = 15, Code = "5000", Name = "المصروفات", Type = AccountType.Expense, ParentId = null, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Account { Id = 16, Code = "5001", Name = "تكلفة البضاعة المباعة", Type = AccountType.Expense, ParentId = 15, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Account { Id = 17, Code = "5002", Name = "مصروفات تشغيلية", Type = AccountType.Expense, ParentId = 15, IsActive = true, CreatedAt = DateTime.UtcNow }
            );

        }



    }
}   