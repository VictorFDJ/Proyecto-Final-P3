export interface User { id: string; name: string; email: string }
export interface AuthResponse { token: string; expiresAtUtc: string; user: User }
export interface Category { id: string; name: string; color: string; isActive: boolean; createdAtUtc: string }
export interface PaymentMethod { id: string; name: string; icon?: string; isActive: boolean; createdAtUtc: string }
export interface Expense {
  id: string; amount: number; date: string; description?: string
  categoryId: string; categoryName: string; categoryColor: string
  paymentMethodId: string; paymentMethodName: string; paymentMethodIcon?: string; createdAtUtc: string
}
export interface PagedResponse<T> { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number }
export type AlertLevel = 'normal' | 'warning' | 'critical' | 'limit_reached' | 'exceeded'
export interface Budget {
  id: string; year: number; month: number; amount: number; spent: number; remaining: number
  percentageUsed: number; alertLevel: AlertLevel; isExceeded: boolean
  categoryId: string; categoryName: string; categoryColor: string; createdAtUtc: string
}
export interface CategoryReport {
  categoryId: string; categoryName: string; categoryColor: string
  total: number; percentage: number; transactionCount: number
}
export interface DailyReport { date: string; total: number }
export interface MonthlyReport {
  year: number; month: number; totalSpent: number; transactionCount: number; averageExpense: number
  previousMonthTotal: number; differenceFromPreviousMonth: number; percentageChange?: number; trend: 'up' | 'down' | 'same'
  categoryBreakdown: CategoryReport[]; topCategories: CategoryReport[]; dailyTotals: DailyReport[]
}
export interface ImportError { rowNumber: number; message: string }
export interface ImportResult { totalRows: number; importedRows: number; failedRows: number; errors: ImportError[] }
