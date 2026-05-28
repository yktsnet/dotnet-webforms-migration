using AttendanceApi.Services;

namespace AttendanceApi.Endpoints;

public static class AttendanceEndpoints
{
    public static void MapAttendanceEndpoints(this WebApplication app)
    {
        app.MapPost("/attendances/clock-in", async (ClockInRequest req, AttendanceService svc) =>
        {
            var ok = await svc.ClockInAsync(req);
            return ok ? Results.Ok() : Results.Conflict("既に出勤打刻済みです。");
        })
        .WithName("ClockIn").WithTags("Attendances");

        app.MapPost("/attendances/clock-out", async (ClockOutRequest req, AttendanceService svc) =>
        {
            var ok = await svc.ClockOutAsync(req);
            return ok ? Results.Ok() : Results.NotFound("出勤打刻が見つかりません。");
        })
        .WithName("ClockOut").WithTags("Attendances");

        // 打刻修正は管理者のみ
        app.MapPut("/attendances/{id}", async (int id, CorrectAttendanceRequest req, AttendanceService svc) =>
        {
            var ok = await svc.CorrectAttendanceAsync(id, req);
            return ok ? Results.Ok() : Results.NotFound();
        })
        .WithName("CorrectAttendance").WithTags("Attendances")
        .RequireAuthorization();

        app.MapGet("/attendances/{employeeId}/monthly",
            async (string employeeId, int? year, int? month, AttendanceService svc) =>
        {
            var now = DateTime.Now;
            return Results.Ok(await svc.GetMonthlyAsync(
                employeeId, year ?? now.Year, month ?? now.Month));
        })
        .WithName("GetMonthlySummary").WithTags("Attendances");

        app.MapGet("/attendances/{employeeId}/history",
            async (string employeeId, AttendanceService svc) =>
            Results.Ok(await svc.GetHistoryAsync(employeeId)))
           .WithName("GetHistory").WithTags("Attendances");

        app.MapGet("/attendances/{employeeId}/monthly/csv",
            async (string employeeId, int? year, int? month, AttendanceService svc, HttpResponse response) =>
        {
            var now = DateTime.Now;
            var y   = year  ?? now.Year;
            var m   = month ?? now.Month;
            var csv = await svc.ExportMonthlyCsvAsync(employeeId, y, m);
            response.Headers["Content-Disposition"] =
                $"attachment; filename=attendance_{employeeId}_{y}{m:D2}.csv";
            return Results.File(csv, "text/csv; charset=utf-8");
        })
        .WithName("ExportMonthlyCsv").WithTags("Attendances");

        app.MapGet("/attendances/{employeeId}/payroll",
            async (string employeeId, int? year, int? month, AttendanceService svc) =>
        {
            var now = DateTime.Now;
            return Results.Ok(await svc.CalcMonthlyPayrollAsync(
                employeeId, year ?? now.Year, month ?? now.Month));
        })
        .WithName("GetMonthlyPayroll").WithTags("Attendances");

        // デモリセット（認証不要）
        app.MapPost("/demo/reset", async (AttendanceService svc) =>
        {
            await svc.ResetForDemoAsync();
            return Results.Ok();
        })
        .WithName("DemoReset").WithTags("Demo");
    }
}
