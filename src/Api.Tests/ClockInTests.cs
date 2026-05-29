using AttendanceApi.Hubs;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Moq;
using Npgsql;

namespace AttendanceApi.Tests;

public class ClockInTests : IAsyncLifetime
{
    private const string TestEmployeeId = "TEST-CI";
    private readonly AttendanceService _svc;
    private readonly string _conn;

    public ClockInTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=KINTAI;Username=kintai_user;Password=kintai_pass"
            }).Build();

        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        // hub は ClockIn/ClockOut の Push のみ使うため no-op でよい
        var mockClients   = new Mock<IHubClients>();
        var mockProxy     = new Mock<IClientProxy>();
        var mockHubCtx    = new Mock<IHubContext<AttendanceHub>>();
        mockHubCtx.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.All).Returns(mockProxy.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockProxy.Object);
        mockProxy.Setup(c => c.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _svc  = new AttendanceService(config, mockHubCtx.Object);
        _conn = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task InitializeAsync()
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.ExecuteAsync(
            "INSERT INTO employees (id, name, hourly_wage, round_unit_minutes) VALUES (@Id, @Name, 1000, 1) ON CONFLICT DO NOTHING",
            new { Id = TestEmployeeId, Name = "テスト社員" });
        await conn.ExecuteAsync(
            "DELETE FROM attendance_logs WHERE employee_id = @Id AND DATE(clock_in) = CURRENT_DATE",
            new { Id = TestEmployeeId });
    }

    public async Task DisposeAsync()
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.ExecuteAsync(
            "DELETE FROM attendance_logs WHERE employee_id = @Id", new { Id = TestEmployeeId });
        await conn.ExecuteAsync(
            "DELETE FROM employees WHERE id = @Id", new { Id = TestEmployeeId });
    }

    [Fact]
    public async Task ClockIn_FirstTime_Succeeds()
    {
        var result = await _svc.ClockInAsync(new ClockInRequest(TestEmployeeId));
        Assert.True(result);
    }

    [Fact]
    public async Task ClockIn_Duplicate_ReturnsFalse()
    {
        await _svc.ClockInAsync(new ClockInRequest(TestEmployeeId));
        var result = await _svc.ClockInAsync(new ClockInRequest(TestEmployeeId));
        Assert.False(result);
    }
}
