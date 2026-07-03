namespace ErpCosmetiqueApi.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int BrandId { get; set; }
    public Brand Brand { get; set; } = null!;

    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public int MinStockLevel { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<StockLot> StockLots { get; set; } = new List<StockLot>();
}

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<SupplierOrder> Orders { get; set; } = new List<SupplierOrder>();
    public ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int LoyaltyPoints { get; set; }
    public bool IsLoyal { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<CustomerPayment> Payments { get; set; } = new List<CustomerPayment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public enum StockMovementType
{
    PurchaseIn = 1,
    SaleOut = 2,
    Adjustment = 3,
    SaleCancel = 4
}

public class StockLot
{
    public long Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public DateOnly ExpirationDate { get; set; }
    public int QuantityOnHand { get; set; }

    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
}

public class StockMovement
{
    public long Id { get; set; }

    public StockMovementType Type { get; set; }

    public long StockLotId { get; set; }
    public StockLot StockLot { get; set; } = null!;

    public int QuantityDelta { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum PurchaseStatus
{
    Pending = 1,
    Delivered = 2,
    Cancelled = 3
}

public class Purchase
{
    public int Id { get; set; }
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public PurchaseStatus Status { get; set; } = PurchaseStatus.Delivered;
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }

    public ICollection<PurchaseLine> Lines { get; set; } = new List<PurchaseLine>();
}

public class PurchaseLine
{
    public int Id { get; set; }

    public int PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPurchasePrice { get; set; }
    public DateOnly ExpirationDate { get; set; }
}

public enum SupplierOrderStatus
{
    Pending = 1,
    Delivered = 2,
    Cancelled = 3
}

public class SupplierOrder
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public SupplierOrderStatus Status { get; set; } = SupplierOrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }

    public ICollection<SupplierOrderLine> Lines { get; set; } = new List<SupplierOrderLine>();
}

public class SupplierOrderLine
{
    public int Id { get; set; }

    public int SupplierOrderId { get; set; }
    public SupplierOrder SupplierOrder { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class SupplierPayment
{
    public long Id { get; set; }

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public int? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}

public enum PaymentMode
{
    Cash = 1,
    MobileMoney = 2,
    Card = 3,
    Credit = 4
}

public enum SaleStatus
{
    Active = 1,
    Cancelled = 2
}

public class Sale
{
    public int Id { get; set; }
    public DateTime SoldAt { get; set; } = DateTime.UtcNow;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }

    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;
    public SaleStatus Status { get; set; } = SaleStatus.Active;

    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    public Invoice? Invoice { get; set; }

    public ICollection<SaleLine> Lines { get; set; } = new List<SaleLine>();
}

public class SaleLine
{
    public int Id { get; set; }

    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitSalePrice { get; set; }
    public decimal LineTotal => Quantity * UnitSalePrice;
}

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Paid = 3,
    Cancelled = 4
}

public class Invoice
{
    public int Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}

public class InvoiceLine
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class CustomerPayment
{
    public long Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;
    public string? Note { get; set; }
}

public class CashBox
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }

    public ICollection<CashTransaction> Transactions { get; set; } = new List<CashTransaction>();
}

public enum CashTransactionKind
{
    Expense = 1,
    Income = 2
}

public class CashTransaction
{
    public long Id { get; set; }

    public int CashBoxId { get; set; }
    public CashBox CashBox { get; set; } = null!;

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public CashTransactionKind Kind { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public decimal Amount { get; set; }
    public string Motive { get; set; } = string.Empty;
}

public class EmployeeRole
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public int RoleId { get; set; }
    public EmployeeRole Role { get; set; } = null!;

    public decimal MonthlySalary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime HiredAt { get; set; } = DateTime.UtcNow;

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<SalaryAdvance> SalaryAdvances { get; set; } = new List<SalaryAdvance>();
    public ICollection<CashTransaction> CashTransactions { get; set; } = new List<CashTransaction>();
}

public class Attendance
{
    public long Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly Date { get; set; }
    public bool Present { get; set; }
    public string? Note { get; set; }
}

public class SalaryAdvance
{
    public long Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Motive { get; set; }
}

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Vendeur";
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
