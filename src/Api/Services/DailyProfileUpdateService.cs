using Dapper;
using Npgsql;

namespace AttendanceApi.Services;

/// <summary>
/// 日次バッチ: 直近30日の退勤時刻平均を employee_profiles に書き込む。
/// 起動時に即実行し、以降は毎日0時に実行。
/// </summary>
public class DailyProfileUpdateService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<DailyProfileUpdateService> _logger;

    public DailyProfileUpdateService(IConfiguration config, ILogger<DailyProfileUpdateService> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await UpdateProfilesAsync(); // 起動時に即実行

        while (!stoppingToken.IsCancellationRequested)
        {
            // 次の0時まで待機
            var delay = DateTime.Today.AddDays(1) - DateTime.Now;
            await Task.Delay(delay, stoppingToken);
            if (!stoppingToken.IsCancellationRequested)
                await UpdateProfilesAsync();
        }
    }

    private async Task UpdateProfilesAsync()
    {
        try
        {
            var connStr = _config.GetConnectionString("DefaultConnection")!;
            using var conn = new NpgsqlConnection(connStr);

            // 直近30日の clock_out の時刻部分を平均して employee_profiles をUPSERT
            const string sql = @"
                INSERT INTO employee_profiles (employee_id, avg_clockout_time, updated_at)
                SELECT
                    employee_id,
                    MAKE_TIME(
                        FLOOR(AVG(EXTRACT(HOUR   FROM clock_out)))::int,
                        FLOOR(AVG(EXTRACT(MINUTE FROM clock_out)))::int,
                        0
                    ) AS avg_clockout_time,
                    NOW()
                FROM attendance_logs
                WHERE clock_out IS NOT NULL
                  AND clock_in >= CURRENT_DATE - INTERVAL '30 days'
                GROUP BY employee_id
                ON CONFLICT (employee_id) DO UPDATE
                  SET avg_clockout_time = EXCLUDED.avg_clockout_time,
                      updated_at        = NOW()";

            await conn.ExecuteAsync(sql);
            _logger.LogInformation("[DailyProfile] Updated at {Time}", DateTime.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DailyProfile] Update failed");
        }
    }
}
