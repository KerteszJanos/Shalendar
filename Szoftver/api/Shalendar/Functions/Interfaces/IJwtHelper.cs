namespace Shalendar.Functions.Interfaces
{
	public interface IJwtHelper
	{
		Task<bool> HasCalendarPermission(HttpContext httpContext, string requiredPermissionLevel);
		Task<bool> HasCalendarPermission(HttpContext httpContext, string requiredPermissionLevel, int calendarId);
	}
}
