using Microsoft.EntityFrameworkCore;

namespace ErpCosmetiqueApi.Models;

public class ErpDbContext : DbContext
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<StockLot> StockLots => Set<StockLot>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseLine> PurchaseLines => Set<PurchaseLine>();

    public DbSet<SupplierOrder> SupplierOrders => Set<SupplierOrder>();
    public DbSet<SupplierOrderLine> SupplierOrderLines => Set<SupplierOrderLine>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();

    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    public DbSet<CashBox> CashBoxes => Set<CashBox>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();

    public DbSet<EmployeeRole> EmployeeRoles => Set<EmployeeRole>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<SalaryAdvance> SalaryAdvances => Set<SalaryAdvance>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>().HasKey(x => x.Id);
        modelBuilder.Entity<Brand>().HasKey(x => x.Id);
        modelBuilder.Entity<Product>().HasKey(x => x.Id);

        modelBuilder.Entity<Product>()
            .HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(x => x.Brand)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockLot>().HasKey(x => x.Id);
        modelBuilder.Entity<StockLot>()
            .HasOne(x => x.Product)
            .WithMany(x => x.StockLots)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockMovement>().HasKey(x => x.Id);
        modelBuilder.Entity<StockMovement>()
            .HasOne(x => x.StockLot)
            .WithMany(x => x.Movements)
            .HasForeignKey(x => x.StockLotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Purchase>().HasKey(x => x.Id);
        modelBuilder.Entity<PurchaseLine>().HasKey(x => x.Id);
        modelBuilder.Entity<PurchaseLine>()
            .HasOne(x => x.Purchase)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseLine>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SupplierOrder>().HasKey(x => x.Id);
        modelBuilder.Entity<SupplierOrderLine>().HasKey(x => x.Id);
        modelBuilder.Entity<SupplierOrderLine>()
            .HasOne(x => x.SupplierOrder)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.SupplierOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SupplierPayment>().HasKey(x => x.Id);

        modelBuilder.Entity<Sale>().HasKey(x => x.Id);
        modelBuilder.Entity<SaleLine>().HasKey(x => x.Id);
        modelBuilder.Entity<SaleLine>()
            .HasOne(x => x.Sale)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Invoice>()
            .HasOne(x => x.Sale)
            .WithOne(x => x.Invoice)
            .HasForeignKey<Invoice>(x => x.SaleId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Invoice>().HasKey(x => x.Id);
        modelBuilder.Entity<Invoice>()
            .HasIndex(x => x.InvoiceNumber)
            .IsUnique();

        modelBuilder.Entity<InvoiceLine>().HasKey(x => x.Id);
        modelBuilder.Entity<InvoiceLine>()
            .HasOne(x => x.Invoice)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CustomerPayment>().HasKey(x => x.Id);

        modelBuilder.Entity<CashBox>().HasKey(x => x.Id);
        modelBuilder.Entity<CashTransaction>().HasKey(x => x.Id);
        modelBuilder.Entity<CashTransaction>()
            .HasOne(x => x.CashBox)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.CashBoxId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CashTransaction>()
            .HasIndex(x => x.CreatedAt);

        modelBuilder.Entity<EmployeeRole>().HasKey(x => x.Id);
        modelBuilder.Entity<Employee>().HasKey(x => x.Id);
        modelBuilder.Entity<Attendance>().HasKey(x => x.Id);
        modelBuilder.Entity<Attendance>()
            .HasIndex(x => new { x.EmployeeId, x.Date })
            .IsUnique();

        modelBuilder.Entity<SalaryAdvance>().HasKey(x => x.Id);

        modelBuilder.Entity<AppUser>().HasKey(x => x.Id);
        modelBuilder.Entity<AppUser>()
            .HasIndex(x => x.Username)
            .IsUnique();
        modelBuilder.Entity<AppUser>()
            .HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Sale>()
            .HasIndex(x => x.SoldAt);

        modelBuilder.Entity<CashTransaction>()
            .HasOne(x => x.Employee)
            .WithMany(x => x.CashTransactions)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
