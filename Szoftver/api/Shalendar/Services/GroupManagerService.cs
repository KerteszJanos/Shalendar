using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public class GroupManagerService
{
	private readonly ConcurrentDictionary<string, HashSet<string>> _groupConnections = new();

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

	public bool IsUserAloneInGroup(string groupName)
	{
		return _groupConnections.TryGetValue(groupName, out var connections) && connections.Count == 1;
	}

	public IEnumerable<string> GetGroupsForConnection(string connectionId)
	{
		return _groupConnections
			.Where(kvp => kvp.Value.Contains(connectionId))
			.Select(kvp => kvp.Key)
			.ToList();
	}
}
