const API_BASE = '/api'

function getToken() {
  return localStorage.getItem('token')
}

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  }
  const token = getToken()
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(`${API_BASE}${path}`, { ...options, headers })

  if (res.status === 401) {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    window.location.href = '/login'
    throw new Error('Non autorisé')
  }

  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }))
    throw new Error(err.error || err.title || 'Erreur serveur')
  }

  if (res.status === 204) return undefined as T
  return res.json()
}

export async function apiBlob(path: string) {
  const token = getToken()
  const res = await fetch(`${API_BASE}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  })
  if (!res.ok) throw new Error('Téléchargement échoué')
  return res.blob()
}

export const authApi = {
  login: (username: string, password: string) =>
    api<{ token: string; username: string; role: string; employeeId: number | null; employeeName: string | null }>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),
  me: () => api<{ id: number; username: string; role: string; employeeId: number | null; employeeName: string | null }>('/auth/me'),
}

export const dashboardApi = {
  get: () => api<import('./types').Dashboard>('/dashboard'),
}

export const productsApi = {
  list: () => api<import('./types').Product[]>('/products'),
  lowStock: () => api<import('./types').Product[]>('/products/low-stock'),
  create: (data: object) => api('/products', { method: 'POST', body: JSON.stringify(data) }),
  update: (id: number, data: object) => api(`/products/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id: number) => api(`/products/${id}`, { method: 'DELETE' }),
}

export const catalogApi = {
  categories: () => api<import('./types').Category[]>('/categories'),
  brands: () => api<import('./types').Brand[]>('/brands'),
  createCategory: (name: string) => api('/categories', { method: 'POST', body: JSON.stringify({ name }) }),
  updateCategory: (id: number, name: string) => api(`/categories/${id}`, { method: 'PUT', body: JSON.stringify({ name }) }),
  deleteCategory: (id: number) => api(`/categories/${id}`, { method: 'DELETE' }),
  createBrand: (name: string) => api('/brands', { method: 'POST', body: JSON.stringify({ name }) }),
  updateBrand: (id: number, name: string) => api(`/brands/${id}`, { method: 'PUT', body: JSON.stringify({ name }) }),
  deleteBrand: (id: number) => api(`/brands/${id}`, { method: 'DELETE' }),
}

export const salesApi = {
  list: (params?: string) => api<import('./types').Sale[]>(`/sales${params ? '?' + params : ''}`),
  create: (data: object) => api<import('./types').Sale>('/sales', { method: 'POST', body: JSON.stringify(data) }),
  cancel: (id: number, reason: string) =>
    api<import('./types').Sale>(`/sales/${id}/cancel`, { method: 'POST', body: JSON.stringify({ reason }) }),
  receipt: (id: number) => api(`/sales/${id}/receipt`),
}

export const customersApi = {
  list: () => api<import('./types').Customer[]>('/customers'),
  loyal: () => api<import('./types').Customer[]>('/customers/loyal'),
  create: (data: object) => api('/customers', { method: 'POST', body: JSON.stringify(data) }),
  update: (id: number, data: object) => api(`/customers/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id: number) => api(`/customers/${id}`, { method: 'DELETE' }),
  payments: (customerId?: number) =>
    api(`/customer-payments${customerId ? '?customerId=' + customerId : ''}`),
  pay: (data: object) => api('/customer-payments', { method: 'POST', body: JSON.stringify(data) }),
}

export const suppliersApi = {
  list: () => api<import('./types').Supplier[]>('/suppliers'),
  create: (data: object) => api('/suppliers', { method: 'POST', body: JSON.stringify(data) }),
  update: (id: number, data: object) => api(`/suppliers/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id: number) => api(`/suppliers/${id}`, { method: 'DELETE' }),
}

export const purchasesApi = {
  list: () => api<import('./types').Purchase[]>('/purchases'),
  create: (data: object) => api('/purchases', { method: 'POST', body: JSON.stringify(data) }),
  update: (id: number, data: object) => api(`/purchases/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  cancel: (id: number) => api(`/purchases/${id}/cancel`, { method: 'POST' }),
}

export const cashApi = {
  boxes: () => api<import('./types').CashBox[]>('/cash-boxes'),
  expenses: () => api<import('./types').CashTransaction[]>('/expenses'),
  addExpense: (data: object) => api('/expenses', { method: 'POST', body: JSON.stringify(data) }),
  updateExpense: (id: number, data: object) => api(`/expenses/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteExpense: (id: number) => api(`/expenses/${id}`, { method: 'DELETE' }),
  transactions: () => api<import('./types').CashTransaction[]>('/cash-transactions'),
}

export const personnelApi = {
  employees: () => api<import('./types').Employee[]>('/employees'),
  roles: () => api<import('./types').EmployeeRole[]>('/employee-roles'),
  createEmployee: (data: object) => api('/employees', { method: 'POST', body: JSON.stringify(data) }),
  updateEmployee: (id: number, data: object) => api(`/employees/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteEmployee: (id: number) => api(`/employees/${id}`, { method: 'DELETE' }),
  createRole: (name: string) => api('/employee-roles', { method: 'POST', body: JSON.stringify({ name }) }),
  updateRole: (id: number, name: string) => api(`/employee-roles/${id}`, { method: 'PUT', body: JSON.stringify({ name }) }),
  deleteRole: (id: number) => api(`/employee-roles/${id}`, { method: 'DELETE' }),
  attendance: () => api<{ id: number; employeeId: number; employeeName: string; date: string; present: boolean; note?: string }[]>('/attendance'),
  addAttendance: (data: object) => api('/attendance', { method: 'POST', body: JSON.stringify(data) }),
  updateAttendance: (id: number, data: object) => api(`/attendance/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteAttendance: (id: number) => api(`/attendance/${id}`, { method: 'DELETE' }),
  advances: () => api<{ id: number; employeeId: number; employeeName: string; amount: number; createdAt: string; motive: string | null }[]>('/salary-advances'),
  addAdvance: (data: object) => api('/salary-advances', { method: 'POST', body: JSON.stringify(data) }),
  updateAdvance: (id: number, data: object) => api(`/salary-advances/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteAdvance: (id: number) => api(`/salary-advances/${id}`, { method: 'DELETE' }),
}

export const invoicesApi = {
  list: () => api<import('./types').Invoice[]>('/invoices'),
  pdfUrl: (id: number) => `${API_BASE}/invoices/${id}/pdf`,
  downloadPdf: (id: number) => apiBlob(`/invoices/${id}/pdf`),
  cancel: (id: number) => api(`/invoices/${id}/cancel`, { method: 'POST' }),
}

export const reportsApi = {
  daily: (date?: string) => api<import('./types').DailyReport>(`/reports/daily${date ? '?date=' + date : ''}`),
  weekly: () => api<import('./types').PeriodReport>('/reports/weekly'),
  monthly: () => api<import('./types').PeriodReport>('/reports/monthly'),
  yearly: () => api<import('./types').PeriodReport>('/reports/yearly'),
}
