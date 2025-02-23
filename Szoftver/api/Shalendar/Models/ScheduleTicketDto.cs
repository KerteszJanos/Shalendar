namespace Shalendar.Models
{
	public class ScheduleTicketDto
	{
		public int CalendarId { get; set; }
		public DateTime Date { get; set; }
		public Ticket Ticket { get; set; }
	}
}
