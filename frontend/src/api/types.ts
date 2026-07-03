export interface LoginResponse {
  token: string
  username: string
  role: string
  employeeId: number | null
  employeeName: string | null
}

export interface Dashboard {
  todaySales: number
  todayExpenses: number
  todayProfit: number
  cashBalance: number
  customerDebts: number
  lowStockCount: number
  todaySaleCount: number
}

export interface Product {
  id: number
  name: string
  sku: string | null
  categoryId: number
  categoryName: string
  brandId: number
  brandName: string
  purchasePrice: number
  salePrice: number
  minStockLevel: number
  stockQuantity: number
  isActive: boolean
}

export interface Category { id: number; name: string }
export interface Brand { id: number; name: string }

export interface Customer {
  id: number
  name: string
  phone: string | null
  email: string | null
  address: string | null
  loyaltyPoints: number
  isLoyal: boolean
  totalDebt: number
  purchaseCount: number
}

export interface Employee {
  id: number
  name: string
  phone: string | null
  email: string | null
  roleId: number
  roleName: string
  monthlySalary: number
  isActive: boolean
  hiredAt: string
}

export interface EmployeeRole { id: number; name: string }

export interface Supplier {
  id: number
  name: string
  phone: string | null
  email: string | null
  address: string | null
  totalPurchases: number
  totalPaid: number
  balance: number
}

export interface SaleLine {
  productId: number
  productName: string
  quantity: number
  unitSalePrice: number
  lineTotal: number
}

export interface Sale {
  id: number
  soldAt: string
  customerId: number
  customerName: string
  employeeId: number | null
  employeeName: string | null
  subTotal: number
  discountAmount: number
  discountPercent: number
  totalAmount: number
  amountPaid: number
  paymentMode: number
  status: number
  cancellationReason: string | null
  cancelledAt: string | null
  invoiceId: number | null
  invoiceNumber: string | null
  lines: SaleLine[]
}

export interface Invoice {
  id: number
  invoiceNumber: string
  issuedAt: string
  customerId: number
  customerName: string
  saleId: number | null
  subTotal: number
  discountAmount: number
  totalAmount: number
  status: number
  lines: { productId: number; productName: string; quantity: number; unitPrice: number; lineTotal: number }[]
}

export interface CashBox {
  id: number
  name: string
  openingBalance: number
  currentBalance: number
}

export interface CashTransaction {
  id: number
  cashBoxId: number
  cashBoxName: string
  employeeId: number | null
  employeeName: string | null
  kind: number
  createdAt: string
  amount: number
  motive: string
}

export interface Purchase {
  id: number
  supplierId: number
  supplierName: string
  purchasedAt: string
  deliveredAt: string | null
  status: number
  totalAmount: number
  amountPaid: number
  lines: { productId: number; productName: string; quantity: number; unitPurchasePrice: number; expirationDate: string }[]
}

export interface DailyReport {
  date: string
  totalSales: number
  totalExpenses: number
  profit: number
  saleCount: number
  sales: { id: number; soldAt: string; customerName: string; employeeName: string | null; totalAmount: number; paymentMode: number; itemCount: number }[]
  expenses: CashTransaction[]
}

export interface PeriodReport {
  from: string
  to: string
  totalSales: number
  totalPurchases: number
  totalExpenses: number
  profit: number
  saleCount: number
  topProducts: { productId: number; productName: string; quantitySold: number; revenue: number }[]
  topCustomers: { customerId: number; customerName: string; purchaseCount: number; totalSpent: number }[]
}

export const PAYMENT_MODES: Record<number, string> = {
  1: 'Espèces',
  2: 'Mobile Money',
  3: 'Carte',
  4: 'Crédit',
}

export const SALE_STATUS: Record<number, string> = {
  1: 'Active',
  2: 'Annulée',
}

export function formatMoney(n: number) {
  return new Intl.NumberFormat('fr-FR').format(n) + ' F'
}

export function formatDate(d: string) {
  return new Date(d).toLocaleString('fr-FR')
}
