using Shalendar.Services.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public class GroupManagerService : IGroupManagerService
{
	private readonly ConcurrentDictionary<string, HashSet<string>> _groupConnections = new();

	/// <summary>
	/// Adds a connection ID to the specified group, creating the group if necessary.
	/// </summary>
	public void AddConnection(string groupName, string connectionId)
	{
		_groupConnections.AddOrUpdate(groupName,
			_ => new HashSet<string> { connectionId },
			(_, existingConnections) =>
			{
				existingConnections.Add(connectionId);
				return existingConnections;
			});
	}


	/// <summary>
	/// Removes a connection ID from the specified group and deletes the group if empty.
	/// </summary>
	public void RemoveConnection(string groupName, string connectionId)
	{
		if (_groupConnections.TryGetValue(groupName, out var connections))
		{
			connections.Remove(connectionId);
			if (connections.Count == 0)
			{
				_groupConnections.TryRemove(groupName, out _);
			}
		}
	}


	/// <summary>
	/// Checks if only one user remains in the specified group.
	/// </summary>
	public bool IsUserAloneInGroup(string groupName)
	{
		return _groupConnections.TryGetValue(groupName, out var connections) && connections.Count == 1;
	}


	/// <summary>
	/// Retrieves all groups that a given connection ID belongs to.
	/// </summary>
	public IEnumerable<string> GetGroupsForConnection(string connectionId)
	{
		return _groupConnections
			.Where(kvp => kvp.Value.Contains(connectionId))
			.Select(kvp => kvp.Key)
			.ToList();
	}
}
