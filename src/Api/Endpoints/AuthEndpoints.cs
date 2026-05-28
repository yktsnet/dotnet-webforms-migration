namespace AttendanceApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", () => Results.Ok(new { token = "stub" }))
           .WithName("Login").WithTags("Auth");
    }
}
