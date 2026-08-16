import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Area, AreaChart, CartesianGrid, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { ArrowDownRight, ArrowRight, ArrowUpRight, CalendarDays, Download, Plus, ReceiptText, TrendingUp, Wallet } from 'lucide-react'
import { api, download } from '../lib/api'
import { money, monthLabel, shortDate } from '../lib/format'
import type { Budget, Expense, MonthlyReport, PagedResponse } from '../types'
import { EmptyState, Spinner, useToast } from '../components/UI'

export function DashboardPage() {
  const now = new Date(); const toast = useToast()
  const [period, setPeriod] = useState({ year: now.getFullYear(), month: now.getMonth() + 1 })
  const [report, setReport] = useState<MonthlyReport | null>(null)
  const [budgets, setBudgets] = useState<Budget[]>([]); const [expenses, setExpenses] = useState<Expense[]>([])
  const [loading, setLoading] = useState(true)
  const load = async () => {
    setLoading(true)
    try {
      const query = `year=${period.year}&month=${period.month}`
      const [nextReport, nextBudgets, nextExpenses] = await Promise.all([
        api<MonthlyReport>(`/reports/monthly?${query}`), api<Budget[]>(`/budgets?${query}`),
        api<PagedResponse<Expense>>(`/expenses?page=1&pageSize=5&fromDate=${period.year}-${String(period.month).padStart(2,'0')}-01&toDate=${period.year}-${String(period.month).padStart(2,'0')}-${new Date(period.year, period.month, 0).getDate()}`),
      ])
      setReport(nextReport); setBudgets(nextBudgets); setExpenses(nextExpenses.items)
    } catch { toast.show('No se pudo cargar el resumen.', 'error') } finally { setLoading(false) }
  }
  useEffect(() => { void load() }, [period.year, period.month])
  const changeMonth = (offset: number) => {
    const date = new Date(period.year, period.month - 1 + offset, 1)
    setPeriod({ year: date.getFullYear(), month: date.getMonth() + 1 })
  }
  if (loading || !report) return <Spinner/>
  const budgetTotal = budgets.reduce((sum, budget) => sum + budget.amount, 0)
  const remaining = budgetTotal - report.totalSpent
  const trendIcon = report.trend === 'up' ? <ArrowUpRight/> : report.trend === 'down' ? <ArrowDownRight/> : <TrendingUp/>
  const chartDays = report.dailyTotals.map(item => ({ day: Number(item.date.slice(-2)), total: item.total }))
  return <div className="page-stack">
    <div className="page-heading dashboard-heading"><div><span className="eyebrow">PANORAMA GENERAL</span><h1>Tu resumen financiero</h1><p>Todo lo importante de {monthLabel(period.year, period.month)}.</p></div>
      <div className="heading-actions"><div className="period-switch"><button onClick={()=>changeMonth(-1)}>‹</button><span><CalendarDays size={17}/>{monthLabel(period.year,period.month)}</span><button onClick={()=>changeMonth(1)}>›</button></div><Link className="btn primary" to="/gastos"><Plus size={18}/>Nuevo gasto</Link></div></div>
    <section className="stats-grid">
      <article className="stat-card accent"><span className="stat-icon"><Wallet/></span><div><small>Gastado este mes</small><strong>{money(report.totalSpent)}</strong><p className={report.trend}>{trendIcon}{report.percentageChange == null ? 'Sin comparación previa' : `${Math.abs(report.percentageChange)}% vs. mes anterior`}</p></div></article>
      <article className="stat-card"><span className="stat-icon green"><ReceiptText/></span><div><small>Presupuesto total</small><strong>{money(budgetTotal)}</strong><p>{budgets.length} categorías planificadas</p></div></article>
      <article className="stat-card"><span className="stat-icon amber"><TrendingUp/></span><div><small>Disponible</small><strong className={remaining < 0 ? 'text-danger':''}>{money(remaining)}</strong><p>{budgetTotal ? `${Math.round(report.totalSpent/budgetTotal*100)}% consumido` : 'Crea tu primer presupuesto'}</p></div></article>
      <article className="stat-card"><span className="stat-icon blue"><CalendarDays/></span><div><small>Gasto promedio</small><strong>{money(report.averageExpense)}</strong><p>{report.transactionCount} movimientos este mes</p></div></article>
    </section>
    <section className="dashboard-grid">
      <article className="card chart-card wide"><header><div><h2>Ritmo de gastos</h2><p>Gasto acumulado por día</p></div></header>
        <div className="chart"><ResponsiveContainer width="100%" height="100%"><AreaChart data={chartDays}><defs><linearGradient id="areaFill" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor="#6366f1" stopOpacity={.38}/><stop offset="1" stopColor="#6366f1" stopOpacity={0}/></linearGradient></defs><CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e8e9f0"/><XAxis dataKey="day" tickLine={false} axisLine={false}/><YAxis tickFormatter={v=>`${v/1000}k`} tickLine={false} axisLine={false} width={42}/><Tooltip formatter={(value)=>money(Number(value))}/><Area type="monotone" dataKey="total" stroke="#4f46e5" strokeWidth={3} fill="url(#areaFill)"/></AreaChart></ResponsiveContainer></div>
      </article>
      <article className="card chart-card"><header><div><h2>Por categoría</h2><p>Distribución del mes</p></div></header>
        {report.categoryBreakdown.length ? <><div className="donut"><ResponsiveContainer width="100%" height="100%"><PieChart><Pie data={report.categoryBreakdown} dataKey="total" nameKey="categoryName" innerRadius={58} outerRadius={82} paddingAngle={3}>{report.categoryBreakdown.map(item=><Cell key={item.categoryId} fill={item.categoryColor}/>)}</Pie><Tooltip formatter={v=>money(Number(v))}/></PieChart></ResponsiveContainer><div><strong>{report.categoryBreakdown.length}</strong><span>categorías</span></div></div><div className="legend">{report.categoryBreakdown.slice(0,4).map(item=><div key={item.categoryId}><span style={{background:item.categoryColor}}/><p>{item.categoryName}<small>{item.percentage}%</small></p><strong>{money(item.total)}</strong></div>)}</div></> : <EmptyState icon={<ReceiptText/>} title="Sin gastos" text="Registra gastos para ver la distribución."/>}
      </article>
      <article className="card budget-overview"><header><div><h2>Estado de presupuestos</h2><p>Alertas del mes</p></div><Link to="/presupuestos">Ver todos <ArrowRight size={16}/></Link></header>
        <div className="budget-list">{budgets.length ? budgets.slice(0,4).map(budget=><div key={budget.id} className="budget-row"><span className="color-dot" style={{background:budget.categoryColor}}/><div><div><strong>{budget.categoryName}</strong><span>{money(budget.spent)} de {money(budget.amount)}</span></div><div className="progress"><i className={budget.alertLevel} style={{width:`${Math.min(budget.percentageUsed,100)}%`}}/></div></div><b className={budget.alertLevel}>{budget.percentageUsed}%</b></div>) : <EmptyState icon={<Wallet/>} title="Sin presupuestos" text="Define límites para recibir alertas."/>}</div>
      </article>
      <article className="card recent-expenses"><header><div><h2>Últimos gastos</h2><p>Movimientos recientes</p></div><Link to="/gastos">Ver todos <ArrowRight size={16}/></Link></header>
        {expenses.length ? <div className="compact-list">{expenses.map(expense=><div key={expense.id}><span className="category-icon" style={{background:`${expense.categoryColor}18`,color:expense.categoryColor}}><ReceiptText size={19}/></span><div><strong>{expense.description || expense.categoryName}</strong><small>{expense.categoryName} · {shortDate(expense.date)}</small></div><b>-{money(expense.amount)}</b></div>)}</div> : <EmptyState icon={<ReceiptText/>} title="Aún no hay gastos" text="Tu actividad reciente aparecerá aquí."/>}
      </article>
    </section>
    <section className="card export-strip"><div><Download/><div><h3>Exporta tu reporte mensual</h3><p>Descárgalo para analizarlo o compartirlo.</p></div></div><div><button className="btn ghost" onClick={()=>download(`/reports/monthly/export/json?year=${period.year}&month=${period.month}`)}>JSON</button><button className="btn ghost" onClick={()=>download(`/reports/monthly/export/txt?year=${period.year}&month=${period.month}`)}>TXT</button><button className="btn primary" onClick={()=>download(`/reports/monthly/export/xlsx?year=${period.year}&month=${period.month}`)}>Excel</button></div></section>
  </div>
}
