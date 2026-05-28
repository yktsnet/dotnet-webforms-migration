using AttendanceApi.Services;

namespace AttendanceApi.Endpoints;

public static class EmployeeEndpoints
{
    public static void MapEmployeeEndpoints(this WebApplication app)
    {
        // 社員一覧は認証不要（打刻パネルで全員分使用）
        app.MapGet("/employees", async (EmployeeService svc) =>
            Results.Ok(await svc.GetAllAsync()))
           .WithName("GetEmployees").WithTags("Employees");

        app.MapPost("/employees", async (CreateEmployeeRequest req, EmployeeService svc) =>
        {
            var ok = await svc.CreateAsync(req);
            return ok ? Results.Created($"/employees/{req.Id}", req) : Results.Conflict();
        })
        .WithName("CreateEmployee").WithTags("Employees")
        .RequireAuthorization();

        app.MapPut("/employees/{id}", async (string id, UpdateEmployeeRequest req, EmployeeService svc) =>
        {
            var ok = await svc.UpdateAsync(id, req);
            return ok ? Results.Ok() : Results.NotFound();
        })
        .WithName("UpdateEmployee").WithTags("Employees")
        .RequireAuthorization();

        app.MapDelete("/employees/{id}", async (string id, EmployeeService svc) =>
        {
            var ok = await svc.DeleteAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteEmployee").WithTags("Employees")
        .RequireAuthorization();
    }
}
