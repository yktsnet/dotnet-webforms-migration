export interface Employee {
  id: string
  name: string
  hourlyWage: number
  roundUnitMinutes: number
}
export interface AttendanceLog {
  id: number
  employeeId: string
  clockIn: string | null
  clockOut: string | null
  breakMinutes: number
  isCorrected: boolean
}
export interface CurrentAttendance {
  employeeId: string
  employeeName: string
  clockIn: string
}
export interface MonthlySummary {
  employeeId: string
  year: number
  month: number
  workDays: number
  totalHours: number
  overtimeHours: number
}
export interface MonthlyPayroll {
  employeeId: string
  year: number
  month: number
  totalHours: number
  overtimeHours: number
  regularPay: number
  overtimePay: number
  totalPay: number
}
