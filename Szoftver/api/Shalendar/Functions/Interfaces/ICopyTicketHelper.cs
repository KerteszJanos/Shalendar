using Shalendar.Contexts;

namespace Shalendar.Functions.Interfaces
{
	public interface ICopyTicketHelper
	{
		Task<bool> CopyTicketAsync(ShalendarDbContext context, int ticketId, int calendarId, DateTime? date);
	}
}
