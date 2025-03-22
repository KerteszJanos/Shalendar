using Shalendar.Functions.Interfaces;

namespace Shalendar.Functions
{
	public class GetCalendarIdHelper : IGetCalendarIdHelper
	{
		/// <summary>
		/// Attempts to retrieve and parse the "X-Calendar-Id" header as an integer.
		/// </summary>
		public bool TryGetCalendarId(HttpContext httpContext, out int calendarId)
		{
			if (!httpContext.Request.Headers.TryGetValue("X-Calendar-Id", out var calendarIdHeader) ||
				!int.TryParse(calendarIdHeader, out calendarId))
			{
				calendarId = 0;
				return false;
			}
			return true;
		}
	}
}
