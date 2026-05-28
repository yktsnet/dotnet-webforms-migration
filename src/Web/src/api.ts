const BASE = import.meta.env.VITE_API_URL ?? ''

function authHeaders(token: string): HeadersInit {
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
  }
}

export const api = {
  // ── Public ────────────────────────────────────────────────────────────
  getEmployees: (): Promise<import('./types').Employee[]> =>
    fetch(`${BASE}/employees`).then(r => r.json()),

  clockIn: (employeeId: string) =>
    fetch(`${BASE}/attendances/clock-in`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ employeeId }),
    }),

  clockOut: (employeeId: string) =>
    fetch(`${BASE}/attendances/clock-out`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ employeeId }),
    }),

  getMonthlySummary: (employeeId: string, year: number, month: number) =>
    fetch(`${BASE}/attendances/${employeeId}/monthly?year=${year}&month=${month}`)
      .then(r => r.json()),

  getHistory: (employeeId: string): Promise<import('./types').AttendanceLog[]> =>
    fetch(`${BASE}/attendances/${employeeId}/history`).then(r => r.json()),

  downloadCsv: (employeeId: string, year: number, month: number) =>
    fetch(`${BASE}/attendances/${employeeId}/monthly/csv?year=${year}&month=${month}`),

  demoReset: () =>
    fetch(`${BASE}/demo/reset`, { method: 'POST' }),

  // ── Auth ──────────────────────────────────────────────────────────────
  login: (username: string, password: string): Promise<{ token?: string }> =>
    fetch(`${BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password }),
    }).then(r => r.ok ? r.json() : Promise.resolve({})),

  // ── Admin: Employee ───────────────────────────────────────────────────
  createEmployee: (
    req: { id: string; name: string; hourlyWage: number; roundUnitMinutes: number },
    token: string
  ) =>
    fetch(`${BASE}/employees`, {
      method: 'POST',
      headers: authHeaders(token),
      body: JSON.stringify(req),
    }),

  updateEmployee: (
    id: string,
    req: { name: string; hourlyWage: number; roundUnitMinutes: number },
    token: string
  ) =>
    fetch(`${BASE}/employees/${id}`, {
      method: 'PUT',
      headers: authHeaders(token),
      body: JSON.stringify(req),
    }),

  deleteEmployee: (id: string, token: string) =>
    fetch(`${BASE}/employees/${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    }),

  // ── Admin: Attendance correction ─────────────────────────────────────
  correctAttendance: (
    id: number,
    req: { clockIn: string; clockOut: string },
    token: string
  ) =>
    fetch(`${BASE}/attendances/${id}`, {
      method: 'PUT',
      headers: authHeaders(token),
      body: JSON.stringify(req),
    }),
}
