namespace Shalendar.Functions.Interfaces
{
	public interface IDeleteCalendarHelper
	{
		Task<bool> ShouldDeleteCalendar(int userId, int calendarId);

		Task DeleteCalendar(int calendarId);
	}
}
