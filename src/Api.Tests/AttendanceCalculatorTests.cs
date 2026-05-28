namespace AttendanceApi.Tests;

public class AttendanceCalculatorTests
{
    private static AttendanceLogDto Log(int id, DateTime clockIn, DateTime clockOut) =>
        new(id, "EMP-T", clockIn, clockOut, false);

    // --- CalcPayroll ---

    [Fact]
    public void CalcPayroll_ExactlyEightHours_NoOvertime()
    {
        var logs = new[] { Log(1, D(9, 0), D(17, 0)) }; // 8h
        var result = AttendanceCalculator.CalcPayroll("EMP-T", 2026, 5, logs, 1000m, 1);

        Assert.Equal(8m,    result.TotalHours);
        Assert.Equal(0m,    result.OvertimeHours);
        Assert.Equal(8000m, result.RegularPay);
        Assert.Equal(0m,    result.OvertimePay);
    }

    [Fact]
    public void CalcPayroll_TenHours_TwoHoursOvertime()
    {
        var logs = new[] { Log(1, D(9, 0), D(19, 0)) }; // 10h
        var result = AttendanceCalculator.CalcPayroll("EMP-T", 2026, 5, logs, 1000m, 1);

        Assert.Equal(10m,   result.TotalHours);
        Assert.Equal(2m,    result.OvertimeHours);
        Assert.Equal(8000m, result.RegularPay);
        Assert.Equal(2500m, result.OvertimePay); // 2h * 1000 * 1.25
        Assert.Equal(10500m, result.TotalPay);
    }

    [Fact]
    public void CalcPayroll_MultiDay_Mixed()
    {
        // 1日目8h(通常のみ) + 2日目10h(2h残業)
        var logs = new[]
        {
            Log(1, D(9, 0), D(17, 0)),
            Log(2, new DateTime(2026, 5, 2, 9, 0, 0), new DateTime(2026, 5, 2, 19, 0, 0))
        };
        var result = AttendanceCalculator.CalcPayroll("EMP-T", 2026, 5, logs, 1000m, 1);

        Assert.Equal(18m,    result.TotalHours);
        Assert.Equal(2m,     result.OvertimeHours);
        Assert.Equal(16000m, result.RegularPay);
        Assert.Equal(2500m,  result.OvertimePay);
    }

    // --- RoundDown ---

    [Theory]
    [InlineData(480,  15, 480)]  // ちょうど8h → そのまま
    [InlineData(482,  15, 480)]  // 2分超過 → 切り捨て
    [InlineData(29,   30,   0)]  // 29分 / 30単位 → 0
    [InlineData(60,    1,  60)]  // 単位1 → そのまま
    [InlineData(499,  15, 495)]  // 499分 → 495
    public void RoundDown_ReturnsFlooredValue(decimal minutes, int unit, decimal expected) =>
        Assert.Equal(expected, AttendanceCalculator.RoundDown(minutes, unit));

    // --- GetMonthlyAsync相当（純粋計算） ---

    [Fact]
    public void CalcSummary_CountsWorkDaysCorrectly()
    {
        var logs = new[]
        {
            Log(1, D(9, 0), D(17, 0)),
            Log(2, new DateTime(2026, 5, 2, 9, 0, 0), new DateTime(2026, 5, 2, 17, 0, 0)),
        };
        var result = AttendanceCalculator.CalcSummary("EMP-T", 2026, 5, logs, 1);

        Assert.Equal(2,   result.WorkDays);
        Assert.Equal(16m, result.TotalHours);
        Assert.Equal(0m,  result.OvertimeHours);
    }

    private static DateTime D(int h, int m) => new(2026, 5, 1, h, m, 0);
}
