using Dapper;
using Npgsql;

namespace AttendanceApi.Services;

public record EmployeeDto(
    string Id,
    string Name,
    decimal HourlyWage,
    int RoundUnitMinutes
);

public record CreateEmployeeRequest(
    string Id,
    string Name,
    decimal HourlyWage,
    int RoundUnitMinutes
);

public record UpdateEmployeeRequest(
    string Name,
    decimal HourlyWage,
    int RoundUnitMinutes
);

public class EmployeeService
{
    private readonly string _connectionString;

    public EmployeeService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection が設定されていません。");
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        const string sql = "SELECT id, name, hourly_wage, round_unit_minutes FROM employees ORDER BY id";
        return await conn.QueryAsync<EmployeeDto>(sql);
    }

    public async Task<bool> CreateAsync(CreateEmployeeRequest req)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO employees (id, name, hourly_wage, round_unit_minutes)
            VALUES (@Id, @Name, @HourlyWage, @RoundUnitMinutes)";
        var rows = await conn.ExecuteAsync(sql, req);
        return rows > 0;
    }

    public async Task<bool> UpdateAsync(string id, UpdateEmployeeRequest req)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            UPDATE employees
            SET name = @Name, hourly_wage = @HourlyWage, round_unit_minutes = @RoundUnitMinutes
            WHERE id = @Id";
        var rows = await conn.ExecuteAsync(sql, new { req.Name, req.HourlyWage, req.RoundUnitMinutes, Id = id });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        const string sql = "DELETE FROM employees WHERE id = @Id";
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}
