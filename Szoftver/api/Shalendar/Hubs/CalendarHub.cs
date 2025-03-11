using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

public class CalendarHub : Hub
{
	private readonly GroupManagerService _groupManager;

	public CalendarHub(GroupManagerService groupManager)
	{
		_groupManager = groupManager;
	}

	public override async Task OnConnectedAsync()
	{
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception exception)
	{
		foreach (var group in _groupManager.GetGroupsForConnection(Context.ConnectionId))
		{
			_groupManager.RemoveConnection(group, Context.ConnectionId);
		}

		await base.OnDisconnectedAsync(exception);
	}

	public async Task JoinGroup(string groupName)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
		_groupManager.AddConnection(groupName, Context.ConnectionId);
	}

	public async Task LeaveGroup(string groupName)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
		_groupManager.RemoveConnection(groupName, Context.ConnectionId);
	}
}
