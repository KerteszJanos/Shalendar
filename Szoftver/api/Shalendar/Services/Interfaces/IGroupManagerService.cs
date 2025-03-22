namespace Shalendar.Services.Interfaces
{
	public interface IGroupManagerService
	{
		void AddConnection(string groupName, string connectionId);
		void RemoveConnection(string groupName, string connectionId);
		bool IsUserAloneInGroup(string groupName);
		IEnumerable<string> GetGroupsForConnection(string connectionId);
	}
}
