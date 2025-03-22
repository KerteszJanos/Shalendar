using Microsoft.AspNetCore.Http;

namespace Shalendar.Functions.Interfaces
{
	public interface IGetCalendarIdHelper
	{
		bool TryGetCalendarId(HttpContext httpContext, out int calendarId);
	}
}
