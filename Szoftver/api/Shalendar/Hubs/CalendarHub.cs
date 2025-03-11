using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class CalendarHub : Hub
{
	public async Task JoinGroup(string calendarId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, calendarId);
	}

	public async Task LeaveGroup(string calendarId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, calendarId);
	}
}
