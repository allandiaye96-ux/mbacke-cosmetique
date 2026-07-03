using ErpCosmetiqueApi.Models;

namespace ErpCosmetiqueApi.DTOs;

public record CategoryDto(int Id, string Name);
public record CreateCategoryDto(string Name);

public record BrandDto(int Id, string Name);
public record CreateBrandDto(string Name);

public record ProductDto(
    int Id, string Name, string? Sku,
    int CategoryId, string CategoryName,
    int BrandId, string BrandName,
    decimal PurchasePrice, decimal SalePrice,
    int MinStockLevel, int StockQuantity, bool IsActive);

public record CreateProductDto(
    string Name, string? Sku,
    int CategoryId, int BrandId,
    decimal PurchasePrice, decimal SalePrice,
    int MinStockLevel = 5);

public record UpdateProductDto(
    string Name, string? Sku,
    int CategoryId, int BrandId,
    decimal PurchasePrice, decimal SalePrice,
    int MinStockLevel, bool IsActive);

public record StockLotDto(
    long Id, int ProductId, string ProductName,
    DateOnly ExpirationDate, int QuantityOnHand);

public record StockMovementDto(
    long Id, string Type, string ProductName,
    int QuantityDelta, string? Note, DateTime CreatedAt);

public record CustomerDto(
    int Id, string Name, string? Phone, string? Email, string? Address,
    int LoyaltyPoints, bool IsLoyal, decimal TotalDebt, int PurchaseCount);

public record CreateCustomerDto(string Name, string? Phone, string? Email, string? Address, bool IsLoyal = false);
public record UpdateCustomerDto(string Name, string? Phone, string? Email, string? Address, bool IsLoyal);

public record CustomerPaymentDto(
    long Id, int CustomerId, string CustomerName,
    int? SaleId, decimal Amount, DateTime PaidAt,
    PaymentMode PaymentMode, string? Note);

public record CreateCustomerPaymentDto(
    int CustomerId, int? SaleId, decimal Amount,
    PaymentMode PaymentMode, string? Note);

public record SupplierDto(
    int Id, string Name, string? Phone, string? Email, string? Address,
    decimal TotalPurchases, decimal TotalPaid, decimal Balance);

public record CreateSupplierDto(string Name, string? Phone, string? Email, string? Address);

public record SupplierOrderLineDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice);
public record SupplierOrderDto(
    int Id, int SupplierId, string SupplierName,
    DateTime OrderedAt, DateTime? DeliveredAt,
    SupplierOrderStatus Status, decimal TotalAmount, string? Note,
    List<SupplierOrderLineDto> Lines);

public record CreateSupplierOrderLineDto(int ProductId, int Quantity, decimal UnitPrice);
public record CreateSupplierOrderDto(int SupplierId, string? Note, List<CreateSupplierOrderLineDto> Lines);

public record SupplierPaymentDto(
    long Id, int SupplierId, string SupplierName,
    int? PurchaseId, decimal Amount, DateTime PaidAt, string? Note);

public record CreateSupplierPaymentDto(int SupplierId, int? PurchaseId, decimal Amount, string? Note);

public record PurchaseLineDto(int ProductId, string ProductName, int Quantity, decimal UnitPurchasePrice, DateOnly ExpirationDate);
public record PurchaseDto(
    int Id, int SupplierId, string SupplierName,
    DateTime PurchasedAt, DateTime? DeliveredAt,
    PurchaseStatus Status, decimal TotalAmount, decimal AmountPaid,
    List<PurchaseLineDto> Lines);

public record CreatePurchaseLineDto(int ProductId, int Quantity, decimal UnitPurchasePrice, DateOnly ExpirationDate);
public record CreatePurchaseDto(int SupplierId, List<CreatePurchaseLineDto> Lines);

public record SaleLineDto(int ProductId, string ProductName, int Quantity, decimal UnitSalePrice, decimal LineTotal);
public record SaleDto(
    int Id, DateTime SoldAt,
    int CustomerId, string CustomerName,
    int? EmployeeId, string? EmployeeName,
    decimal SubTotal, decimal DiscountAmount, decimal DiscountPercent, decimal TotalAmount, decimal AmountPaid,
    PaymentMode PaymentMode, SaleStatus Status,
    string? CancellationReason, DateTime? CancelledAt,
    int? InvoiceId, string? InvoiceNumber,
    List<SaleLineDto> Lines);

