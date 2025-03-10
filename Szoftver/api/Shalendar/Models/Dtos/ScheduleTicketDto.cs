namespace Shalendar.Models.Dtos
{
    public class ScheduleTicketDto
    {
        public int CalendarId { get; set; }
        public DateTime Date { get; set; }
        public int TicketId { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
    }
}
