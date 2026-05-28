using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace AttendanceApi.Endpoints;

public record LoginRequest(string Username, string Password);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (LoginRequest req, IConfiguration config) =>
        {
            var connStr = config.GetConnectionString("DefaultConnection")!;
            using var conn = new NpgsqlConnection(connStr);

            const string sql = "SELECT password_hash FROM admin_users WHERE username = @Username";
            var hash = await conn.ExecuteScalarAsync<string?>(sql, new { req.Username });

            if (hash == null || !BCrypt.Net.BCrypt.Verify(req.Password, hash))
                return Results.Unauthorized();

            var jwtCfg = config.GetSection("Jwt");
            var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtCfg["Key"]!));
            var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer:            jwtCfg["Issuer"],
                audience:          jwtCfg["Audience"],
                claims:            [new Claim(ClaimTypes.Name, req.Username),
                                    new Claim(ClaimTypes.Role, "admin")],
                expires:           DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        })
        .WithName("Login").WithTags("Auth");
    }
}
