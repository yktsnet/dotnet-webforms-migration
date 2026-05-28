using Dapper;
using Npgsql;
using System.Text;

namespace AttendanceApi.Services;

// --- DTOs ---
public record AttendanceLogDto(
    int Id,
    string EmployeeId,
    DateTime? ClockIn,
    DateTime? ClockOut,
    bool IsCorrected
);

public record ClockInRequest(string EmployeeId);
public record ClockOutRequest(string EmployeeId);

public record CorrectAttendanceRequest(DateTime ClockIn, DateTime ClockOut);

public record MonthlySummaryDto(
    string EmployeeId,
    int Year,
    int Month,
    int WorkDays,
    decimal TotalHours,
    decimal OvertimeHours
);

public record MonthlyPayrollDto(
    string EmployeeId,
    int Year,
    int Month,
    decimal TotalHours,
    decimal OvertimeHours,
    decimal RegularPay,
    decimal OvertimePay,
    decimal TotalPay
);

public class AttendanceService
{
    private readonly string _connectionString;

    public AttendanceService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection が設定されていません。");
    }

    public async Task<bool> ClockInAsync(ClockInRequest req)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tran = await conn.BeginTransactionAsync();
        try
        {
            const string checkSql = @"
                SELECT COUNT(*) FROM attendance_logs
                WHERE employee_id = @EmployeeId
                  AND DATE(clock_in) = CURRENT_DATE
                  AND clock_out IS NULL";
            var count = await conn.ExecuteScalarAsync<int>(checkSql, new { req.EmployeeId }, tran);
            if (count > 0) return false;

            const string sql = "INSERT INTO attendance_logs (employee_id, clock_in) VALUES (@EmployeeId, NOW())";
            await conn.ExecuteAsync(sql, new { req.EmployeeId }, tran);
            await tran.CommitAsync();
            return true;
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ClockOutAsync(ClockOutRequest req)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tran = await conn.BeginTransactionAsync();
        try
        {
            const string findSql = @"
                SELECT id FROM attendance_logs
                WHERE employee_id = @EmployeeId
                  AND DATE(clock_in) = CURRENT_DATE
                  AND clock_out IS NULL
                LIMIT 1";
            var id = await conn.ExecuteScalarAsync<int?>(findSql, new { req.EmployeeId }, tran);
            if (id == null) return false;

            const string sql = "UPDATE attendance_logs SET clock_out = NOW() WHERE id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id }, tran);
            await tran.CommitAsync();
            return true;
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CorrectAttendanceAsync(int id, CorrectAttendanceRequest req)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            UPDATE attendance_logs
            SET clock_in = @ClockIn, clock_out = @ClockOut, is_corrected = TRUE
            WHERE id = @Id";
        var rows = await conn.ExecuteAsync(sql, new { req.ClockIn, req.ClockOut, Id = id });
        return rows > 0;
    }

    public async Task<IEnumerable<AttendanceLogDto>> GetHistoryAsync(string employeeId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT id, employee_id, clock_in, clock_out, is_corrected
            FROM attendance_logs
            WHERE employee_id = @EmployeeId
            ORDER BY clock_in DESC";
        return await conn.QueryAsync<AttendanceLogDto>(sql, new { EmployeeId = employeeId });
    }

    public async Task<MonthlySummaryDto> GetMonthlyAsync(string employeeId, int year, int month)
    {
        var logs = await GetMonthlyLogsAsync(employeeId, year, month);
        var employee = await GetEmployeeRecordAsync(employeeId);

        var workDays = 0;
        var totalMinutes = 0m;
        var overtimeMinutes = 0m;

        foreach (var log in logs)
        {
            if (log.ClockIn == null || log.ClockOut == null) continue;
            workDays++;
            var workedMin = (decimal)(log.ClockOut.Value - log.ClockIn.Value).TotalMinutes;
            var rounded = RoundMinutes(workedMin, employee.RoundUnitMinutes);
            totalMinutes += rounded;
            overtimeMinutes += Math.Max(0, rounded - 480);
        }

        return new MonthlySummaryDto(
            employeeId, year, month, workDays,
            Math.Round(totalMinutes / 60, 2),
            Math.Round(overtimeMinutes / 60, 2)
        );
    }

    public async Task<MonthlyPayrollDto> CalcMonthlyPayrollAsync(string employeeId, int year, int month)
    {
        var logs = await GetMonthlyLogsAsync(employeeId, year, month);
        var employee = await GetEmployeeRecordAsync(employeeId);

        var totalMinutes = 0m;
        var overtimeMinutes = 0m;

        foreach (var log in logs)
        {
            if (log.ClockIn == null || log.ClockOut == null) continue;
            var workedMin = (decimal)(log.ClockOut.Value - log.ClockIn.Value).TotalMinutes;
            var rounded = RoundMinutes(workedMin, employee.RoundUnitMinutes);
            totalMinutes += rounded;
            overtimeMinutes += Math.Max(0, rounded - 480);
        }

        var regularMinutes = totalMinutes - overtimeMinutes;
        var regularPay  = Math.Round(regularMinutes  / 60 * employee.HourlyWage, 0);
        var overtimePay = Math.Round(overtimeMinutes / 60 * employee.HourlyWage * 1.25m, 0);

        return new MonthlyPayrollDto(
            employeeId, year, month,
            Math.Round(totalMinutes / 60, 2),
            Math.Round(overtimeMinutes / 60, 2),
            regularPay, overtimePay, regularPay + overtimePay
        );
    }

    public async Task<byte[]> ExportMonthlyCsvAsync(string employeeId, int year, int month)
    {
        var logs = await GetMonthlyLogsAsync(employeeId, year, month);
        var sb = new StringBuilder();
        sb.AppendLine("日付,出勤時刻,退勤時刻,勤務時間(h),修正フラグ");
        foreach (var log in logs)
        {
            var hours = log.ClockIn.HasValue && log.ClockOut.HasValue
                ? (log.ClockOut.Value - log.ClockIn.Value).TotalHours.ToString("F2")
                : "";
            sb.AppendLine($"{log.ClockIn:yyyy-MM-dd},{log.ClockIn:HH:mm:ss},{log.ClockOut:HH:mm:ss},{hours},{log.IsCorrected}");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    // --- private ---
    private static decimal RoundMinutes(decimal minutes, int unit) =>
        Math.Floor(minutes / unit) * unit;

    private async Task<IEnumerable<AttendanceLogDto>> GetMonthlyLogsAsync(string employeeId, int year, int month)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT id, employee_id, clock_in, clock_out, is_corrected
            FROM attendance_logs
            WHERE employee_id = @EmployeeId
              AND clock_in IS NOT NULL
              AND EXTRACT(YEAR  FROM clock_in) = @Year
              AND EXTRACT(MONTH FROM clock_in) = @Month
            ORDER BY clock_in";
        return await conn.QueryAsync<AttendanceLogDto>(sql, new { EmployeeId = employeeId, Year = year, Month = month });
    }

    private async Task<EmployeeRecord> GetEmployeeRecordAsync(string employeeId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        const string sql = "SELECT id, hourly_wage, round_unit_minutes FROM employees WHERE id = @Id";
        return await conn.QuerySingleAsync<EmployeeRecord>(sql, new { Id = employeeId });
    }

    private record EmployeeRecord(string Id, decimal HourlyWage, int RoundUnitMinutes);
}
