using AttendanceApi.Services;

namespace AttendanceApi.Endpoints;

public static class EmployeeEndpoints
{
    public static void MapEmployeeEndpoints(this WebApplication app)
    {
        app.MapGet("/employees", async (EmployeeService svc) =>
            Results.Ok(await svc.GetAllAsync()))
           .WithName("GetEmployees").WithTags("Employees");

        app.MapPost("/employees", async (CreateEmployeeRequest req, EmployeeService svc) =>
        {
            await svc.CreateAsync(req);
            return Results.Created($"/employees/{req.Id}", req);
        }).WithName("CreateEmployee").WithTags("Employees");

        app.MapPut("/employees/{id}", async (string id, UpdateEmployeeRequest req, EmployeeService svc) =>
        {
            var ok = await svc.UpdateAsync(id, req);
            return ok ? Results.Ok() : Results.NotFound();
        }).WithName("UpdateEmployee").WithTags("Employees");

        app.MapDelete("/employees/{id}", async (string id, EmployeeService svc) =>
        {
            var ok = await svc.DeleteAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteEmployee").WithTags("Employees");
    }
}
