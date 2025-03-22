using Microsoft.AspNetCore.SignalR;
using Shalendar.Services.Interfaces;
using System;
using System.Threading.Tasks;

public class CalendarHub : Hub
{
	private readonly IGroupManagerService _groupManager;

	public CalendarHub(IGroupManagerService groupManager)
	{
		_groupManager = groupManager;
	}

	/// <summary>
	/// /// Handles client connection events and invokes the base connection logic.
	/// </summary>
	public override async Task OnConnectedAsync()
	{
		await base.OnConnectedAsync();
	}


	/// <summary>
	/// Handles client disconnection events and removes the connection from all groups.
	/// </summary>
	public override async Task OnDisconnectedAsync(Exception exception)
	{
		foreach (var group in _groupManager.GetGroupsForConnection(Context.ConnectionId))
		{
			_groupManager.RemoveConnection(group, Context.ConnectionId);
		}

		await base.OnDisconnectedAsync(exception);
	}


	/// <summary>
	/// Adds the client to the specified group and tracks the connection.
	/// </summary>
	public async Task JoinGroup(string groupName)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
		_groupManager.AddConnection(groupName, Context.ConnectionId);
	}


	/// <summary>
	/// Removes the client from the specified group and updates the connection tracking.
	/// </summary>
	public async Task LeaveGroup(string groupName)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
		_groupManager.RemoveConnection(groupName, Context.ConnectionId);
	}
}
