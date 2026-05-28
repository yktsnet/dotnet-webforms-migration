using AttendanceApi.Endpoints;
using AttendanceApi.Services;

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "Attendance API", Version = "v1" }));

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        var origins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:5173"];
        policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<AttendanceService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();

app.UseSwagger(options => {
    options.RouteTemplate = "api-docs/{documentName}/swagger.json";
});
app.UseSwaggerUI(options => {
    options.RoutePrefix = "api-docs";
    options.SwaggerEndpoint("/api-docs/v1/swagger.json", "Attendance API v1");
});

app.MapAuthEndpoints();
app.MapEmployeeEndpoints();
app.MapAttendanceEndpoints();

app.MapFallbackToFile("index.html");

app.Run();
