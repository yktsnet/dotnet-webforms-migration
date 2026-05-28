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
        }).WithName("ClockIn").WithTags("Attendances");

        app.MapPost("/attendances/clock-out", async (ClockOutRequest req, AttendanceService svc) =>
        {
            var ok = await svc.ClockOutAsync(req);
            return ok ? Results.Ok() : Results.BadRequest("出勤打刻が見つかりません。");
        }).WithName("ClockOut").WithTags("Attendances");

        app.MapPut("/attendances/{id}", async (int id, CorrectAttendanceRequest req, AttendanceService svc) =>
        {
            var ok = await svc.CorrectAttendanceAsync(id, req);
            return ok ? Results.Ok() : Results.NotFound();
        }).WithName("CorrectAttendance").WithTags("Attendances");

        app.MapGet("/attendances/{employeeId}/monthly", async (
            string employeeId, int? year, int? month, AttendanceService svc) =>
        {
            var y = year  ?? DateTime.Now.Year;
            var m = month ?? DateTime.Now.Month;
            return Results.Ok(await svc.GetMonthlyAsync(employeeId, y, m));
        }).WithName("GetMonthly").WithTags("Attendances");

        app.MapGet("/attendances/{employeeId}/history", async (string employeeId, AttendanceService svc) =>
            Results.Ok(await svc.GetHistoryAsync(employeeId)))
           .WithName("GetHistory").WithTags("Attendances");

        app.MapGet("/attendances/{employeeId}/monthly/csv", async (
            string employeeId, int? year, int? month, AttendanceService svc) =>
        {
            var y = year  ?? DateTime.Now.Year;
            var m = month ?? DateTime.Now.Month;
            var csv = await svc.ExportMonthlyCsvAsync(employeeId, y, m);
            return Results.File(csv, "text/csv; charset=utf-8", $"attendance_{employeeId}_{y}{m:D2}.csv");
        }).WithName("GetMonthlyCsv").WithTags("Attendances");

        app.MapGet("/attendances/{employeeId}/payroll", async (
            string employeeId, int? year, int? month, AttendanceService svc) =>
        {
            var y = year  ?? DateTime.Now.Year;
            var m = month ?? DateTime.Now.Month;
            return Results.Ok(await svc.CalcMonthlyPayrollAsync(employeeId, y, m));
        }).WithName("GetPayroll").WithTags("Attendances");
    }
}