public record CreateSaleLineDto(int ProductId, int Quantity, decimal? UnitSalePrice);
public record CreateSaleDto(
    int CustomerId, int? EmployeeId,
    List<CreateSaleLineDto> Lines,
    decimal DiscountAmount = 0, decimal DiscountPercent = 0,
    PaymentMode PaymentMode = PaymentMode.Cash, decimal? AmountPaid = null);

public record CancelSaleDto(string Reason);

public record InvoiceLineDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
public record InvoiceDto(
    int Id, string InvoiceNumber, DateTime IssuedAt,
    int CustomerId, string CustomerName, int? SaleId,
    decimal SubTotal, decimal DiscountAmount, decimal TotalAmount,
    InvoiceStatus Status, List<InvoiceLineDto> Lines);

public record CashBoxDto(int Id, string Name, decimal OpeningBalance, decimal CurrentBalance);
public record CreateCashBoxDto(string Name, decimal OpeningBalance = 0);

public record CashTransactionDto(
    long Id, int CashBoxId, string CashBoxName,
    int? EmployeeId, string? EmployeeName,
    CashTransactionKind Kind, DateTime CreatedAt,
    decimal Amount, string Motive);

public record CreateCashExpenseDto(
    int CashBoxId, int? EmployeeId, decimal Amount, string Motive);

public record EmployeeRoleDto(int Id, string Name);
public record CreateEmployeeRoleDto(string Name);

public record EmployeeDto(
    int Id, string Name, string? Phone, string? Email,
    int RoleId, string RoleName, decimal MonthlySalary,
    bool IsActive, DateTime HiredAt);

public record CreateEmployeeDto(
    string Name, string? Phone, string? Email,
    int RoleId, decimal MonthlySalary);

public record AttendanceDto(long Id, int EmployeeId, string EmployeeName, DateOnly Date, bool Present, string? Note);
public record CreateAttendanceDto(int EmployeeId, DateOnly Date, bool Present, string? Note);

public record SalaryAdvanceDto(long Id, int EmployeeId, string EmployeeName, decimal Amount, DateTime CreatedAt, string? Motive);
public record CreateSalaryAdvanceDto(int EmployeeId, decimal Amount, string? Motive);

public record DashboardDto(
    decimal TodaySales, decimal TodayExpenses, decimal TodayProfit,
    decimal CashBalance, decimal CustomerDebts,
    int LowStockCount, int TodaySaleCount);

public record DailyReportDto(
    DateOnly Date, decimal TotalSales, decimal TotalExpenses, decimal Profit,
    int SaleCount, List<SaleSummaryDto> Sales, List<CashTransactionDto> Expenses);

public record SaleSummaryDto(
    int Id, DateTime SoldAt, string CustomerName, string? EmployeeName,
    decimal TotalAmount, PaymentMode PaymentMode, int ItemCount);

public record PeriodReportDto(
    DateOnly From, DateOnly To,
    decimal TotalSales, decimal TotalPurchases, decimal TotalExpenses, decimal Profit,
    int SaleCount, List<TopProductDto> TopProducts, List<TopCustomerDto> TopCustomers);

public record TopProductDto(int ProductId, string ProductName, int QuantitySold, decimal Revenue);
public record TopCustomerDto(int CustomerId, string CustomerName, int PurchaseCount, decimal TotalSpent);

public record SalesByDayDto(DateOnly Date, decimal TotalSales, int SaleCount);

public record FinanceSummaryDto(
    decimal TotalIncome, decimal TotalExpenses, decimal NetBalance, decimal Profit);

public record ReceiptDto(
    int SaleId,
    string ShopName,
    string ShopTagline,
    string? ShopPhone,
    string? ShopAddress,
    string? ShopCity,
    string? ShopEmail,
    DateTime SoldAt,
    string SoldAtFormatted,
    string? InvoiceNumber,
    string CustomerName,
    string? CustomerPhone,
    string? EmployeeName,
    List<SaleLineDto> Lines,
    int ItemCount,
    int TotalQuantity,
    decimal SubTotal,
    decimal DiscountPercent,
    decimal DiscountAmountFixed,
    decimal DiscountTotal,
    decimal TotalAmount,
    PaymentMode PaymentMode,
    string PaymentModeLabel,
    decimal AmountPaid,
    decimal ChangeGiven,
    decimal BalanceDue);

public record LoginRequestDto(string Username, string Password);
public record LoginResponseDto(string Token, string Username, string Role, int? EmployeeId, string? EmployeeName);
public record UserProfileDto(int Id, string Username, string Role, int? EmployeeId, string? EmployeeName);
