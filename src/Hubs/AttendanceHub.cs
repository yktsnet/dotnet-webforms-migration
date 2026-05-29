using Microsoft.AspNetCore.SignalR;

namespace AttendanceApi.Hubs;

public class AttendanceHub : Hub
{
    /// <summary>管理者グループに参加（OvertimeAlert / LateStayAlert 受信用）</summary>
    public async Task JoinAdminGroup()
        => await Groups.AddToGroupAsync(Context.ConnectionId, "admins");

    /// <summary>管理者グループから離脱</summary>
    public async Task LeaveAdminGroup()
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admins");
}
