export const money = (value: number) => new Intl.NumberFormat('es-DO', {
  style: 'currency', currency: 'DOP', minimumFractionDigits: 2,
}).format(value)

export const shortDate = (value: string) => new Intl.DateTimeFormat('es-DO', {
  day: '2-digit', month: 'short', year: 'numeric', timeZone: 'UTC',
}).format(new Date(`${value.slice(0, 10)}T00:00:00Z`))

export const monthLabel = (year: number, month: number) => new Intl.DateTimeFormat('es-DO', {
  month: 'long', year: 'numeric',
}).format(new Date(year, month - 1, 1))

export const todayInput = () => new Date().toISOString().slice(0, 10)
