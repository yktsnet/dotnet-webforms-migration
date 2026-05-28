namespace AttendanceApi.Services;

public static class AttendanceCalculator
{
    public static MonthlySummaryDto CalcSummary(
        string employeeId, int year, int month,
        IEnumerable<AttendanceLogDto> logs,
        int roundUnitMinutes)
    {
        var workDays = 0;
        var totalMin = 0m;
        var overtimeMin = 0m;

        foreach (var log in logs)
        {
            if (log.ClockIn == null || log.ClockOut == null) continue;
            workDays++;
            var rounded = RoundDown((decimal)(log.ClockOut.Value - log.ClockIn.Value).TotalMinutes, roundUnitMinutes);
            totalMin += rounded;
            overtimeMin += Math.Max(0, rounded - 480);
        }

        return new MonthlySummaryDto(
            employeeId, year, month, workDays,
            Math.Round(totalMin / 60, 2),
            Math.Round(overtimeMin / 60, 2));
    }

    public static MonthlyPayrollDto CalcPayroll(
        string employeeId, int year, int month,
        IEnumerable<AttendanceLogDto> logs,
        decimal hourlyWage,
        int roundUnitMinutes)
    {
        var totalMin = 0m;
        var overtimeMin = 0m;

        foreach (var log in logs)
        {
            if (log.ClockIn == null || log.ClockOut == null) continue;
            var rounded = RoundDown((decimal)(log.ClockOut.Value - log.ClockIn.Value).TotalMinutes, roundUnitMinutes);
            totalMin += rounded;
            overtimeMin += Math.Max(0, rounded - 480);
        }

        var regularMin = totalMin - overtimeMin;
        var regularPay  = Math.Round(regularMin  / 60 * hourlyWage,        0);
        var overtimePay = Math.Round(overtimeMin / 60 * hourlyWage * 1.25m, 0);

        return new MonthlyPayrollDto(
            employeeId, year, month,
            Math.Round(totalMin / 60, 2),
            Math.Round(overtimeMin / 60, 2),
            regularPay, overtimePay, regularPay + overtimePay);
    }

    public static decimal RoundDown(decimal minutes, int unit) =>
        Math.Floor(minutes / unit) * unit;
}
